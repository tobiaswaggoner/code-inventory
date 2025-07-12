using Microsoft.EntityFrameworkCore;
using CodeInventory.Common.Models;

namespace CodeInventory.Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectLocation> ProjectLocations { get; set; }
    public DbSet<Commit> Commits { get; set; }
    public DbSet<Author> Authors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Author entity
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => e.Email);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Project entity
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => e.InitialCommitSha);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.InitialCommitSha).IsRequired();
            entity.HasIndex(e => e.InitialCommitSha).IsUnique();
        });

        // Configure ProjectLocation entity
        modelBuilder.Entity<ProjectLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).IsRequired();
            entity.Property(e => e.Path).IsRequired();
            entity.HasIndex(e => new { e.ProjectId, e.Path }).IsUnique();
            
            entity.HasOne(e => e.Project)
                .WithMany(e => e.Locations)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Commit entity
        modelBuilder.Entity<Commit>(entity =>
        {
            entity.HasKey(e => e.Sha);
            entity.Property(e => e.Sha).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.AuthorTimestamp).IsRequired();
            
            entity.HasOne(e => e.Author)
                .WithMany(e => e.Commits)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Project)
                .WithMany(e => e.Commits)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}