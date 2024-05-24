using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuurtApplicatie
{
    public static class InitializeDatabase
    {
        public static async Task EnsureImportantContentIsCreated(IServiceProvider services)
        {
            await using var context = new BuurtApplicatieDbContext(services.GetRequiredService<
                    DbContextOptions<BuurtApplicatieDbContext>>());

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<BuurtApplicatieUser>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            EnsureCategoriesAreCreated(context, configuration);
            await EnsureModeratorRoleExists(roleManager);
            await EnsuresModeratorUserIsCreated(userManager, context);
        }

        private static async Task EnsuresModeratorUserIsCreated(
            UserManager<BuurtApplicatieUser> userManager,
            BuurtApplicatieDbContext ctx)
        {
            var createdUser = _seedUser(ctx, "Moderator","mod@3d.com", "moderator");
            var untrackedUser = await userManager.FindByIdAsync(createdUser.Id);
            if (!await userManager.IsInRoleAsync(untrackedUser, "Moderator"))
            {
                await userManager.AddToRoleAsync(untrackedUser, "Moderator");   
            }
        }
        
        private static async Task EnsureModeratorRoleExists(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Moderator"))
            {
                await roleManager.CreateAsync(new IdentityRole("Moderator"));
            }     
        }

        private static void EnsureCategoriesAreCreated(BuurtApplicatieDbContext ctx, IConfiguration configuration)
        {
            var selection = configuration.GetSection("PostCategories");
            if (!selection.Exists())
                throw new SystemException("Section PostCategories does not exist");
            
            var categoryNames = selection.GetChildren().Select(c => c.Value);
            foreach (var name in categoryNames)
            {
                if (!ctx.Categories.Any(c => c.Name == name))
                {
                    ctx.Categories.Add(new Category {Name = name});
                }
            }
            ctx.SaveChanges();
        }
        
        public static void Seed(BuurtApplicatieDbContext context)
        {
            _seedUsers(context);
            _seedPosts(context);
            _seedComments(context);
            _seedPostStats(context);
            
            context.SaveChanges();
        }

        private static void _seedUsers(BuurtApplicatieDbContext ctx)
        {
            _seedUser(ctx, "admin@3d.com", "admin");
            _seedUser(ctx, "john@3d.com", "password");
        }

        private static BuurtApplicatieUser _seedUser(BuurtApplicatieDbContext ctx,
            string username, string password)
        {
            return _seedUser(ctx, username, username, password);
        }

        private static BuurtApplicatieUser _seedUser(BuurtApplicatieDbContext ctx, 
            string username, string email, string password)
        {
            var user = ctx.Users.AsNoTracking().FirstOrDefault(u => u.Email == email);
            if (user != null) return user;
            
            user = new BuurtApplicatieUser
            {
                UserName = username,
                Email = email,
                NormalizedEmail = email.ToUpper(),
                NormalizedUserName = username.ToUpper(),
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            var hasher = new PasswordHasher<BuurtApplicatieUser>();
            user.PasswordHash = hasher.HashPassword(user, password);
            var userStore = new UserStore<BuurtApplicatieUser>(ctx);
            userStore.CreateAsync(user).Wait();
            return user;
        }
        
        private static void _seedPosts(BuurtApplicatieDbContext ctx)
        {
            // TODO: Seed post images
            var author = ctx.Users.FirstOrDefault(u => u.UserName == "john@3d.com");
            var adminAuthor = ctx.Users.FirstOrDefault(u => u.UserName == "admin@3d.com");
            if (author == null || 
                adminAuthor == null || 
                ctx.Posts.Any(c => c.Title.StartsWith("Test3D-")) ||
                !ctx.Categories.Any()) return;

            var categories = ctx.Categories.ToList();
            var posts = new List<Post>();
            
            var post1 = new Post
            {
                Title = "Test3D-Prullenbak vernield",
                Author = author,
                Category = categories.ElementAtOrDefault(0),
                Content = "In het Vondelpark is er door een vuurwerkbom een prullenbak vernield.",
                CreatedAt = DateTime.Now,
                IsOpen = true
            };

            var post2 = new Post
            {
                Title = "Test3D-Poging tot inbraak aan de Lelylaan",
                Author = adminAuthor,
                Category = categories.ElementAtOrDefault(1),
                Content = "Om 23:20 op maandag 26 september zag ik een verdachte man in donkere kleding het huis" +
                          " op Lelylaan 26 inspecteren. Vermoedelijk was hij van plan om in te breken. Ik heb tien " +
                          "minuten lang gekeken hoe hij rond het huis liep. Waarschijnlijk drong het tot hem door " +
                          "dat een inbraak niet ging lukken, hij is weggelopen.",
                CreatedAt = DateTime.Now,
                IsOpen = true
            };

            var post3 = new Post
            {
                Title = "Test3D-Losliggende stoeptegels",
                Author = author,
                Category = categories.ElementAtOrDefault(2),
                Content = "De wortels van verschillende bomen naast de Boustraat zorgen ervoor dat de stoeptegels " +
                          "op het voetpad los zijn komen te liggen.",
                CreatedAt = DateTime.Now,
                IsOpen = true
            };
            
            posts.AddRange(new []{ post1, post2, post3});

            
            for (var i = 0; i < 30; i++)
            {
                var r = new Random();
                posts.Add(new Post
                {
                    Title = $"Test3D-{LoremGenerator.GetRandomWords(3)}",
                    Author = r.Next(2) == 0 ? author : adminAuthor,
                    Category = categories.ElementAtOrDefault(r.Next(5)),
                    Content = LoremGenerator.GetRandomWords(100, 15),
                    CreatedAt = DateTime.Now.AddDays(-i),
                    IsOpen = true,
                });
            }

            ctx.Posts.AddRange(posts);
            ctx.SaveChanges();
        }
        
        /* Adds comments to the first three posts */
        private static void _seedComments(BuurtApplicatieDbContext ctx)
        {
            var authorJohn = ctx.Users.FirstOrDefault(u => u.UserName == "john@3d.com");
            var authorAdmin = ctx.Users.FirstOrDefault(u => u.UserName == "admin@3d.com");
            if (authorJohn == null || authorAdmin == null || ctx.Comments.Any()) return;
            
            var postTitles = new[]
            {
                "Test3D-Prullenbak vernield", 
                "Test3D-Poging tot inbraak aan de Lelylaan",
                "Test3D-Losliggende stoeptegels"
            };
            var r = new Random();
            foreach (var title in postTitles)
            {
                var post = ctx.Posts.FirstOrDefault(p => p.Title == title);
                if (post == null) continue;

                var comment = new Comment
                {
                    Author = r.Next(2) == 0 ? authorJohn : authorAdmin,
                    Post = post,
                    CreatedAt = DateTime.Now,
                    Content =
                        "Dit is een test comment. Er moet echt iets aan dit probleem gedaan worden," +
                        " mijn kinderen beginnen wanhopig te worden!"
                };

                ctx.Comments.Add(comment);
            }
            ctx.SaveChanges();
        }
        
        private static void _seedPostStats(BuurtApplicatieDbContext ctx)
        {
            var authorJohn = ctx.Users.FirstOrDefault(u => u.UserName == "john@3d.com");
            var authorAdmin = ctx.Users.FirstOrDefault(u => u.UserName == "admin@3d.com");
            if (authorJohn == null || authorAdmin == null || !ctx.Posts.Any()) return;

            var postTitles = new[]
            {
                "Test3D-Prullenbak vernield", 
                "Test3D-Poging tot inbraak aan de Lelylaan",
                "Test3D-Losliggende stoeptegels"
            };
            
            foreach (var title in postTitles)
            {
                var post = ctx.Posts.FirstOrDefault(p => p.Title == title);
                if (post == null || ctx.UserPostStats.Any(ups => ups.PostId == post.Id)) continue;
                
                var userPostStat = new UserPostStats
                {
                    User = post.Author.UserName == authorJohn.UserName ? authorAdmin : authorJohn,
                    Post = post,
                    IsFavorited = true,
                    IsViewed = true
                };
                ctx.Add(userPostStat);
            }
            ctx.SaveChanges();
        }

        private static class LoremGenerator
        {        
            private static readonly string[] LoremWords =
            {
                "nulla", "facilisi", "cras", "fermentum", "odio", "eu", "feugiat", "pretium", "nibh", "ipsum",
                "consequat", "nisl", "vel", "pretium", "lectus", "quam", "id", "leo", "in", "vitae", "turpis", "massa",
                "sed", "elementum", "tempus", "egestas", "sed", "sed", "risus", "pretium", "quam", "vulputate",
                "dignissim", "suspendisse", "in", "est", "ante", "in", "nibh", "mauris", "cursus", "mattis", "molestie",
                "a", "iaculis", "at", "erat", "pellentesque", "adipiscing", "commodo", "elit", "at", "imperdiet", "dui",
                "accumsan", "sit", "amet", "nulla", "facilisi", "morbi", "tempus", "iaculis", "urna", "id", "volutpat",
                "lacus", "laoreet", "non", "curabitur", "gravida", "arcu", "ac", "tortor", "dignissim", "convallis",
                "aenean", "et", "tortor", "at", "risus"
            };
            
            private static readonly Random Random = new Random();
            
            private static string GetRandomWord()
            {
                return LoremWords.ElementAt(Random.Next(LoremWords.Length));
            }

            public static string GetRandomWords(int max, int min = 1)
            {
                var words = new List<string>();
                for (int i = 0; i < max; i++)
                {
                    if (Random.Next(min, max) == i) break;
                    words.Add(GetRandomWord());
                }

                return string.Join(' ', words);
            }
        }
    }
}