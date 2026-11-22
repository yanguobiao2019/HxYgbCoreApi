using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ScaffoldData.Models;

public partial class HxykContext : DbContext
{
    public HxykContext()
    {
    }

    public HxykContext(DbContextOptions<HxykContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TSalaryDisclosure> TSalaryDisclosures { get; set; }

    public virtual DbSet<TTest> TTests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=HXYK;Trusted_Connection=True;TrustServerCertificate=true;User Id=sa;Password=850415");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TSalaryDisclosure>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("t_salary_disclosure");

            entity.Property(e => e.Age)
                .HasMaxLength(50)
                .HasColumnName("AGE");
            entity.Property(e => e.Degree)
                .HasMaxLength(50)
                .HasColumnName("DEGREE");
            entity.Property(e => e.DeptName)
                .HasMaxLength(50)
                .HasColumnName("DEPT_NAME");
            entity.Property(e => e.DwGjj)
                .HasMaxLength(20)
                .HasColumnName("DW_GJJ");
            entity.Property(e => e.DwGs)
                .HasMaxLength(20)
                .HasColumnName("DW_GS");
            entity.Property(e => e.DwMaternity)
                .HasMaxLength(20)
                .HasColumnName("DW_MATERNITY");
            entity.Property(e => e.DwSb)
                .HasMaxLength(20)
                .HasColumnName("DW_SB");
            entity.Property(e => e.DwSy)
                .HasMaxLength(20)
                .HasColumnName("DW_SY");
            entity.Property(e => e.DwYb)
                .HasMaxLength(20)
                .HasColumnName("DW_YB");
            entity.Property(e => e.Edu)
                .HasMaxLength(50)
                .HasColumnName("EDU");
            entity.Property(e => e.EmpName)
                .HasMaxLength(50)
                .HasColumnName("EMP_NAME");
            entity.Property(e => e.EmpNo)
                .HasMaxLength(50)
                .HasColumnName("EMP_NO");
            entity.Property(e => e.ErpName)
                .HasMaxLength(50)
                .HasColumnName("ERP_NAME");
            entity.Property(e => e.ErpNo)
                .HasMaxLength(5)
                .HasColumnName("ERP_NO");
            entity.Property(e => e.Gz)
                .HasMaxLength(20)
                .HasColumnName("GZ");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(50)
                .HasColumnName("JOB_TITLE");
            entity.Property(e => e.PosLevle)
                .HasMaxLength(50)
                .HasColumnName("POS_LEVLE");
            entity.Property(e => e.PosType)
                .HasMaxLength(50)
                .HasColumnName("POS_TYPE");
            entity.Property(e => e.SocialAmt)
                .HasMaxLength(20)
                .HasColumnName("SOCIAL_AMT");
            entity.Property(e => e.Yymm)
                .HasMaxLength(8)
                .HasColumnName("YYMM");
        });

        modelBuilder.Entity<TTest>(entity =>
        {
            entity.ToTable("t_test");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
