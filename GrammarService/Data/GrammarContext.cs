using GrammarService.Models;
using Microsoft.EntityFrameworkCore;

namespace GrammarService.Data
{
    public class GrammarContext : DbContext
    {
        public GrammarContext(DbContextOptions<GrammarContext> options) : base(options)
        {
        }

        public DbSet<Grammar> Grammars { get; set; }
        public DbSet<Production> Productions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar relación Grammar -> Productions
            modelBuilder.Entity<Grammar>()
                .HasMany(g => g.Productions)
                .WithOne(p => p.Grammar)
                .HasForeignKey(p => p.GrammarId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices para mejorar rendimiento
            modelBuilder.Entity<Production>()
                .HasIndex(p => p.GrammarId);

            modelBuilder.Entity<Production>()
                .HasIndex(p => p.NonTerminal);
        }
    }
}