using Coordinator.Enums;
using Coordinator.Models;
using Coordinator.Models.Context;
using Coordinator.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Coordinator.Services
{
    public class TransactionService(IHttpClientFactory httpClientFactory, DataContext context) : ITransactionService
    {
        private readonly HttpClient _httpClientOrderApi = httpClientFactory.CreateClient("OrderAPI");
        private readonly HttpClient _httpClientStockApi = httpClientFactory.CreateClient("StockAPI");
        private readonly HttpClient _httpClientPaymentApi = httpClientFactory.CreateClient("PaymentAPI");


        public async Task<Guid> CreateTransaction()
        {
            var transactionId = Guid.NewGuid();
            var nodeList = await context.Nodes.ToListAsync();

            foreach (var item in nodeList)
            {
                item.NodeStates = new List<NodeState>
                {
                    new(TransactionId:transactionId)
                    {
                        IsReady = ReadyType.Pending,
                        TransactionState = TransactionState.Pending
                    }
                };
            }
            await context.SaveChangesAsync();
            return transactionId;
        }

        public async Task PrepareServices(Guid transactionId)
        {
            var transactionNodes = await context.NodeStates.Include(x => x.Node).Where(x => x.TransactionId == transactionId).ToListAsync();

            foreach (var transactionNode in transactionNodes)
            {
                try
                {
                    var response = transactionNode.Node.Name switch
                    {
                        "Order.API" => await _httpClientOrderApi.GetAsync("ready"),
                        "Stock.API" => await _httpClientStockApi.GetAsync("ready"),
                        "Payment.API" => await _httpClientPaymentApi.GetAsync("ready"),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var result = bool.Parse(await response.Content.ReadAsStringAsync());
                    transactionNode.TransactionState = result ? TransactionState.Done : TransactionState.Abort;
                }
                catch (Exception e)
                {
                    transactionNode.TransactionState = TransactionState.Abort;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task<bool> CheckReadyServices(Guid transactionId)
        {
            var result = (await context.NodeStates.Where(x => x.TransactionId == transactionId).ToListAsync()).TrueForAll(x => x.IsReady == ReadyType.Ready);
            return result;
        }
        public async Task Commit(Guid transactionId)
        {
            var transactionNodes = await context.NodeStates.Include(x => x.Node).Where(x => x.TransactionId == transactionId).ToListAsync();

            foreach (var transactionNode in transactionNodes)
            {
                try
                {
                    var response = transactionNode.Node.Name switch
                    {
                        "Order.API" => await _httpClientOrderApi.GetAsync("commit"),
                        "Stock.API" => await _httpClientStockApi.GetAsync("commit"),
                        "Payment.API" => await _httpClientPaymentApi.GetAsync("commit"),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var result = bool.Parse(await response.Content.ReadAsStringAsync());
                    transactionNode.TransactionState = result ? TransactionState.Done : TransactionState.Abort;
                }
                catch (Exception e)
                {
                    transactionNode.TransactionState = TransactionState.Abort;
                }
            }
            await context.SaveChangesAsync();
        }
        public async Task<bool> CheckTransactionStateServices(Guid transactionId)
        {
            var result = (await context.NodeStates.Where(x=>x.TransactionId==transactionId).ToListAsync()).TrueForAll(x=>x.TransactionState == TransactionState.Done);
            return result;
        }

        public async Task RollBack(Guid transactionId)
        {
            var transactionNodes = await context.NodeStates.Include(x => x.Node).Where(x => x.TransactionId == transactionId).ToListAsync();

            foreach (var transactionNode in transactionNodes)
            {
                try
                {
                    var response = transactionNode.Node.Name switch
                    {
                        "Order.API" => await _httpClientOrderApi.GetAsync("rollback"),
                        "Stock.API" => await _httpClientStockApi.GetAsync("rollback"),
                        "Payment.API" => await _httpClientPaymentApi.GetAsync("rollback"),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var result = bool.Parse(await response.Content.ReadAsStringAsync());
                    transactionNode.TransactionState = TransactionState.Abort;
                }
                catch (Exception e)
                {
                    transactionNode.TransactionState = TransactionState.Abort;
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
