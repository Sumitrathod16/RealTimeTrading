using Microsoft.EntityFrameworkCore;
using Trading.Api.Data.Entities;

namespace Trading.Api.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<TradeEntity> Trades => Set<TradeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradeEntity>(e =>
        {
            e.ToTable("Trades");
            e.HasKey(x => x.TradeId);
            e.Property(x => x.Symbol).IsRequired();
            e.Property(x => x.Side).IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.HasIndex(x => x.Timestamp);
        });
    }
}
