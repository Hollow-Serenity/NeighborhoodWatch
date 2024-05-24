using System;
using System.Collections.Generic;
using System.Linq;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Tests
{
    public class InMemoryDbHelper
    {
        private string _databaseName; // used to create clean contexts

        public BuurtApplicatieDbContext GetInMemoryDbWithData() {
            var context = GetNewInMemoryDatabase(true);
            var user1 = CreateUser(context, 1, "Piet");
            var user2 = CreateUser(context, 2, "Henk");
            context.Add(new Post { Id = 1, Title = "OpenPost", Content = "Hello I am a post", 
                IsOpen = true, 
                Author = user1,
                CreatedAt = DateTime.Now,
                UserPostStats = new List<UserPostStats>()});
            context.Add(new Post { Id = 2, Title = "ClosedPost", Content = "Post which should not be seen", CreatedAt = DateTime.Now, IsOpen = false, Author = user2, UserPostStats = new List<UserPostStats>()});
            context.SaveChanges();
            return GetNewInMemoryDatabase(false); // use a new (clean) object for the context
        }

        public BuurtApplicatieDbContext GetNewInMemoryDatabase(bool newDb)
        {
            if (newDb) _databaseName = Guid.NewGuid().ToString(); // unique name
        
            var options = new DbContextOptionsBuilder<BuurtApplicatieDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            return new BuurtApplicatieDbContext(options);
        }
        
        public BuurtApplicatieUser CreateUser(BuurtApplicatieDbContext ctx, int id, string username)
        {
            var user = ctx.Users.Find(id.ToString());
            if (user != null) return user;
            
            user = new BuurtApplicatieUser
            {
                Id = id.ToString(),
                UserName = username,
                Email = username,
                NormalizedEmail = username.ToUpper(),
                NormalizedUserName = username.ToUpper(),
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            var hasher = new PasswordHasher<BuurtApplicatieUser>();
            user.PasswordHash = hasher.HashPassword(user, "password");
            var userStore = new UserStore<BuurtApplicatieUser>(ctx);
            userStore.CreateAsync(user).Wait();
            return user;
        }
    }
}