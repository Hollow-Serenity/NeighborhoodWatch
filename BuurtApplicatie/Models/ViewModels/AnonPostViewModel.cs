using System;

namespace BuurtApplicatie.Models.ViewModels
{
    public class AnonPostViewModel
    {
        public string Title { get; } = "Privé melding";

        public string Content { get; } = "Verborgen inhoud";
        
        public DateTime CreatedAt { get; set; }
    }
}