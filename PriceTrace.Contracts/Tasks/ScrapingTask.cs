namespace PriceTrace.Contracts.Tasks;

public class ScrapingTask
{
    public Guid SearchRequestId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string[] ProductNames { get; set; } = Array.Empty<string>();
    public int? SoftUpLimit { get; set; }
    public int? SoftDownLimit { get; set; }
    public int? HardUpLimit { get; set; }
    public int? HardDownLimit { get; set; }
    public int FleaMarketWeb { get; set; }
    public long IntervalInSeconds { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public Task? RunningTask { get; set; }
}
