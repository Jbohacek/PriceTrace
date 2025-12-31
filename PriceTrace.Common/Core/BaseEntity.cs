using System.ComponentModel.DataAnnotations;

namespace PriceTrace.Common.Core;

public class BaseEntity : IBaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; } = null;
}