using System.Collections.Generic;
using BuurtApplicatie.Models;
namespace BuurtApplicatie.Models.PostDetailsViewModels
{
    public class PostDetailsViewModel
    {
        public int Likes { get; set; }

        public IEnumerable<Comment> Comments { get; set; }

        public Post Post{get; set;}
    }
}