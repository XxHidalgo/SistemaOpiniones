using AutoMapper;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;

namespace SistemaOpiniones.Data.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Cliente, ClientDto>().ReverseMap();
        CreateMap<Producto, ProductDto>().ReverseMap();
        CreateMap<FuenteDatos, FuenteDatosDto>().ReverseMap();

        // Opiniones: los FK (IdCliente, IdProducto, IdFuente, IdClasificacion) vienen
        // como texto o fuera de rango, así que se ignoran aquí y se resuelven en el
        // servicio (ResolveForeignKeysAsync). IdOpinion es identity -> se ignora.
        CreateMap<SocialCommentDto, Opinion>()
            .ForMember(d => d.IdOpinion, o => o.Ignore())
            .ForMember(d => d.IdCliente, o => o.Ignore())
            .ForMember(d => d.IdProducto, o => o.Ignore())
            .ForMember(d => d.IdFuente, o => o.Ignore())
            .ForMember(d => d.IdClasificacion, o => o.Ignore())
            .ForMember(d => d.PuntajeSatisfaccion, o => o.Ignore());

        CreateMap<WebReviewDto, Opinion>()
            .ForMember(d => d.IdOpinion, o => o.Ignore())
            .ForMember(d => d.IdCliente, o => o.Ignore())
            .ForMember(d => d.IdProducto, o => o.Ignore())
            .ForMember(d => d.IdFuente, o => o.Ignore())
            .ForMember(d => d.IdClasificacion, o => o.Ignore())
            .ForMember(d => d.PuntajeSatisfaccion, o => o.MapFrom(s => (byte?)s.Rating));

        CreateMap<SurveyDto, Opinion>()
            .ForMember(d => d.IdOpinion, o => o.Ignore())
            .ForMember(d => d.IdCliente, o => o.Ignore())
            .ForMember(d => d.IdProducto, o => o.Ignore())
            .ForMember(d => d.IdFuente, o => o.Ignore())
            .ForMember(d => d.IdClasificacion, o => o.Ignore());

        // Tablas de catálogo: se derivan del texto repetido dentro de otros CSV
        CreateMap<ProductDto, Categoria>()
            .ForMember(d => d.Nombre, o => o.MapFrom(s => s.Categoria));

        CreateMap<FuenteDatosDto, TipoFuente>()
            .ForMember(d => d.Nombre, o => o.MapFrom(s => s.TipoFuente));

        CreateMap<SurveyDto, Clasificacion>()
            .ForMember(d => d.Nombre, o => o.MapFrom(s => s.Clasificacion));
    }
}
