using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Db.EfConfiguration
{
    public class TestBattleServerConfiguration : IEntityTypeConfiguration<TestBattleServer>
    {
        public void Configure(EntityTypeBuilder<TestBattleServer> entity)
        {
            entity.HasKey(bs => bs.BattleId);
        }
    }
}