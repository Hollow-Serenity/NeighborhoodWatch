using System;
using System.Collections.Generic;
using System.Linq;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Helpers;
using BuurtApplicatie.Models;
using BuurtApplicatie.Models.PostOverviewViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests
{
    public class PostCollatorTests : IDisposable
    {
        private readonly BuurtApplicatieDbContext _context;
        private readonly InMemoryDbHelper _inMemoryDbHelper;
        
        public PostCollatorTests()
        {
            _inMemoryDbHelper = new InMemoryDbHelper();
            _context = _inMemoryDbHelper.GetInMemoryDbWithData();
        }
        
        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public void Filter_collection_by_search_query()
        {
            var author = _inMemoryDbHelper.CreateUser(_context, 1000, "Author");
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            _context.Posts.AddRange(alphabet.Select(c => new Post
            {
                Author = author,
                Title = new string(c, 5), // returns aaaaa, bbbbb, ccccc, etc.
                Content = $"My letter is: {c}"
            }));
            _context.SaveChanges();
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            Assert.Equal(26, cleanContext.Posts.Count(p => p.Content.Contains("My letter is:")));
            
            const string query = "letter";
            var postWhichShouldBeFoundByTitle = new Post {AuthorId = "1000", Title = query, Content = "....."};
            var postWhichShouldBeFoundByContent = new Post {AuthorId = "1000", Title = ".....", Content = query};
            var postWhichShouldNotBeFound = new Post {AuthorId = "1000", Title = ".....", Content = "....."};
            _context.Posts.AddRange(
                postWhichShouldBeFoundByTitle, 
                postWhichShouldBeFoundByContent, 
                postWhichShouldNotBeFound);
            _context.SaveChanges();
            
            cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var collator = new PostCollator(cleanContext);
            
            var collated = collator.FilterQuery(
                cleanContext.Posts.AsQueryable(), query).ToList();
            
            Assert.All(collated, p => Assert.True(p.Content.Contains(query) || p.Title.Contains(query)));
            Assert.NotNull(collated.Find(p => p.Id == postWhichShouldBeFoundByTitle.Id));
            Assert.NotNull(collated.Find(p => p.Id == postWhichShouldBeFoundByContent.Id));
            Assert.Null(collated.Find(p => p.Id == postWhichShouldNotBeFound.Id));
        }

        [Fact]
        public void Sort_collection_by_likes_asc()
        {
            var unsortedNumbers = new []{200, 300, 100, 100, 5, 10};
            var unsortedCollection = unsortedNumbers
                .Select(n => new PostOverviewPostViewModel {Likes = n})
                .AsQueryable();
            
            var collator = new PostCollator(_context);
            var supposedlySortedCollection = collator
                .OrderBy(unsortedCollection, "likes_asc")
                .ToList();
            
            Assert.Collection(supposedlySortedCollection,
                item => Assert.Equal(5, item.Likes),
                item => Assert.Equal(10, item.Likes),
                item => Assert.Equal(100, item.Likes),
                item => Assert.Equal(100, item.Likes),
                item => Assert.Equal(200, item.Likes),
                item => Assert.Equal(300, item.Likes)
            );
        }
        
        [Fact]
        public void Sort_collection_by_likes_desc()
        {
            var unsortedNumbers = new []{200, 300, 100, 100, 5, 10};
            var unsortedCollection = unsortedNumbers
                .Select(n => new PostOverviewPostViewModel {Likes = n})
                .AsQueryable();
            
            var collator = new PostCollator(_context);
            var supposedlySortedCollection = collator
                .OrderBy(unsortedCollection, "likes_desc")
                .ToList();
            
            Assert.Collection(supposedlySortedCollection,
                item => Assert.Equal(300, item.Likes),
                item => Assert.Equal(200, item.Likes),
                item => Assert.Equal(100, item.Likes),
                item => Assert.Equal(100, item.Likes),
                item => Assert.Equal(10, item.Likes),
                item => Assert.Equal(5, item.Likes)
            );
        }
        
        [Fact]
        public void Sort_collection_by_views_asc()
        {
            var unsortedNumbers = new []{200, 300, 100, 100, 5, 10};
            var unsortedCollection = unsortedNumbers
                .Select(n => new PostOverviewPostViewModel {Views = n})
                .AsQueryable();
            
            var collator = new PostCollator(_context);
            var supposedlySortedCollection = collator
                .OrderBy(unsortedCollection, "views_asc")
                .ToList();
            
            Assert.Collection(supposedlySortedCollection,
                item => Assert.Equal(5, item.Views),
                item => Assert.Equal(10, item.Views),
                item => Assert.Equal(100, item.Views),
                item => Assert.Equal(100, item.Views),
                item => Assert.Equal(200, item.Views),
                item => Assert.Equal(300, item.Views)
            );
        }
        
        [Fact]
        public void Sort_collection_by_views_desc()
        {
            var unsortedNumbers = new []{200, 300, 100, 100, 5, 10};
            var unsortedCollection = unsortedNumbers
                .Select(n => new PostOverviewPostViewModel {Views = n})
                .AsQueryable();
            
            var collator = new PostCollator(_context);
            var supposedlySortedCollection = collator
                .OrderBy(unsortedCollection, "views_desc")
                .ToList();
            
            Assert.Collection(supposedlySortedCollection,
                item => Assert.Equal(300, item.Views),
                item => Assert.Equal(200, item.Views),
                item => Assert.Equal(100, item.Views),
                item => Assert.Equal(100, item.Views),
                item => Assert.Equal(10, item.Views),
                item => Assert.Equal(5, item.Views)
            );
        }
        
        [Fact]
        public void Sort_collection_by_date_asc()
        {
            var unsortedDates = new []
            {
                new DateTime(2021, 1, 20, 12, 0, 0),
                new DateTime(2021, 1, 20, 13, 30, 0),
                new DateTime(2020, 12, 25),
                new DateTime(2020, 12, 15),
                new DateTime(2021, 1, 17),
                new DateTime(2020, 6, 18)
            };
            var unsortedCollection = unsortedDates
                .Select(d => new PostOverviewPostViewModel {Post = new Post {CreatedAt = d}})
                .AsQueryable();
            
            var collator = new PostCollator(_context);
            var supposedlySortedCollection = collator
                .OrderBy(unsortedCollection, "date_asc")
                .Select(n => n.Post)
                .ToList();
            
            Assert.Collection(supposedlySortedCollection,
                item => Assert.Equal(new DateTime(2020, 6, 18), item.CreatedAt),
                item => Assert.Equal(new DateTime(2020, 12, 15), item.CreatedAt),
                item => Assert.Equal(new DateTime(2020, 12, 25), item.CreatedAt),
                item => Assert.Equal(new DateTime(2021, 1, 17), item.CreatedAt),
                item => Assert.Equal(new DateTime(2021, 1, 20, 12, 0, 0), item.CreatedAt),
                item => Assert.Equal(new DateTime(2021, 1, 20, 13, 30, 0), item.CreatedAt)
            );
        }
        
        [Fact]
        public void Sort_collection_by_date_desc()
        {
            var unsortedDates = new []
            {
                new DateTime(2021, 1, 20, 12, 0, 0),
                new DateTime(2021, 1, 20, 13, 30, 0),
                new DateTime(2020, 12, 25),
                new DateTime(2020, 12, 15),
                new DateTime(2021, 1, 17),
                new DateTime(2020, 6, 18)
            };
            var unsortedCollection = unsortedDates
                .Select(d => new PostOverviewPostViewModel {Post = new Post {CreatedAt = d}})
                .AsQueryable();
            
            var collator = new PostCollator(_context);
            var supposedlySortedCollection = collator
                .OrderBy(unsortedCollection, "date_desc")
                .Select(n => n.Post)
                .ToList();
            
            Assert.Collection(supposedlySortedCollection,
                item => Assert.Equal(new DateTime(2021, 1, 20, 13, 30, 0), item.CreatedAt),
                item => Assert.Equal(new DateTime(2021, 1, 20, 12, 0, 0), item.CreatedAt),
                item => Assert.Equal(new DateTime(2021, 1, 17), item.CreatedAt),
                item => Assert.Equal(new DateTime(2020, 12, 25), item.CreatedAt),
                item => Assert.Equal(new DateTime(2020, 12, 15), item.CreatedAt),
                item => Assert.Equal(new DateTime(2020, 6, 18), item.CreatedAt)
            );
        }

        [Fact]
        public void Filter_by_favorites_in_given_collection_only()
        {
            const int amount = 5;
            var posts = new List<Post>();
            for (var i = 1; i <= amount; i++)
            {
                posts.Add(new Post {Title = i.ToString()});
            }

            var excludedPost = new Post { Title = "I'm excluded"};
            _context.AddRange(posts);
            _context.Add(excludedPost);
            _context.SaveChanges();
            
            var user = _inMemoryDbHelper.CreateUser(_context, 5000, "TestUser");
            
            var postStats = posts.Select(p => new UserPostStats
            {
                PostId = p.Id,
                UserId = user.Id,
                IsFavorited = true
            }).ToList();
            postStats.Add(new UserPostStats {PostId = excludedPost.Id, UserId = user.Id, IsFavorited = true});
            _context.UserPostStats.AddRange(postStats);
            _context.SaveChanges();

            var collator = new PostCollator(_inMemoryDbHelper.GetNewInMemoryDatabase(false));
            var result = collator.InUsersFavorites(posts.AsQueryable(), user.Id);
            
            Assert.Equal(posts.Count, result.Count());
            Assert.DoesNotContain(excludedPost.Id, result.Select(p => p.Id));
        }
        
        [Fact]
        public void Filter_collection_by_favorites()
        {
            // Setup begin
            var user = _inMemoryDbHelper.CreateUser(_context, 5000, "TestUser");
            var author = _inMemoryDbHelper.CreateUser(_context, 5001, "author");
            var posts = Enumerable.Range(1, 8).Select(_ => new Post // Create 8 posts
            {
                Author = author, Title = "A Title", Content = "The Post Content"
            }).ToList();
            
            _context.Posts.AddRange(posts);
            _context.SaveChanges();
            
            var postIndexes = posts.Select(p => posts.IndexOf(p)).ToList();
            
            var postsIncludedInResult = postIndexes
                .Where(i => i % 2 == 0)
                .Select(i => posts[i])
                .ToList();
            
            var postsExcludedFromResult = postIndexes
                .Where(i => i % 2 != 0)
                .Select(i => posts[i]);

            var postStats = postsIncludedInResult.Select(p => new UserPostStats
            {
                PostId = p.Id,
                UserId = user.Id,
                IsFavorited = true
            }).ToList();
            _context.UserPostStats.AddRange(postStats);
            _context.SaveChanges();

            var collator = new PostCollator(_inMemoryDbHelper.GetNewInMemoryDatabase(false)); // clean context
            // Setup end
            
            var result = collator.InUsersFavorites(posts.AsQueryable(), user.Id).ToList();
            Assert.Equal(postStats.Count, result.Count());
            Assert.All(result, p =>
            {
                Assert.Contains(p.Id, posts.Select(i => i.Id));
                Assert.DoesNotContain(p.Id, postsExcludedFromResult.Select(i => i.Id));
            });
        }

        [Fact]
        public void Filter_collection_by_users_favorites_only()
        {
            // Setup begin
            var user = _inMemoryDbHelper.CreateUser(_context, 5000, "TestUser");
            var author = _inMemoryDbHelper.CreateUser(_context, 5001, "author");
            var posts = Enumerable.Range(1, 8).Select(_ => new Post // Create 8 posts
            {
                Author = author, Title = "A Title", Content = "The Post Content"
            }).ToList();

            _context.Posts.AddRange(posts);
            _context.SaveChanges();
            
            var userFavoritedPostStats = posts.Select(p => new UserPostStats
            {
                PostId = p.Id,
                UserId = user.Id,
                IsFavorited = true
            }).ToList();
            
            var authorFavoritedPostStat = new UserPostStats
            {
                PostId = posts[0].Id,
                UserId = author.Id,
                IsFavorited = true
            };
            _context.UserPostStats.Add(authorFavoritedPostStat);
            _context.UserPostStats.AddRange(userFavoritedPostStats);
            _context.SaveChanges();

            var collator = new PostCollator(_inMemoryDbHelper.GetNewInMemoryDatabase(false));
            var userResult = collator.InUsersFavorites(posts.AsQueryable(), user.Id);
            Assert.Equal(posts.Count, userResult.Count());

            var authorResult = collator.InUsersFavorites(posts.AsQueryable(), author.Id);
            Assert.Equal(1, authorResult.Count());
        }

        [Fact]
        public void Filter_collection_by_status()
        {            
            // Setup begin
            var author = _inMemoryDbHelper.CreateUser(_context, 5000, "author");
            var openPosts = Enumerable.Range(1, 4).Select(_ => new Post // Create 4 open posts
            {
                Author = author, Title = "A Title", Content = "The Post Content", IsOpen = true
            }).ToList();
            
            var closedPosts = Enumerable.Range(1, 4).Select(_ => new Post // Create 4 closed posts
            {
                Author = author, Title = "A Title", Content = "The Post Content", IsOpen = false
            }).ToList();

            var allPosts = openPosts.Concat(closedPosts).ToList();
            
            _context.Posts.AddRange(openPosts);
            _context.Posts.AddRange(closedPosts);
            _context.SaveChanges();
            
            var collator = new PostCollator(_inMemoryDbHelper.GetNewInMemoryDatabase(false));
            
            var openResult = collator.FilterStatus(allPosts.AsQueryable(), new []{"Open"});
            Assert.Equal(openPosts.Count, openResult.Count());
            Assert.All(openResult, p =>
            {
                Assert.True(p.IsOpen);
                Assert.Contains(p.Id, openPosts.Select(o => o.Id));
                Assert.DoesNotContain(p.Id, closedPosts.Select(o => o.Id));
            });
            
            var closedResult = collator.FilterStatus(allPosts.AsQueryable(), new []{"Closed"});
            Assert.Equal(closedPosts.Count, closedResult.Count());
            Assert.All(closedResult, p =>
            {
                Assert.False(p.IsOpen);
                Assert.Contains(p.Id, closedPosts.Select(o => o.Id));
                Assert.DoesNotContain(p.Id, openPosts.Select(o => o.Id));
            });
            
            var openAndClosedResult = collator.FilterStatus(
                allPosts.AsQueryable(), new []{"Open", "Closed"});
            Assert.Equal(allPosts.Count, openAndClosedResult.Count());
            Assert.All(openAndClosedResult, p =>
            {
                Assert.Contains(p.Id, allPosts.Select(o => o.Id));
            });
        }

        [Fact]
        public void FilterCategory_loads_categories()
        {
            const string categoryName = "Category";
            var category = new Category {Name = categoryName};
            _context.Categories.Add(category);
            _context.Posts.Add(new Post {Category = category});
            _context.SaveChanges();

            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var collator = new PostCollator(cleanContext);
            var result = collator.FilterCategory(cleanContext.Posts.AsQueryable(),
                new[] {categoryName});
            Assert.Single(result);
            var resultCategory = result.First().Category;
            Assert.NotNull(resultCategory);
            Assert.Equal(categoryName, resultCategory.Name);
        }
        
        [Fact]
        public void Filter_collection_by_category()
        {
            // Begin setup
            var categoryNames = new[] {"Category1", "Category2", "Category3"};
            _context.Categories.AddRange(categoryNames.Select(c => new Category {Name = c}));

            var author = _inMemoryDbHelper.CreateUser(_context, 5000, "Author");
            var posts = new List<Post>();
            foreach (var cName in categoryNames)
            {
                posts = posts.Concat(Enumerable.Range(1, 3).Select(_ =>
                {
                    var category = _context.Categories.First(c => c.Name == cName);
                    return new Post
                        {Author = author, Title = "Post Title", Category = category, Content = "The content"};
                })).ToList();
            }
            _context.AddRange(posts);
            _context.SaveChanges();
            Assert.Equal(9, posts.Count);
            // End setup

            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var collator = new PostCollator(cleanContext);

            foreach (var categoryName in categoryNames)
            {
                // Ensure categories were added to correctly during setup
                Assert.Equal(3, _context.Posts
                    .Include(p => p.Category)
                    .Count(p => p.Category.Name == categoryName)
                );
                
                var result = collator.FilterCategory(cleanContext.Posts.AsQueryable(),
                    new[]{categoryName});
                Assert.Equal(3, result.Count());
                Assert.All(result, p => Assert.Equal(categoryName, p.Category.Name));
            }

            var twoCategories = categoryNames.Take(2).ToList();
            var twoCategoriesResult = collator.FilterCategory(cleanContext.Posts.AsQueryable(),
               twoCategories);
            
            Assert.Equal(6, twoCategoriesResult.Count());
            Assert.All(twoCategoriesResult, p => 
                Assert.Contains(p.Category.Name, twoCategories));
        }
        
        [Fact]
        public void FilterInDateRange_returns_posts_from_same_day_when_begin_and_end_dates_are_equal()
        {
            const string createDateString = "01-01-2050";
            var createDate = DateTime.Parse(createDateString);
            createDate = createDate.AddHours(12);
            
            var post = new Post {CreatedAt = createDate};
            _context.Posts.Add(post);
            _context.SaveChanges();
            
            var cleanContext = _inMemoryDbHelper.GetNewInMemoryDatabase(false);
            var collator = new PostCollator(cleanContext);
            var result = collator.FilterInDateRange(
                cleanContext.Posts.AsQueryable(), createDateString, createDateString);
            
            Assert.Single(result);
            Assert.Contains(post.Id, result.Select(p => p.Id));
        }

        [Fact]
        public void FilterInDateRange_returns_input_collection_when_invalid_date_strings_are_given()
        {
            const string validDate = "01-01-2050";
            var posts = Enumerable.Range(1, 5).Select(_ => new Post {CreatedAt = new DateTime(2050, 1, 1)}).ToList();
            var collator = new PostCollator(_context); // a clean context doesn't matter for this test
            var resultWithBothDatesInvalid = collator.FilterInDateRange(posts.AsQueryable(), "...", "...");
            Assert.Equal(posts.Count, resultWithBothDatesInvalid.Count());
            
            var resultWithStartDateInvalid = collator.FilterInDateRange(posts.AsQueryable(), "...", validDate);
            Assert.Equal(posts.Count, resultWithStartDateInvalid.Count());
            
            var resultWithEndDateInvalid = collator.FilterInDateRange(posts.AsQueryable(), validDate, "...");
            Assert.Equal(posts.Count, resultWithEndDateInvalid.Count());
        }
        
        [Fact]
        public void FilterInDateRange_returns_input_collection_when_start_date_is_after_end_date()
        {
            var posts = Enumerable.Range(1, 5).Select(_ => new Post {CreatedAt = new DateTime(2050, 1, 1)}).ToList();
            var collator = new PostCollator(_context); // a clean context doesn't matter for this test
            var resultWithStartDateAfterEndDate = collator.FilterInDateRange(posts.AsQueryable(), "18-06-2050", "17-06-2050");
            Assert.Equal(posts.Count, resultWithStartDateAfterEndDate.Count());
        }

        [Fact]
        public void Filter_date_range_boundary_checks()
        {
            const string startBoundary = "01-01-2050";
            const string endBoundary = "31-12-2050";
            var postAtStartBoundary = new Post {CreatedAt = DateTime.Parse(startBoundary)};
            var postAtEndBoundary = new Post {CreatedAt = DateTime.Parse(endBoundary)};
            var postJustBeforeStartBoundary = new Post {CreatedAt = DateTime.Parse(startBoundary).AddDays(-1)};
            var postJustAfterEndBoundary = new Post {CreatedAt = DateTime.Parse(endBoundary).AddDays(1)};
            var postBetweenBoundaries  = new Post {CreatedAt = DateTime.Parse("18-06-2050")};
            var posts = new[]
            {
                postAtStartBoundary, postAtEndBoundary,
                postJustBeforeStartBoundary, postJustAfterEndBoundary, postBetweenBoundaries
            };
            
            var collator = new PostCollator(_context);
            
            var startEndResult = collator.FilterInDateRange(posts.AsQueryable(), startBoundary, endBoundary);
            Assert.Contains(postAtStartBoundary, startEndResult);
            Assert.Contains(postAtEndBoundary, startEndResult);
            Assert.Contains(postBetweenBoundaries, startEndResult);
            Assert.DoesNotContain(postJustBeforeStartBoundary, startEndResult);
            Assert.DoesNotContain(postJustAfterEndBoundary, startEndResult);
        }
    }
}