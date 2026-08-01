using SGRVBackEnd.Models.Empresa;

namespace SGRVBackEnd.Models.EmpresasScopedEntity
{
    public abstract class EmpresaScopedEntity : BaseEntity
    {
        public int IdEmpresa { get; set; }
        public Empresa Empresa { get; set; } = null!;
    }
}
