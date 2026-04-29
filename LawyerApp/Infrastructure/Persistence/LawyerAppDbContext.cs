using LawyerApp.Domain.Aggregates.DocumentAggregate;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LawyerApp.Infrastructure.Persistence
{
    public class LawyerAppDbContext : DbContext
    {
        public LawyerAppDbContext(DbContextOptions<LawyerAppDbContext> options) : base(options) { }

        // Aggregates
        public DbSet<User> Users { get; set; }
        public DbSet<LegalProcess> LegalProcesses { get; set; }
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH Configuration for User Aggregate
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Lawyer>("Lawyer")
                .HasValue<LegalAssistant>("Assistant")
                .HasValue<Client>("Client");

            // LegalProcess Configuration
            modelBuilder.Entity<LegalProcess>(entity =>
            {
                entity.HasKey(e => e.ProcessId);
                entity.Property(e => e.ProcessId).ValueGeneratedNever(); // GUID generated in Domain
            });

            // Document Configuration
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.DocumentId);
            });
        }
    }
}