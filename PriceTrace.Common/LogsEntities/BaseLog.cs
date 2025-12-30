using PriceTrace.Common.Core;
using PriceTrace.Common.Enums;

namespace PriceTrace.Common.LogsEntities;

public class BaseLog : BaseEntity
{
    public LogLevelEnum Level { get; set; }

    public required string Name { get; set; }

    public required string Originator { get; set; }

    public required string Message { get; set; }
}