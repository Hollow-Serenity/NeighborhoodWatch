using System.ComponentModel.DataAnnotations;

namespace BuurtApplicatie.Models.ViewModels
{
    public class DeletePostViewModel
    {
        public Post Post { get; set; }
        
        [Display(Name = "Reden voor verwijdering")]
        public string Reason { get; set; }
    }
}