using PriceTrace.Common.LogsEntities;
using PriceTrace.DataEf.Repositories.LogsRepositories.Interfaces;

namespace PriceTrace.DataEf.Repositories.Logs;

public class LogRepository : Repository<LogEntity>, ILogRepository
{
    public LogRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
