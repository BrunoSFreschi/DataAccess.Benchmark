using Microsoft.EntityFrameworkCore;

namespace DataAccess.Benchmark;

public class AppDbContext : DbContext
{
    public DbSet<Cliente> Clientes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseSqlite("Data Source=benchmark.db;")
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(false);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Cliente>();

        // 🔗 Nome da tabela (igual ao seu benchmark)
        entity.ToTable("Pessoas");

        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // 🔥 Mapeando nomes corretos das colunas
        entity.Property(c => c.Name)
            .HasColumnName("Nome")
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(c => c.Mail)
            .HasColumnName("Email")
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(c => c.Activated)
            .HasColumnName("Ativo")
            .IsRequired();

        entity.Property(c => c.CreateAt)
            .HasColumnName("DataCriacao")
            .IsRequired()
            .HasColumnType("TEXT"); // SQLite padrão
    }
}