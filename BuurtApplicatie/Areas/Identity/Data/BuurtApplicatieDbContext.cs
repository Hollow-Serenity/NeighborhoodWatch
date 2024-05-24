using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BuurtApplicatie.Areas.Identity.Data
{
    public class BuurtApplicatieDbContext : IdentityDbContext<BuurtApplicatieUser>
    {
        public BuurtApplicatieDbContext() {}
        public BuurtApplicatieDbContext(DbContextOptions<BuurtApplicatieDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ReportedPost>()
                .HasKey(rp => new { rp.PostId, rp.ReportedById });
            builder.Entity<UserPostStats>()
                .HasKey(ups => new { ups.UserId, ups.PostId });
            builder.Entity<BuurtApplicatieUser>()
                .HasOne(bau => bau.Address)
                .WithOne(a => a.User)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(bau => bau.Posts)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(bau => bau.Comments)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Image>()
                .HasOne(i => i.Post)
                .WithOne(p => p.Image)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Image>()
                .HasOne(i => i.User)
                .WithOne(u => u.ProfilePicture)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<DeletedPost>()
                .HasOne(dp => dp.User)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Image>()
                .Property(i => i.Data)
                .HasColumnType("MediumBlob")
                .HasMaxLength(2097152);

            builder.Entity<AnonymousUser>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();
            
            builder.Entity<AnonymousPost>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            
            builder.Entity<AnonymousImage>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            builder.Entity<AnonymousUser>()
                .HasOne(p => p.User)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<AnonymousImage>()
                .HasOne(p => p.Post)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<AnonymousPost>()
                .HasOne(p => p.AnonUser)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull);
        }
        
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        
        public DbSet<Category> Categories { get; set; }
        public DbSet<ReportedPost> ReportedPosts { get; set; }
        public DbSet<DeletedPost> DeletedPosts { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<UserPostStats> UserPostStats { get; set; }
        public DbSet<Address> Addresses { get; set; }
        
        public DbSet<AnonymousImage> AnonImages { get; set; }
        public DbSet<AnonymousUser> AnonUsers { get; set; }
        public DbSet<AnonymousPost> AnonPosts { get; set; }
    }
}
