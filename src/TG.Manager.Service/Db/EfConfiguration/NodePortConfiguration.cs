using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Db.EfConfiguration
{
    public class NodePortConfiguration : IEntityTypeConfiguration<NodePort>
    {
        public void Configure(EntityTypeBuilder<NodePort> entity)
        {
            entity.HasKey(p => p.Port);
            entity.Property(p => p.LastUpdate)
                .IsConcurrencyToken();
        }
    }
}