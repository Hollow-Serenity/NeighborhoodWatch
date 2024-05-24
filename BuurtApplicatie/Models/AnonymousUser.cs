using System.ComponentModel.DataAnnotations;
using BuurtApplicatie.Areas.Identity.Data;

namespace BuurtApplicatie.Models
{
    public class AnonymousUser
    {
        public string Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public BuurtApplicatieUser User { get; set; }
    }
}