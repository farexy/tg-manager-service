using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Db.EfConfiguration
{
    public class LoadBalancerConfiguration : IEntityTypeConfiguration<NodePort>
    {
        public void Configure(EntityTypeBuilder<NodePort> entity)
        {
            entity.HasKey(lb => lb.Port);
        }
    }
}