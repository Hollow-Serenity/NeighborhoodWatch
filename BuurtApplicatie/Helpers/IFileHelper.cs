using System.Threading.Tasks;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Http;

namespace BuurtApplicatie.Helpers
{
    public interface IFileHelper
    {
        Task<ImageResult> GetImageFromFileAsync(IFormFile file);

        Task<ImageResult> ReplaceImageAsync(IFormFile newFile, Image oldImage);
    }
}