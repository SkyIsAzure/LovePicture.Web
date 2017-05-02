using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LovePicture.Model.Models
{
    public partial class LovePicture_DbContext : DbContext
    {
        public virtual DbSet<ToContent> ToContent { get; set; }
        public virtual DbSet<ToContentFiles> ToContentFiles { get; set; }
        public virtual DbSet<ToModule> ToModule { get; set; }
        public virtual DbSet<ToUserInfo> ToUserInfo { get; set; }
        public virtual DbSet<ToUserLog> ToUserLog { get; set; }

        public LovePicture_DbContext(DbContextOptions<LovePicture_DbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ToContent>(entity =>
            {
                entity.ToTable("To_Content");

                entity.Property(e => e.CreateTime)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getdate()");

                entity.Property(e => e.Des).HasMaxLength(500);

                entity.Property(e => e.MaxPic)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.MinPic).HasMaxLength(200);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.ReadNum).HasDefaultValueSql("0");

                entity.Property(e => e.ZanNum).HasDefaultValueSql("0");

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.ToContent)
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_To_Content_To_Module");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ToContent)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_To_Content_To_UserInfo");
            });

            modelBuilder.Entity<ToContentFiles>(entity =>
            {
                entity.ToTable("To_Content_Files");

                entity.Property(e => e.MaxPic)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.MinPic).HasMaxLength(200);

                entity.Property(e => e.ZanNum).HasDefaultValueSql("0");

                entity.HasOne(d => d.Content)
                    .WithMany(p => p.ToContentFiles)
                    .HasForeignKey(d => d.ContentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_To_Content_Files_To_Content");
            });

            modelBuilder.Entity<ToModule>(entity =>
            {
                entity.ToTable("To_Module");

                entity.Property(e => e.CreateTime)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getdate()");

                entity.Property(e => e.Des)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.SortNum).HasDefaultValueSql("0");
            });

            modelBuilder.Entity<ToUserInfo>(entity =>
            {
                entity.ToTable("To_UserInfo");

                entity.Property(e => e.Addr).HasMaxLength(200);

                entity.Property(e => e.Birthday).HasMaxLength(20);

                entity.Property(e => e.Blog).HasMaxLength(200);

                entity.Property(e => e.CreateTime)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getdate()");

                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.HeadPhoto).HasMaxLength(200);

                entity.Property(e => e.Introduce).HasMaxLength(200);

                entity.Property(e => e.Ips).HasMaxLength(50);

                entity.Property(e => e.LevelNum).HasDefaultValueSql("0");

                entity.Property(e => e.LoginTime)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getdate()");

                entity.Property(e => e.NickName).HasMaxLength(20);

                entity.Property(e => e.Sex).HasDefaultValueSql("0");

                entity.Property(e => e.Tel).HasMaxLength(20);

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UserPwd)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ToUserLog>(entity =>
            {
                entity.ToTable("To_UserLog");

                entity.Property(e => e.CodeId).HasDefaultValueSql("0");

                entity.Property(e => e.CreateTime)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getdate()");

                entity.Property(e => e.Des)
                    .IsRequired()
                    .HasColumnType("text");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ToUserLog)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_To_UserLog_To_UserInfo");
            });
        }
    }
}