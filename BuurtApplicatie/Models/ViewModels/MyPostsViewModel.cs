using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuurtApplicatie.Models.ViewModels
{
    public class MyPostsViewModel
    {
        public IEnumerable<Post> PublicPosts { private get; set; }
        
        public IEnumerable<AnonPostViewModel> AnonPosts { private get; set; }
        
        public IEnumerable<MyPostViewModel> AllPosts
        {
            get
            {
                var allPosts = PublicPosts.Select(p => new MyPostViewModel
                {
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt
                });
                allPosts = allPosts.Concat(AnonPosts.Select(p => new MyPostViewModel
                {
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt
                }));
                return allPosts.OrderByDescending(p => p.CreatedAt);
            }
        }
    }
}