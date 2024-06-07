using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TekegramBotRent.Models;

namespace TekegramBotRent;

public partial class RentbotContext : DbContext
{
    public RentbotContext()
    {
    }

    public RentbotContext(DbContextOptions<RentbotContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Flat> Flats { get; set; }

    public virtual DbSet<Owner> Owners { get; set; }

    public virtual DbSet<Rent> Rents { get; set; }

    public virtual DbSet<SearchSession> SearchSessions { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("CONECTION_STRING"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Flat_pkey");

            entity.ToTable("Flat");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.Adress)
                .HasMaxLength(150)
                .IsFixedLength();
            entity.Property(e => e.OwnerId)
                .HasMaxLength(30)
                .IsFixedLength();
            entity.Property(e => e.Price).HasColumnType("money");
            entity.Property(e => e.Zone)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.Owner).WithMany(p => p.Flats)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("Flat_OwnerId_fkey");
        });

        modelBuilder.Entity<Owner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Owner_pkey");

            entity.ToTable("Owner");

            entity.Property(e => e.Id)
                .HasMaxLength(30)
                .IsFixedLength();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Owner)
                .HasForeignKey<Owner>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Owner_Id_fkey");
        });

        modelBuilder.Entity<Rent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Rent_pkey");

            entity.ToTable("Rent");

            entity.Property(e => e.Id)
                .HasMaxLength(30)
                .IsFixedLength();
            entity.Property(e => e.FlatId)
                .HasMaxLength(30)
                .IsFixedLength();
            entity.Property(e => e.IsCanceledOwner).HasDefaultValue(false);
            entity.Property(e => e.IsCanceledTenant).HasDefaultValue(false);
            entity.Property(e => e.IsConfirmed).HasDefaultValue(false);
            entity.Property(e => e.TenantId)
                .HasMaxLength(30)
                .IsFixedLength();

            entity.HasOne(d => d.Flat).WithMany(p => p.Rents)
                .HasForeignKey(d => d.FlatId)
                .HasConstraintName("Rent_FlatId_fkey");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Rents)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("Rent_TenantId_fkey");
        });

        modelBuilder.Entity<SearchSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SearchSession_pkey");

            entity.ToTable("SearchSession");

            entity.Property(e => e.Id)
                .HasMaxLength(30)
                .IsFixedLength();
            entity.Property(e => e.Dates)
                .HasMaxLength(30)
                .IsFixedLength();
            entity.Property(e => e.TenantId)
                .HasMaxLength(30)
                .IsFixedLength();

            entity.HasOne(d => d.Tenant).WithMany(p => p.SearchSessions)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("SearchSession_TenantId_fkey");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tenant_pkey");

            entity.ToTable("Tenant");

            entity.Property(e => e.Id)
                .HasMaxLength(30)
                .IsFixedLength();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Tenant)
                .HasForeignKey<Tenant>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Tenant_Id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("User_pkey");

            entity.ToTable("User");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Contact)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsFixedLength();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
