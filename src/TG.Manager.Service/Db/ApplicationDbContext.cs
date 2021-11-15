using Microsoft.EntityFrameworkCore;
using TG.Core.Db.Postgres;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Db
{
    public class ApplicationDbContext : TgDbContext
    {
        public DbSet<BattleServer> BattleServersPool { get; set; } = default!;
        
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}