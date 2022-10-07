using MachineDataApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MachineDataApi.Db
{
    public class MachineDataDbContext : DbContext
    {
        public MachineDataDbContext(DbContextOptions options)
            : base(options)
        {

        }

        public DbSet<Machine> Machines { get; set; }

        public DbSet<MachineData> MachineData { get; set; }
    }
}
