using BuurtApplicatie.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BuurtApplicatie.Models
{
    public class Address
    {
        [JsonIgnore]
        public int Id { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Huisnummer moet positief zijn.")]
        [Display(Name = "Huisnr.")]
        public int HouseNr { get; set; }
        
        [MaxLength(8)]
        [Display(Name = "Toevoeging")]
        public string Addition { get; set; }
        
        [Required]
        [RegularExpression("^[1-9][0-9]{3}[ ]?([A-RT-Za-rt-z][A-Za-z]|[sS][BCbcE-Re-rT-Zt-z])$", ErrorMessage = "Dit is geen geldige postcode.")]
        [Display(Name = "Postcode")]
        public string PostCode { get; set; }
        
        [Display(Name = "Straat")]
        public string StreetName { get; set; }
        
        [Display(Name = "Stad")]
        public string City { get; set; }

        [JsonIgnore]
        public string UserId { get; set; }
        [JsonIgnore]
        public BuurtApplicatieUser User { get; set; }
    }
}
