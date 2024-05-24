using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using BuurtApplicatie.Areas.Identity.Data;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using BuurtApplicatie.Controllers;
using BuurtApplicatie.Models;
using BuurtApplicatie.Models.PostOverviewViewModels;
using BuurtApplicatie.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;
using Tests.Mocks;

namespace Tests
{
    public class PostsControllerTests : IDisposable
    {
        private readonly InMemoryDbHelper _inMemoryDbHelper;
        private readonly BuurtApplicatieDbContext _context;

        public PostsControllerTests()
        {
            _inMemoryDbHelper = new InMemoryDbHelper();
            _context = _inMemoryDbHelper.GetNewInMemoryDatabase(true);
        }
        
        public void Dispose()
        {
            _context.Dispose();
        }

        private PostsController GetControllerSetupForTests(int userManagerReturnsUserWithId = 99, bool userManagerReturnsInvalidUser = false)
        {
            var user = _inMemoryDbHelper.CreateUser(_context, userManagerReturnsUserWithId, "testUser");
            
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, $"{user.Id}", null)
            }));
            
            user = userManagerReturnsInvalidUser ? null : user;
            
            var mockObjectValidator = new Mock<IObjectModelValidator>();
            mockObjectValidator.Setup(o => o.Validate(It.IsAny<ActionContext>(),
                It.IsAny<ValidationStateDictionary>(),
                It.IsAny<string>(),
                It.IsAny<object>()));
            
            var mockUserManager = MockUserManager.GetMockUserManager();
            
            mockUserManager.Setup(_ => _.GetUserId(mockUser)).Returns(user?.Id);
            mockUserManager.Setup(_ => _.GetUserAsync(mockUser).Result).Returns(user);
            
            var mockFileHelper = new Mock<FakeFileHelper>(_inMemoryDbHelper.GetNewInMemoryDatabase(false));
            return new PostsController(_context, mockUserManager.Object, mockFileHelper.Object)
            {
                ObjectValidator = mockObjectValidator.Object,
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = mockUser
                    }
                }
            };
        }
        
        [Fact]
        public async void PostOverviewReturnsOpenPostsOnly()
        {
            var c = GetControllerSetupForTests();
            var result = Assert.IsType<ViewResult>(await c.Index(null, null, 0, 
                new string[]{}, new string[]{}, false, null, null));
            var model = Assert.IsType<PostOverviewViewModel>(result.Model);
            
            Assert.All(model.Posts, vm => Assert.True(vm.Post.IsOpen));
        }
        
        [Fact]
        public async void CreateValidPost()
        {
            var c = GetControllerSetupForTests();
            var postsInDb = _context.Posts.Count();
            
            var result = Assert.IsType<RedirectToActionResult>(await c.Create(new Post {Title = "CreatedPost", Content = "The Post Content"}));
            Assert.Null(result.ControllerName);
            Assert.Equal("Index", result.ActionName);

            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            Assert.Equal(postsInDb + 1, cleanContext.Posts.Count());
        }
        
        [Fact]
        public async void CreateValidPostWithInvalidFile()
        {
            var c = GetControllerSetupForTests();
            var postsInDb = _context.Posts.Count();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(_ => _.FileName).Returns("test.pdf");

            var post = new Post {Title = "CreatedPost", Content = "The Post Content"};
            var result = Assert.IsType<ViewResult>(await c.Create(post, false, mockFile.Object));
            
            Assert.Null(result.ViewName); // Create() returns the same view when errored, so ViewName is null
            Assert.Equal(post, result.Model);
            Assert.Single(result.ViewData.ModelState);
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            Assert.Equal(postsInDb, cleanContext.Posts.Count());
        }

        [Fact]
        public async void ModeratorsCanClosePosts()
        {
            var openPost = new Post {Title = "I'm open", Content = "Whatevers"};
            _context.Posts.Add(openPost);
            _context.SaveChanges();
            
            var c = GetControllerSetupForTests();
            var result = Assert.IsType<RedirectToActionResult>(await c.Close(openPost.Id));
            
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var cleanPost = cleanContext.Posts.Find(openPost.Id);
            Assert.False(cleanPost.IsOpen);
            Assert.Null(result.ControllerName);
            Assert.Equal("Details", result.ActionName);
        }

        [Fact]
        public async void PostViewIncrements()
        {
            var user = _inMemoryDbHelper.CreateUser(_context, 1000, "user");
            var author = _inMemoryDbHelper.CreateUser(_context, 1001, "author");
            var post = new Post {Author = author, Content = "Post Content", Title = "ViewCountTest", IsOpen = true};
            _context.Posts.Add(post);
            _context.SaveChanges();

            var postViewCount = _context.UserPostStats.Count(ups => ups.PostId == post.Id && ups.IsViewed);
            
            var c = GetControllerSetupForTests(1000);
            
            Assert.Null(_context.UserPostStats.FirstOrDefault(
                ups => ups.UserId == user.Id && ups.PostId == post.Id));
            
            await c.Details(post.Id);
            
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var postStats = cleanContext.UserPostStats.FirstOrDefault(
                ups => ups.UserId == user.Id && ups.PostId == post.Id);
            Assert.NotNull(postStats); // This should be created when a post is viewed for the first time
            Assert.True(postStats.IsViewed);
            Assert.Equal(postViewCount + 1, cleanContext.UserPostStats.Count(
                ups => ups.PostId == post.Id && ups.IsViewed));

            // Ensure that viewing the same post multiple times does not increment the counter by more than 1 
            await c.Details(post.Id);
            Assert.Equal(postViewCount + 1, cleanContext.UserPostStats.Count(
                ups => ups.PostId == post.Id && ups.IsViewed));
        }

        [Fact]
        public async void LikeButton()
        {
            var user = _inMemoryDbHelper.CreateUser(_context, 1000, "user");
            var post = new Post {Content = "Post Content", Title = "LikeButtonTest", IsOpen = true };
            _context.Posts.Add(post);
            _context.SaveChanges();

            var postLikeCount = _context.UserPostStats.Count(ups => ups.UserId == user.Id && 
            ups.PostId == post.Id && ups.IsFavorited);

            var c = GetControllerSetupForTests(1000);

            Assert.Null(_context.UserPostStats.FirstOrDefault(ups => ups.UserId == user.Id && 
            ups.PostId == post.Id));

            await c.LikePost(post.Id);

            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var postStats = cleanContext.UserPostStats.FirstOrDefault(
                ups => ups.UserId == user.Id && ups.PostId == post.Id);
            Assert.NotNull(postStats); 
            Assert.True(postStats.IsFavorited);
            Assert.Equal(postLikeCount + 1, cleanContext.UserPostStats.Count(
                ups => ups.PostId == post.Id && ups.IsFavorited));
            
            await c.LikePost(post.Id);
            Assert.Equal(postLikeCount, cleanContext.UserPostStats.Count(
                ups => ups.PostId == post.Id && ups.IsFavorited));
        }
        
        [Fact]
        public async void CreateComment()
        {
            var user = _inMemoryDbHelper.CreateUser(_context, 1000, "user");
            var post = new Post { Content = "Post Content", Title = "CreateCommentTest", IsOpen = true };
            _context.Posts.Add(post);
            _context.SaveChanges();

            var postCommentCount = _context.Comments.Count();

            var c = GetControllerSetupForTests(1000);

            var result = Assert.IsType<RedirectToActionResult>(await c.CreateComment("ik ben te cool voor deze wijk", post.Id));
            Assert.Null(result.ControllerName);
            Assert.Equal("Details", result.ActionName);

            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var postComment = cleanContext.Comments.SingleOrDefault(c=>c.Content == "ik ben te cool voor deze wijk");
            Assert.NotNull(postComment);
            Assert.Equal(post.Id, postComment.PostId);
            Assert.Equal(postCommentCount + 1, cleanContext.Comments.Count());
        }

        [Fact]
        public async void MyPosts_censors_anonymous_posts()
        {
            // Setup
            var c = GetControllerSetupForTests(3565);
            var user = _context.Users.Find("3565");
            
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            
            var anonUser = new AnonymousUser {Id = "1", UserId = user.Id};
            _context.AnonUsers.Add(anonUser);
            
            var publicPosts = Enumerable.Range(1, 4).Select(_ => new Post
            {
                Author = user, Title = "A Title", Content = "The Post Content", CreatedAt = new DateTime(2021, 01, 01)
            }).ToList();
            var anonPosts = Enumerable.Range(1, 4).Select(_ => new AnonymousPost
            {
                AnonUser = anonUser, Title = "I am anonymous", Content = "You can not see me", CreatedAt = new DateTime(2021, 01, 01)
            }).ToList();
            _context.Posts.AddRange(publicPosts);
            _context.AnonPosts.AddRange(anonPosts);
            _context.SaveChanges();

            // Test
            var result = Assert.IsType<ViewResult>(await c.MyPosts());
            var model = Assert.IsType<MyPostsViewModel>(result.Model);
            Assert.Equal(publicPosts.Count + anonPosts.Count, model.AllPosts.Count());
            Assert.All(model.AllPosts, 
                p => Assert.False(p.Title == "I am anonymous" || p.Content == "You can not see me"));
        }

        [Fact]
        public async void CreateAnonPost_is_untraceable_to_user_id()
        {
            // Setup
            var c = GetControllerSetupForTests(7000);
            var user = _context.Users.Find("7000");
            var post = new Post {Title = "Big issue", Content = "Massive drug distribution"};
            
            var result = Assert.IsType<RedirectToActionResult>(await c.Create(post, true));
            Assert.Null(result.ControllerName);
            Assert.Equal("Index", result.ActionName);
            
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var newlyCreatedAnonPost = cleanContext.AnonPosts.First(p => p.Content == post.Content);
            Assert.NotEqual(user.Id, newlyCreatedAnonPost.AnonUserId);
        }
    }
}