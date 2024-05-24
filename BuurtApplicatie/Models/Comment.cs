using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BuurtApplicatie.Areas.Identity.Data;

namespace BuurtApplicatie.Models
{
    public class Comment
    {
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        [Required]
        [JsonIgnore]
        public string AuthorId { get; set; }
        [JsonIgnore]
        public BuurtApplicatieUser Author { get; set; }
        
        [Required]
        public int PostId { get; set; }
        [JsonIgnore]
        public Post Post { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}