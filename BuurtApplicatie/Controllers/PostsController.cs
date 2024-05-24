using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Helpers;
using BuurtApplicatie.Models;
using BuurtApplicatie.Models.PostEditViewModels;
using BuurtApplicatie.Models.PostOverviewViewModels;
using Microsoft.AspNetCore.Http;
using BuurtApplicatie.Models.PostDetailsViewModels;
using BuurtApplicatie.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StringExtensions;

namespace BuurtApplicatie.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly BuurtApplicatieDbContext _context;
        private readonly UserManager<BuurtApplicatieUser> _userManager;
        private readonly IFileHelper _fileHelper;
        public PostsController(BuurtApplicatieDbContext context,
            UserManager<BuurtApplicatieUser> userManager,
            IFileHelper fileHelper)
        {
            _context = context;
            _userManager = userManager;
            _fileHelper = fileHelper;
        }
        public async Task<IActionResult> Index(
            string orderBy,
            string q,
            int pageNumber,
            string[] category,
            string[] status,
            bool inMyFavorites,
            string start,
            string end)
        {
            var viewModel = new PostOverviewViewModel();

            var posts = _context.Posts
                .Include(p => p.Category)
                .AsQueryable();

            var collator = new PostCollator(_context);
            ViewData["Filter"] = q;
            viewModel.InMyFavorites.IsChecked = inMyFavorites;
            ViewBag.SelectedCategories = category;
            viewModel.OrderBy = orderBy ?? "";
            foreach (var checkBox in viewModel.StatusCheckBoxes)
            {
                checkBox.IsChecked = status.Contains(checkBox.Value);
            }
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                var startDate = DateTime.Parse(start);
                var endDate = DateTime.Parse(end);
                ViewData["start"] = startDate.ToString("dd-MM-yyyy");
                ViewData["end"] = endDate.ToString("dd-MM-yyyy");
            }

            posts = collator.FilterInDateRange(posts, start, end);

            if (inMyFavorites)
                posts = collator.InUsersFavorites(posts, _userManager.GetUserId(User));

            posts = collator.FilterStatus(posts, status);

            posts = collator.FilterCategory(posts, category);

            posts = collator.FilterQuery(posts, q);

            var overviewPosts = posts.Select(p => new PostOverviewPostViewModel
            {
                Post = p,
                TruncatedContent = p.Content.Truncate(100, "..."),
                Likes = _context.UserPostStats.Count(ups => ups.PostId == p.Id && ups.IsFavorited),
                Views = _context.UserPostStats.Count(ups => ups.PostId == p.Id && ups.IsViewed),
                Comments = _context.Comments.Count(c => c.PostId == p.Id)
            });

            overviewPosts = collator.OrderBy(overviewPosts, orderBy);
            var paginatedResult = await PaginatedList<
                PostOverviewPostViewModel>.CreateAsync(overviewPosts, pageNumber, 10);

            /* [Dino] We explicitly load missing values that may have been lost during the creation
             * of the paginated list. For more information, see the last note in
             * https://docs.microsoft.com/en-us/ef/core/querying/related-data/eager#filtered-include
             */
            foreach (var item in paginatedResult)
            {
                await _context.Entry(item.Post).Reference(p => p.Author).LoadAsync();
                await _context.Entry(item.Post.Author).Reference(a => a.ProfilePicture).LoadAsync();
                await _context.Entry(item.Post).Reference(p => p.Category).LoadAsync();
                await _context.Entry(item.Post).Reference(p => p.Image).LoadAsync();
            }

            viewModel.Posts = paginatedResult;
            viewModel.Categories = _context.Categories.Select(c => new CategoryViewModel
            {
                Name = c.Name,
                IsChecked = category.Contains(c.Name)
            });

            return View(viewModel);
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Image)
                .Include(p => p.Author)
                    .ThenInclude(a => a.ProfilePicture)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            var viewModel = new PostDetailsViewModel
            {
                Post = post,
                Likes = _context.UserPostStats.Count(ups => ups.PostId == post.Id && ups.IsFavorited),
                Comments = _context.Comments
                .Include(c => c.Author)
                    .ThenInclude(a => a.ProfilePicture)
                .Where(c => c.PostId == post.Id)
            };
            var user = await _userManager.GetUserAsync(User);
            ViewData["UserIsAuthor"] = post.AuthorId == user.Id;

            await IncrementPostViewCount(post, user.Id);
            var userPostStats = await _context.UserPostStats
                .FirstOrDefaultAsync(ups =>
                    ups.PostId == post.Id &&
                    ups.UserId == user.Id);
            ViewData["userHasLikedPost"] = userPostStats.IsFavorited;

            ViewData["userHasReportedPost"] = await _context.ReportedPosts
                .FirstOrDefaultAsync(ups =>
                    ups.PostId == post.Id &&
                    ups.ReportedById == user.Id) != null;
            return View(viewModel);
        }

        private async Task IncrementPostViewCount(Post post, string userId)
        {
            var userPostStats = await _context.UserPostStats
                .FirstOrDefaultAsync(ups =>
                    ups.PostId == post.Id &&
                    ups.UserId == userId);

            if (userPostStats == null)
            {
                userPostStats = new UserPostStats { PostId = post.Id, UserId = userId };
                // ReSharper disable once MethodHasAsyncOverload
                _context.UserPostStats.Add(userPostStats);
            }

            userPostStats.IsViewed = true;
            await _context.SaveChangesAsync();
        }
        // POST: ReportPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost(int postId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.IsOpen && p.Id == postId);
            if (post == null)
            {
                return NotFound();
            }
            var user = await _userManager.GetUserAsync(User);

            var reportedPost = await _context.ReportedPosts
                .FirstOrDefaultAsync(rp =>
                    rp.PostId == post.Id &&
                    rp.ReportedById == user.Id);

            if (reportedPost != null)
            {
                return RedirectToAction(nameof(Details), new { id = postId });
            }
            if (reportedPost == null)
            {
                reportedPost = new ReportedPost { PostId = post.Id, ReportedById = user.Id };
                // ReSharper disable once MethodHasAsyncOverload
                _context.ReportedPosts.Add(reportedPost);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = postId });
        }

        // POST: LikeButton
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikePost(int postId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.IsOpen && p.Id == postId);
            if (post == null)
            {
                return NotFound();
            }
            var user = await _userManager.GetUserAsync(User);

            var userPostStats = await _context.UserPostStats
                .FirstOrDefaultAsync(ups =>
                    ups.PostId == post.Id &&
                    ups.UserId == user.Id);

            if (userPostStats == null)
            {
                userPostStats = new UserPostStats { PostId = post.Id, UserId = user.Id };
                // ReSharper disable once MethodHasAsyncOverload
                _context.UserPostStats.Add(userPostStats);
            }

            userPostStats.IsFavorited = !userPostStats.IsFavorited;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = postId });
        }

        // POST: CreateComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(string commentBody, int postId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.IsOpen && p.Id == postId);
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var postComment = new Comment { AuthorId = user.Id, CreatedAt = DateTime.Now, Content = commentBody, PostId = postId };

            if (TryValidateModel(postComment, nameof(Comment)))
            {
                _context.Add(postComment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = postId });
        }

        // GET: Posts/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        private async Task<IActionResult> CreateAnonPost(Post post, Image image)
        {
            var anonPost = new AnonymousPost { Content = post.Content, Title = post.Title, CreatedAt = DateTime.Now };
            var anonUser = await _context.AnonUsers
                .SingleOrDefaultAsync(u => u.UserId == _userManager.GetUserId(User));
            if (anonUser != null)
            {
                anonPost.AnonUserId = anonUser.Id;
            }

            if (image != null)
                _context.AnonImages.Add(new AnonymousImage { Data = image.Data, Post = anonPost });

            _context.AnonPosts.Add(anonPost);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,CategoryId")] Post post,
            bool isPrivate = false,
            IFormFile uploadedFile = null)
        {
            ViewBag.IsPrivate = isPrivate;
            if (isPrivate && ModelState.IsValid)
            {
                var result = await _fileHelper.GetImageFromFileAsync(uploadedFile);
                if (!result.Succeeded && !string.IsNullOrEmpty(result.Error.Code))
                {
                    ModelState.AddModelError(string.Empty, result.Error.Description);
                    ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
                return await CreateAnonPost(post, result.Image);
            }

            if (ModelState.IsValid && !PostWithTitleExists(post.Title) && CategoryExists(post.CategoryId))
            {
                var user = await _userManager.GetUserAsync(User);
                post.AuthorId = user.Id;
                post.CreatedAt = DateTime.Now;
                post.IsOpen = true;

                if (uploadedFile != null)
                {
                    var result = await _fileHelper.GetImageFromFileAsync(uploadedFile);
                    if (!result.Succeeded && !string.IsNullOrEmpty(result.Error.Code))
                    {
                        ModelState.AddModelError(string.Empty, result.Error.Description);
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                        return View(post);
                    }

                    post.Image = result.Image;
                }

                if (TryValidateModel(post, nameof(Post)))
                {
                    _context.Add(post);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Image)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            await _context.Entry(post).Reference(p => p.Author).LoadAsync();

            if (post.AuthorId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            ViewData["Author"] = post.Author.UserName;
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(PostEditViewModel.MapEditViewModel(post));
        }

        // POST: Posts2/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,CategoryId")] PostEditViewModel editedPost, IFormFile uploadedFile)
        {
            if (id != editedPost.Id)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Image)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            if (ModelState.IsValid &&
                post.AuthorId == _userManager.GetUserId(User) &&
                CategoryExists(editedPost.CategoryId))
            {
                post.Title = editedPost.Title;
                post.Content = editedPost.Content;
                post.CategoryId = editedPost.CategoryId;

                if (uploadedFile != null)
                {
                    ImageResult result;
                    var currentPostImage = await _context.Images.FirstOrDefaultAsync(i => i.PostId == post.Id);
                    if (currentPostImage == null)
                        result = await _fileHelper.GetImageFromFileAsync(uploadedFile);
                    else
                        result = await _fileHelper.ReplaceImageAsync(uploadedFile, currentPostImage);

                    if (!result.Succeeded && !string.IsNullOrEmpty(result.Error.Code))
                    {
                        ModelState.AddModelError(string.Empty, result.Error.Description);
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                        return View(PostEditViewModel.MapEditViewModel(post));
                    }
                    post.Image = result.Image;
                }

                try
                {
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(editedPost.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            await _context.Entry(post).Reference(p => p.Author).LoadAsync();

            ViewData["Author"] = post.Author.UserName;
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", editedPost.CategoryId);
            return View(PostEditViewModel.MapEditViewModel(post));
        }

        // GET: Posts/MyPosts
        public async Task<IActionResult> MyPosts()
        {
            var userId = _userManager.GetUserId(User);
            var anonUser = await _context.AnonUsers
                .SingleOrDefaultAsync(a => a.UserId == userId);

            var publicPosts = _context.Posts.Where(p => p.AuthorId == userId).AsEnumerable();
            var anonPosts = _context.AnonPosts
                .Where(p => p.AnonUserId == anonUser.Id)
                .Select(p => new AnonPostViewModel { CreatedAt = p.CreatedAt })
                .AsEnumerable();

            var viewModel = new MyPostsViewModel { PublicPosts = publicPosts, AnonPosts = anonPosts };
            return View(viewModel);
        }

        // GET: Posts/DeletedPosts
        public async Task<IActionResult> DeletedPosts()
        {
            var user = await _userManager.GetUserAsync(User);
            var deletedPosts = _context.DeletedPosts.Where(dp => dp.UserId == user.Id);
            return View(deletedPosts);
        }

        // GET: Posts/Delete/5
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Image)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(new DeletePostViewModel { Post = post });
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> DeleteConfirmed(int id, string reason)
        {
            var post = await _context.Posts.FindAsync(id);
            var deletedPost = new DeletedPost
            {
                Content = post.Content,
                Title = post.Title,
                Reason = reason,
                UserId = post.AuthorId
            };
            var reportsForThisPost = _context.ReportedPosts
                .Where(rp => rp.PostId == post.Id);
            _context.ReportedPosts.RemoveRange(reportsForThisPost);
            _context.DeletedPosts.Add(deletedPost);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ReportedPosts));
        }

        [Authorize(Roles = "Moderator")]
        public IActionResult ReportedPosts()
        {
            var reportedPosts = _context.ReportedPosts
                .Include(rp => rp.Post)
                .Include(rp => rp.ReportedBy)
                .AsNoTracking();
            return View(reportedPosts);
        }

        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> Close(int id)
        {
            if (!PostExists(id)) return RedirectToAction(nameof(Index));
            var post = await _context.Posts.FindAsync(id);

            post.IsOpen = false;
            _context.Update(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        /* See https://docs.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-3.1#remote-attribute
         * for information on how this works. 
         */
        [AcceptVerbs("GET", "POST")]
        public IActionResult VerifyTitle(string title)
        {
            return Json(!string.IsNullOrEmpty(title) && !PostWithTitleExists(title));
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult VerifyEditedTitle(string title)
        {
            return Json(!string.IsNullOrEmpty(title) && !PostWithTitleExists(title, true));
        }

        /// <summary>
        /// Indicates whether the specified title already exists in any active posts
        /// </summary>
        /// <param name="title">The post title to compare existing posts against.</param>
        /// <param name="excludePostsByCurrentUser">Whether or not to exclude posts made by the currently logged in user.</param>
        private bool PostWithTitleExists(string title, bool excludePostsByCurrentUser = false)
        {
            if (!excludePostsByCurrentUser)
            {
                return _context.Posts
                        .AsEnumerable()!
                    .Where(p => p.IsOpen)
                    .Any(p => string.Equals(p.Title, title, StringComparison.InvariantCultureIgnoreCase));
            }

            var userId = _userManager.GetUserId(User);
            return _context.Posts
                .Include(p => p.Author)
                .AsEnumerable()
                .Where(p => p.IsOpen && p.Author.Id != userId)
                .Any(p => string.Equals(p.Title, title, StringComparison.InvariantCultureIgnoreCase));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }

        // Returns whether a category exists. Returns true when id is null, since categories are optional.
        private bool CategoryExists(int? id)
        {
            return id == null || _context.Categories.Any(c => c.Id == id);
        }
    }
}