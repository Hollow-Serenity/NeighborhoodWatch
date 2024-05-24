using System;
using System.ComponentModel.DataAnnotations;

namespace BuurtApplicatie.Models.ViewModels
{
    public class MyPostViewModel
    {
        [Display(Name = "Titel")]
        public string Title { get; set; }

        [Display(Name = "Inhoud")]
        public string Content { get; set; }
        
        [Display(Name = "Gemaakt op")]
        public DateTime CreatedAt { get; set; }
    }
}