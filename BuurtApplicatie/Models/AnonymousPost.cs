using System;

namespace BuurtApplicatie.Models
{
    public class AnonymousPost
    {
        public string Id { get; set; }
        
        public string Title { get; set; }
        
        public string Content { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public string AnonUserId { get; set; }
        public AnonymousUser AnonUser { get; set; }
    }
}