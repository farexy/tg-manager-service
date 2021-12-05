using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Db.EfConfiguration
{
    public class BattleServerConfiguration : IEntityTypeConfiguration<BattleServer>
    {
        public void Configure(EntityTypeBuilder<BattleServer> entity)
        {
            entity.HasKey(bs => bs.BattleId);
            entity.HasOne(bs => bs.LoadBalancer)
                .WithOne()
                .HasForeignKey<BattleServer>(bs => bs.LoadBalancerPort);
        }
    }
}