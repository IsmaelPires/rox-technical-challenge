namespace Rox.FinancialControl.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
