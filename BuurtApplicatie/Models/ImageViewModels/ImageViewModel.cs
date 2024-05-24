using System;

namespace BuurtApplicatie.Models.ImageViewModels
{
    public class ImageViewModel
    {
        public byte[] Data { get; set; }

        public string AltText { get; set; }
        
        public string DataUrl => $"data:image/png;base64,{Convert.ToBase64String(Data)}";
    }
}