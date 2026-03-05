namespace SGPP.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaModificacion { get; set; }
}
