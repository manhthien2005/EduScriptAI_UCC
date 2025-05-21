using Microsoft.EntityFrameworkCore;
using EduScriptAI.Models;

namespace EduScriptAI.Data;

public class EduScriptContext : DbContext
{
    public EduScriptContext(DbContextOptions<EduScriptContext> options) : base(options)
    {
    }

    public DbSet<Script> Scripts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Script>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
