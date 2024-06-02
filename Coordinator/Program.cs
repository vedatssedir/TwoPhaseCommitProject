using Coordinator.Models.Context;
using Coordinator.Services;
using Coordinator.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddHttpClient("OrderAPI", client => client.BaseAddress = new Uri("https://localhost:7127/"));
builder.Services.AddHttpClient("StockAPI", client => client.BaseAddress = new Uri("https://localhost:7248/"));
builder.Services.AddHttpClient("PaymentAPI", client => client.BaseAddress = new Uri("https://localhost:7298/"));

builder.Services.AddTransient<ITransactionService, TransactionService>();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/create-order-transaction", async (ITransactionService transactionService) =>
{
    var transactionId = await transactionService.CreateTransaction();
    await transactionService.PrepareServices(transactionId);
    var transactionState = await transactionService.CheckReadyServices(transactionId);
    if (transactionState)
    {
        await transactionService.Commit(transactionId);
        transactionState = await transactionService.CheckTransactionStateServices(transactionId);
    }
    if (!transactionState)
    {
        await transactionService.RollBack(transactionId);
    }
}).WithName("GetWeatherForecast");
app.Run();
