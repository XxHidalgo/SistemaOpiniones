using System.Data;
using System.Globalization;
using AutoMapper;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Services;

public class BaseService<TDto, TEntity> : IBaseService<TDto, TEntity>
    where TDto : class
    where TEntity : class
{
    private readonly SistemaOpinionesContext _context;
    private readonly IMapper _mapper;
    private readonly string _filePath;
    private readonly OperationResult _operationResult;
    private List<TDto> _data;

    /// <summary>Contexto EF disponible para los servicios derivados (ej. resolver FKs).</summary>
    protected SistemaOpinionesContext Context => _context;

    public BaseService(SistemaOpinionesContext context, IMapper mapper, string filePath)
    {
        _context = context;
        _mapper = mapper;
        _filePath = filePath;
        _operationResult = new OperationResult();
        _data = new List<TDto>();
    }

    public async Task<OperationResult> LoadAsync()
    {
        try
        {
            if (!_validateFilePath(_filePath))
                return _operationResult;

            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            _data = new List<TDto>();
            await foreach (var record in csv.GetRecordsAsync<TDto>())
            {
                _data.Add(record);
            }

            _operationResult.Success = true;
            _operationResult.Message = $"Se leyeron {_data.Count} registros del archivo.";
            _operationResult.Data = _data;
        }
        catch (Exception ex)
        {
            _operationResult.Success = false;
            _operationResult.Message = $"Error al cargar los datos: {ex.Message}";
        }

        return _operationResult;
    }

    public async Task<OperationResult> SaveAsync(MethodDbEnum methodDbEnum)
    {
        try
        {
            if (_data.Count == 0)
            {
                _operationResult.Success = false;
                _operationResult.Message = "No hay datos para guardar. Ejecuta LoadAsync primero.";
                return _operationResult;
            }

            int processed = _data.Count;

            List<TEntity> entities = _mapper.Map<List<TEntity>>(_data);

            // Resuelve claves foráneas texto->Id (si el servicio lo necesita).
            // Corre antes de deduplicar, así entities y _data siguen alineados por índice.
            await ResolveForeignKeysAsync(entities, _data);

            // Elimina duplicados si el servicio define una clave
            // (ej. categorías/tipos/clasificaciones que se repiten en el CSV).
            // Se aplica antes del switch, así vale tanto para EF como para ADO.
            if (DistinctKey is not null)
                entities = entities.DistinctBy(DistinctKey).ToList();

            int inserted = methodDbEnum switch
            {
                MethodDbEnum.EntityFramework => await _saveWithEntityFrameworkAsync(entities),
                MethodDbEnum.AdoNet => await _saveWithAdoNetAsync(entities),
                _ => -1
            };

            if (inserted < 0)
            {
                _operationResult.Success = false;
                _operationResult.Message = $"Método de guardado no soportado: {methodDbEnum}.";
                return _operationResult;
            }

            _operationResult.Success = true;
            _operationResult.Processed = processed;
            _operationResult.Inserted = inserted;
            _operationResult.Rejected = processed - inserted;
            _operationResult.Message =
                $"'{typeof(TEntity).Name}': procesados {processed}, insertados {inserted}, rechazados {processed - inserted}.";
            _operationResult.Data = entities;
        }
        catch (Exception ex)
        {
            _operationResult.Success = false;
            _operationResult.Message = $"Error al guardar los datos: {ex.Message}";
        }

        return _operationResult;
    }

    private async Task<int> _saveWithEntityFrameworkAsync(IEnumerable<TEntity> entities)
    {
        await _context.Set<TEntity>().AddRangeAsync(entities);
        return await _context.SaveChangesAsync();
    }

    private async Task<int> _saveWithAdoNetAsync(IReadOnlyList<TEntity> entities)
    {
        var connectionString = _context.Database.GetConnectionString();
        int inserted = 0;

        foreach (var entity in entities)
        {
            var (procedureName, parameters) = BuildInsertCommand(entity);

            using SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(procedureName, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters.ToArray());

            try
            {
                await command.ExecuteNonQueryAsync();
                inserted++;
            }
            catch
            {
                // Fila rechazada (ej. viola FK o dato inválido): se cuenta y se continúa.
            }
        }

        return inserted;
    }

    /// <summary>
    /// Clave para eliminar duplicados antes de guardar. Si es null, no se deduplica.
    /// Cada servicio la sobrescribe según su necesidad (ej. c => c.Nombre).
    /// </summary>
    protected virtual Func<TEntity, object>? DistinctKey => null;

    /// <summary>
    /// Resuelve las claves foráneas que en el CSV vienen como texto (ej. nombre de
    /// categoría -> IdCategoria). Recibe las entidades y los DTO alineados por índice.
    /// Por defecto no hace nada; cada servicio lo sobrescribe si lo necesita.
    /// </summary>
    protected virtual Task ResolveForeignKeysAsync(List<TEntity> entities, List<TDto> dtos)
        => Task.CompletedTask;

    /// <summary>
    /// Cada servicio que use ADO.NET sobrescribe este método para indicar el
    /// procedimiento almacenado y sus parámetros para la entidad dada.
    /// </summary>
    protected virtual (string ProcedureName, List<SqlParameter> Parameters) BuildInsertCommand(TEntity entity)
        => throw new NotImplementedException(
            $"El servicio de '{typeof(TEntity).Name}' no definió su procedimiento almacenado para ADO.NET.");

    private bool _validateFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _operationResult.Success = false;
            _operationResult.Message = "La ruta del archivo no puede estar vacía.";
            return false;
        }

        if (!File.Exists(filePath))
        {
            _operationResult.Success = false;
            _operationResult.Message = $"El archivo '{filePath}' no existe.";
            return false;
        }

        return true;
    }
}
