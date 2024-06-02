using Microsoft.EntityFrameworkCore;

namespace Coordinator.Models.Context
{
    public class DataContext(DbContextOptions<DataContext> context) : DbContext(context)
    {


        public DbSet<Node> Nodes { get; set; }
        public DbSet<NodeState> NodeStates { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>().HasData(
                new Node("Order.API") { Id = Guid.NewGuid() },
                new Node("Stock.API") { Id = Guid.NewGuid() },
                new Node("Payment.API") { Id = Guid.NewGuid() });
        }
    }
}
