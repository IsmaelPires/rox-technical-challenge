namespace Rox.FinancialControl.Application.Abstractions;

public interface IOutboxWriter
{
    Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : notnull;
}
