namespace BuurtApplicatie.Models.PostOverviewViewModels
{
    public class PostOverviewPostViewModel
    {
        public Post Post { get; set; }
        
        public string TruncatedContent { get; set; } 
        
        public int Likes { get; set; }
        
        public int Comments { get; set; }
        
        public int Views { get; set; }
    }
}