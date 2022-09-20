using MachineDataApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MachineDataApi.Db
{
    public class MachineDataConfiguration : IEntityTypeConfiguration<MachineData>
    {
        public void Configure(EntityTypeBuilder<MachineData> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}
