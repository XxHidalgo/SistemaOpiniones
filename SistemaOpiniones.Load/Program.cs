using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Result;
using SistemaOpiniones.Load;

using var host = AppHost.CreateHost(args);
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

// Limpia las tablas para poder re-ejecutar la carga completa.
var db = sp.GetRequiredService<SistemaOpinionesContext>();
await LimpiarBaseDatos(db);
Console.WriteLine("Base de datos limpiada.\n");

// Catálogos primero, porque son FK de las demás tablas.
var categoria = sp.GetRequiredService<ICategoriaService>();
await Ejecutar("Categoria", categoria.LoadCategoria, categoria.SaveCategoria);

var tipoFuente = sp.GetRequiredService<ITipoFuenteService>();
await Ejecutar("TipoFuente", tipoFuente.LoadTipoFuente, tipoFuente.SaveTipoFuente);

var clasificacion = sp.GetRequiredService<IClasificacionService>();
await Ejecutar("Clasificacion", clasificacion.LoadClasificacion, clasificacion.SaveClasificacion);

var cliente = sp.GetRequiredService<IClienteService>();
await Ejecutar("Cliente", cliente.LoadCliente, cliente.SaveCliente);

var producto = sp.GetRequiredService<IProductoService>();
await Ejecutar("Producto", producto.LoadProducto, producto.SaveProducto);

var fuenteDatos = sp.GetRequiredService<IFuenteDatosService>();
await Ejecutar("FuenteDatos", fuenteDatos.LoadFuenteDatos, fuenteDatos.SaveFuenteDatos);

// Las 3 fuentes de opiniones van a la misma tabla Opinion.
var social = sp.GetRequiredService<ISocialCommentService>();
await Ejecutar("Opinion (Social)", social.LoadSocialComment, social.SaveSocialComment);

var web = sp.GetRequiredService<IWebReviewService>();
await Ejecutar("Opinion (Web)", web.LoadWebReview, web.SaveWebReview);

var survey = sp.GetRequiredService<ISurveyService>();
await Ejecutar("Opinion (Survey)", survey.LoadSurvey, survey.SaveSurvey);

static async Task LimpiarBaseDatos(SistemaOpinionesContext db)
{
    await db.Database.ExecuteSqlRawAsync(@"
        DELETE FROM dbo.Opinion;
        DELETE FROM dbo.Producto;
        DELETE FROM dbo.FuenteDatos;
        DELETE FROM dbo.Cliente;
        DELETE FROM dbo.Categoria;
        DELETE FROM dbo.TipoFuente;
        DELETE FROM dbo.Clasificacion;
        DBCC CHECKIDENT ('dbo.Opinion', RESEED, 0);
        DBCC CHECKIDENT ('dbo.Categoria', RESEED, 0);
        DBCC CHECKIDENT ('dbo.TipoFuente', RESEED, 0);
        DBCC CHECKIDENT ('dbo.Clasificacion', RESEED, 0);
    ");
}

static async Task Ejecutar(string nombre, Func<Task<OperationResult>> load, Func<Task<OperationResult>> save)
{
    var carga = await load();
    if (!carga.Success)
    {
        Console.WriteLine($"Tabla: {nombre} -> Error al cargar: {carga.Message}");
        return;
    }

    var r = await save();
    string proc = $"{r.Processed},";
    string ins = $"{r.Inserted},";
    Console.WriteLine($"Tabla: {nombre,-13}-> Procesados: {proc,-5}Insertados: {ins,-5}Rechazados: {r.Rejected}");
    if (!r.Success)
        Console.WriteLine($"Tabla: {nombre,-13}-> Error: {r.Message}");
}
