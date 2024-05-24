using BuurtApplicatie.Areas.Identity.Data;

namespace BuurtApplicatie.Models
{
    public class ReportedPost
    {
        public int PostId { get; set; }
        public Post Post { get; set; }
        
        public string ReportedById { get; set;}
        public BuurtApplicatieUser ReportedBy { get; set; }
    }
}