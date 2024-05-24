using BuurtApplicatie.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BuurtApplicatie.Models
{
    public class DeletedPost
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Titel")]
        public string Title { get; set; }
        
        [Required]
        [Display(Name = "Inhoud")]
        public string Content { get; set; }
        
        [Required]
        [Display(Name = "Reden voor verwijdering")]
        public string Reason { get; set; }

        public string UserId { get; set; }
        public BuurtApplicatieUser User { get; set; }
    }
}
