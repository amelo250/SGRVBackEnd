using EmpresaModel = SGRVBackEnd.Models.Empresa.Empresa;

namespace SGRVBackEnd.Models.EmpresasScopedEntity;

public abstract class EmpresaScopedEntity : BaseEntity
{
    public int IdEmpresa { get; set; }

    public EmpresaModel Empresa { get; set; } = null!;
}