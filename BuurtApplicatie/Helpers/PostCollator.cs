using System;
using System.Collections.Generic;
using System.Linq;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Models;
using BuurtApplicatie.Models.PostOverviewViewModels;
using Microsoft.EntityFrameworkCore;

namespace BuurtApplicatie.Helpers
{
    public class PostCollator
    {
        private readonly BuurtApplicatieDbContext _context;

        public PostCollator(BuurtApplicatieDbContext context)
        {
            _context = context;
        }
        
        public IQueryable<Post> FilterInDateRange(IQueryable<Post> posts, string start, string end)
        {
            var validStartDate = DateTime.TryParse(start, out var startDate);
            var validEndDate = DateTime.TryParse(end, out var endDate);
            if (!validStartDate || !validEndDate) return posts;
            
            if (startDate > endDate) return posts;

            return posts.Where(p => p.CreatedAt.Date >= startDate.Date && p.CreatedAt.Date <= endDate.Date);
        }

        public IQueryable<Post> FilterCategory(IQueryable<Post> posts, IReadOnlyCollection<string> category)
        {
            if (category.Count <= 0) return posts;
            // Explicitly load categories
            foreach (var post in posts)
            {
                _context.Entry(post).Reference(p => p.Category).Load();
            }
            posts = posts.Where(p => category.Contains(p.Category.Name));

            return posts;
        }

        public IQueryable<Post> FilterStatus(IQueryable<Post> posts,
            IReadOnlyCollection<string> status)
        {
            var result = posts.Where(p => p.IsOpen); // Only return open posts by default
            if (status == null || status.Count == 0) return result;
            var normalizedStatus = status.Select(e => e.ToUpper()).ToList();

            if (normalizedStatus.Contains("OPEN") && normalizedStatus.Contains("CLOSED"))
                result = posts;
            else if (normalizedStatus.Count == 1 && normalizedStatus.Contains("OPEN"))
                result = posts.Where(p => p.IsOpen);
            else if (normalizedStatus.Count == 1 && normalizedStatus.Contains("CLOSED"))
                result = posts.Where(p => !p.IsOpen);

            return result;
        }

        public IQueryable<Post> InUsersFavorites(IQueryable<Post> posts, string userId)
        {
            var postsUserHasFavorited = _context.UserPostStats
                .Include(ups => ups.Post)
                .Where(ups =>
                    ups.IsFavorited &&
                    ups.UserId == userId)
                .Select(ups => ups.Post);
            
            return posts.Where(p => postsUserHasFavorited.Select(lp => lp.Id).Contains(p.Id));
        }
        
        public IQueryable<PostOverviewPostViewModel> OrderBy(IQueryable<PostOverviewPostViewModel> result, string orderBy)
        {
            switch (orderBy)
            {
                case "views_asc":
                    result = result.OrderBy(p => p.Views);
                    break;
                case "views_desc":
                    result = result.OrderByDescending(p => p.Views);
                    break;
                case "likes_asc":
                    result = result.OrderBy(p => p.Likes);
                    break;
                case "likes_desc":
                    result = result.OrderByDescending(p => p.Likes);
                    break;
                case "date_asc":
                    result = result.OrderBy(p => p.Post.CreatedAt);
                    break;
                case "date_desc":
                    result = result.OrderByDescending(p => p.Post.CreatedAt);
                    break;
                default:
                    result = result.OrderByDescending(p => p.Post.CreatedAt);
                    break;
            }

            return result;
        }
        
        public IQueryable<Post> FilterQuery(IQueryable<Post> list, string query)
        {
            if (string.IsNullOrEmpty(query)) return list;
            var normalizedQuery = query.ToUpper();
            list = list.Where(p =>
                p.Title.ToUpper().Contains(normalizedQuery) || 
                p.Content.ToUpper().Contains(normalizedQuery));

            return list;
        }
    }
}