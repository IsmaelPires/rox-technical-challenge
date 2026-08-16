using Rox.FinancialControl.Application.Abstractions;

namespace Rox.FinancialControl.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
