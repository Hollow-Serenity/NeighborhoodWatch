using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BuurtApplicatie.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BuurtApplicatie.Models
{
    public class Post
    {
        public int Id { get; set; }

        [JsonIgnore]
        public string AuthorId { get; set; }
        [JsonIgnore]
        public BuurtApplicatieUser Author { get; set; }
        
        [Required(ErrorMessage = "Een melding moet een titel bevatten.")]
        [MinLength(3, ErrorMessage = "Een titel moet minstens 3 karakters bevatten.")]
        [MaxLength(100, ErrorMessage = "Een titel mag niet meer dan 100 karakters bevatten.")]
        [Remote("VerifyTitle", "Posts", AdditionalFields = nameof(Title), ErrorMessage = "Er bestaat al een melding met deze titel")]
        [Display(Name = "Titel")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Een melding moet een beschrijving bevatten.")]
        [MinLength(10, ErrorMessage = "Een beschrijving moet minstens 10 karakters bevatten.")]
        [Display(Name = "Beschrijving")]
        public string Content { get; set; }

        [JsonIgnore]
        public int? CategoryId { get; set; }
        [JsonIgnore]
        [Display(Name = "Categorie")]
        public Category Category { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public bool IsOpen { get; set; }

        [JsonIgnore]
        public Image Image { get; set; }
        
        [JsonIgnore]
        public ICollection<ReportedPost> ReportedPosts { get; set; }
        [JsonIgnore]
        public ICollection<UserPostStats> UserPostStats { get; set; }
    }
}