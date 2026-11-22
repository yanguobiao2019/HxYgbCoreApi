using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ScaffoldData.Models.SerilogDemo;

namespace ScaffoldData.Context;

public partial class SerilogDemoContext : DbContext
{
    public SerilogDemoContext(DbContextOptions<SerilogDemoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Log>(entity =>
        {
            entity.Property(e => e.Level).HasMaxLength(16);
            entity.Property(e => e.MachineName).HasMaxLength(255);
            entity.Property(e => e.TimeStamp).HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Account).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Pwd).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
