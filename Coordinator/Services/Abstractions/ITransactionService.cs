namespace Coordinator.Services.Abstractions;

public interface ITransactionService
{
    Task<Guid> CreateTransaction();
    Task PrepareServices(Guid transactionId);
    Task<bool> CheckReadyServices(Guid transactionId);
    Task<bool> CheckTransactionStateServices(Guid transactionId);
    Task Commit(Guid transactionId);
    Task RollBack(Guid transactionId);
}