using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace BuurtApplicatie.Models.PostEditViewModels
{
    public class PostEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Een melding moet een titel bevatten.")]
        [MinLength(3, ErrorMessage = "Een titel moet minstens 3 karakters bevatten.")]
        [MaxLength(100, ErrorMessage = "Een titel mag niet meer dan 100 karakters bevatten.")]
        [Remote("VerifyEditedTitle", "Posts", AdditionalFields = nameof(Title), ErrorMessage = "Er bestaat al een melding met deze titel")]
        [Display(Name = "Titel")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Een melding moet een beschrijving bevatten.")]
        [MinLength(10, ErrorMessage = "Een beschrijving moet minstens 10 karakters bevatten.")]
        [Display(Name = "Beschrijving")]
        public string Content { get; set; }
        
        public int? CategoryId { get; set; }
        
        public int? ImageId { get; set; }
        
        public static PostEditViewModel MapEditViewModel(Post post)
        {
            return new PostEditViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                CategoryId = post.CategoryId,
                ImageId = post.Image?.Id
            };
        }
    }
}