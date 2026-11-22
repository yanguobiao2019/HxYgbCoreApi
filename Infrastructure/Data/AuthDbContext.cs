using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext()
        {
        }
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<AuthorizationCode> AuthorizationCodes { get; set; } = null!;
        public DbSet<Scope> Scopes { get; set; } = null!;
        public DbSet<UserConsent> UserConsents { get; set; } = null!;
        public DbSet<RevokedToken> RevokedTokens { get; set; } = null!;
        public DbSet<Test> Tests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 配置用户实体
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Roles).IsRequired().HasMaxLength(200);
            });

            // 配置客户端实体
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ClientId).IsUnique();
                entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ClientSecret).IsRequired();
                entity.Property(e => e.ClientName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.RedirectUris).IsRequired();
                entity.Property(e => e.AllowedGrantTypes).IsRequired();
                entity.Property(e => e.AllowedScopes).IsRequired();
            });
            // 配置刷新令牌实体
            modelBuilder.Entity<Tokens>(entity =>
            {
                entity.ToTable("Tokens");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.Scopes).HasMaxLength(200);
                entity.Property(e => e.JwtId).HasMaxLength(100);
            });
            // 配置刷新令牌实体
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.Scopes).HasMaxLength(200);
                entity.Property(e => e.JwtId).HasMaxLength(100);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Client)
                    .WithMany(c => c.RefreshTokens)
                    .HasForeignKey(e => e.ClientId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 配置授权码实体
            modelBuilder.Entity<AuthorizationCode>(entity =>
            {
                entity.ToTable("AuthorizationCodes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired();
                entity.Property(e => e.RedirectUri).IsRequired();
                entity.Property(e => e.Scopes).HasMaxLength(200);
                entity.Property(e => e.CodeChallenge).HasMaxLength(100);
                entity.Property(e => e.CodeChallengeMethod).HasMaxLength(20);
                entity.Property(e => e.Nonce).HasMaxLength(100);
                entity.Property(e => e.State).HasMaxLength(200);

                entity.HasOne(e => e.Client)
                    .WithMany(c => c.AuthorizationCodes)
                    .HasForeignKey(e => e.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuthorizationCodes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置作用域实体
            modelBuilder.Entity<Scope>(entity =>
            {
                entity.ToTable("Scopes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // 配置用户同意记录实体
            modelBuilder.Entity<UserConsent>(entity =>
            {
                entity.ToTable("UserConsents");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ClientId }).IsUnique();
                entity.Property(e => e.Scopes).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Client)
                    .WithMany()
                    .HasForeignKey(e => e.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置已撤销 Token 实体（JWT 黑名单）
            modelBuilder.Entity<RevokedToken>(entity =>
            {
                entity.ToTable("RevokedTokens");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TokenId).IsUnique();
                entity.HasIndex(e => e.TokenHash);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.Property(e => e.TokenId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Reason).HasMaxLength(200);
            });
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            string connStr = "Server=.;Database=HxYgbDb;Uid=sa;Pwd=850415;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(connStr); // SqlServer 数据库
        }
    }
}
