using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;

namespace BuurtApplicatie.Models
{
    public class Image
    {
        public int Id { get; set; }
        
        public byte[] Data { get; set; }

        public int? PostId { get; set; }
        public Post Post { get; set; }
        
#pragma warning disable 8632
        public string? UserId { get; set; }
#pragma warning restore 8632
        public BuurtApplicatieUser User { get; set; }
    }
}
