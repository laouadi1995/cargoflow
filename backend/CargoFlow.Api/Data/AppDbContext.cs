using Microsoft.EntityFrameworkCore;
using CargoFlow.Api.Models;

namespace CargoFlow.Api.Data
{
    public class AppDbContext : DbContext
    {
        // 🔥 هذا هو الحل
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<TakeCargo> TakeCargos { get; set; }
        public DbSet<PushToken> PushTokens { get; set; }
        public DbSet<GasComplaintArchive> GasComplaintArchives { get; set; }
    }
}