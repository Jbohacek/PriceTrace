using PriceTrace.Common.Core;
using PriceTrace.Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceTrace.Common.LogsEntities;

[Table("tbLogs",Schema = "Logging")]
public class LogEntity : BaseEntity
{
    public LogLevelEnum Level { get; set; }

    public required string Name { get; set; }

    public required string Originator { get; set; }

    public required string Message { get; set; }
}