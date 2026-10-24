using Aerolink_Enterprise_Solution.Models;
using Microsoft.EntityFrameworkCore;

namespace Aerolink_Enterprise_Solution.Data;

public class AeroLinkDbContext : DbContext
{
    public AeroLinkDbContext(DbContextOptions<AeroLinkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Bag> Bags => Set<Bag>();

    public DbSet<BaggageException> BaggageExceptions =>
        Set<BaggageException>();

    public DbSet<LoadingCompletion> LoadingCompletions =>
        Set<LoadingCompletion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaggageException>()
            .HasKey(e => e.ExceptionId);

        modelBuilder.Entity<Bag>()
            .HasOne<Flight>()
            .WithMany()
            .HasForeignKey(b => b.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BaggageException>()
            .HasOne<Bag>()
            .WithMany()
            .HasForeignKey(e => e.BagId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BaggageException>()
            .HasOne<Flight>()
            .WithMany()
            .HasForeignKey(e => e.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoadingCompletion>()
            .HasKey(c => c.FlightId);

        modelBuilder.Entity<LoadingCompletion>()
            .Property(c => c.FlightId)
            .ValueGeneratedNever();

        modelBuilder.Entity<LoadingCompletion>()
            .HasOne<Flight>()
            .WithOne()
            .HasForeignKey<LoadingCompletion>(c => c.FlightId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}