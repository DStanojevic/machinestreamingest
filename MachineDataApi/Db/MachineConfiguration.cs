using MachineDataApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MachineDataApi.Db
{
    public class MachineConfiguration : IEntityTypeConfiguration<Machine>
    {
        public void Configure(EntityTypeBuilder<Machine> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasMany(x => x.Data)
                .WithOne()
                .HasForeignKey(x => x.MachineId);
        }
    }
}
