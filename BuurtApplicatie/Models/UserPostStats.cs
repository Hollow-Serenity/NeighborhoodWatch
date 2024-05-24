using BuurtApplicatie.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuurtApplicatie.Models
{
    public class UserPostStats
    {
        public string UserId { get; set; }
        public BuurtApplicatieUser User { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }

        public bool IsViewed { get; set; }
        
        public bool IsFavorited { get; set; }
    }
}
