using System.Collections.Generic;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Identity;

namespace BuurtApplicatie.Areas.Identity.Data
{
    public class BuurtApplicatieUser : IdentityUser
    {
        public ICollection<ReportedPost> ReportedPosts { get; set; }
        public ICollection<UserPostStats> UserPostStats { get; set; }
        public ICollection<Post> Posts { get; set; }
        public ICollection<Comment> Comments { get; set; }

        public Address Address { get; set; }

        public Image ProfilePicture { get; set; }
    }
}