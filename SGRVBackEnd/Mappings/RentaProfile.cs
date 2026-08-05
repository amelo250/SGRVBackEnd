using AutoMapper;
using SGRVBackEnd.DTOs.Rentas;
using SGRVBackEnd.Models.Renta;

namespace SGRVBackEnd.Mappings;

public sealed class RentaProfile : Profile
{
    public RentaProfile()
    {
        CreateMap<RentaCreateDto, Renta>()
            .ForMember(d => d.FechaInicio, o => o.MapFrom(s => s.FechaInicio.UtcDateTime))
            .ForMember(d => d.FechaFin, o => o.MapFrom(s => s.FechaFin.UtcDateTime))
            .ForMember(d => d.Observaciones, o => o.MapFrom(s => Normalize(s.Observaciones)))
            .ForMember(d => d.IdRenta, o => o.Ignore())
            .ForMember(d => d.IdEmpresa, o => o.Ignore())
            .ForMember(d => d.IdReservacion, o => o.Ignore())
            .ForMember(d => d.IdEstado, o => o.Ignore())
            .ForMember(d => d.FechaEntregaReal, o => o.Ignore())
            .ForMember(d => d.PrecioPorDia, o => o.Ignore())
            .ForMember(d => d.CantidadDias, o => o.Ignore())
            .ForMember(d => d.Subtotal, o => o.Ignore())
            .ForMember(d => d.Total, o => o.Ignore())
            .ForMember(d => d.FechaCreacion, o => o.Ignore())
            .ForMember(d => d.FechaActualizacion, o => o.Ignore())
            .ForMember(d => d.IdUsuarioCreacion, o => o.Ignore())
            .ForMember(d => d.IdProveedorVehiculo, o => o.Ignore())
            .ForMember(d => d.IdAcuerdoVehiculo, o => o.Ignore())
            .ForMember(d => d.IdMoneda, o => o.Ignore())
            .ForMember(d => d.TotalMonedaLocal, o => o.Ignore())
            .ForMember(d => d.RowVersion, o => o.Ignore());

        CreateMap<RentaUpdateDto, Renta>()
            .ForMember(d => d.FechaInicio, o => o.MapFrom(s => s.FechaInicio.UtcDateTime))
            .ForMember(d => d.FechaFin, o => o.MapFrom(s => s.FechaFin.UtcDateTime))
            .ForMember(d => d.Observaciones, o => o.MapFrom(s => Normalize(s.Observaciones)))
            .ForMember(d => d.IdRenta, o => o.Ignore())
            .ForMember(d => d.IdEmpresa, o => o.Ignore())
            .ForMember(d => d.IdReservacion, o => o.Ignore())
            .ForMember(d => d.IdEstado, o => o.Ignore())
            .ForMember(d => d.FechaEntregaReal, o => o.Ignore())
            .ForMember(d => d.PrecioPorDia, o => o.Ignore())
            .ForMember(d => d.CantidadDias, o => o.Ignore())
            .ForMember(d => d.Subtotal, o => o.Ignore())
            .ForMember(d => d.Total, o => o.Ignore())
            .ForMember(d => d.FechaCreacion, o => o.Ignore())
            .ForMember(d => d.FechaActualizacion, o => o.Ignore())
            .ForMember(d => d.IdUsuarioCreacion, o => o.Ignore())
            .ForMember(d => d.IdProveedorVehiculo, o => o.Ignore())
            .ForMember(d => d.IdAcuerdoVehiculo, o => o.Ignore())
            .ForMember(d => d.IdMoneda, o => o.Ignore())
            .ForMember(d => d.TotalMonedaLocal, o => o.Ignore())
            .ForMember(d => d.RowVersion, o => o.Ignore());

        CreateMap<Renta, RentaResponseDto>()
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)))
            .ForMember(d => d.ClienteNombre, o => o.Ignore())
            .ForMember(d => d.VehiculoDescripcion, o => o.Ignore())
            .ForMember(d => d.EstadoCodigo, o => o.Ignore())
            .ForMember(d => d.EstadoNombre, o => o.Ignore())
            .ForMember(d => d.MonedaCodigo, o => o.Ignore())
            .ForMember(d => d.MonedaSimbolo, o => o.Ignore());
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
