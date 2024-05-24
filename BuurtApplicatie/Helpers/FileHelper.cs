using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuurtApplicatie.Helpers
{
    public class FileHelper : IFileHelper
    {
        private static readonly string[] PermittedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
        private static readonly Dictionary<string, List<byte[]>> FileSignatures = 
            new Dictionary<string, List<byte[]>>
            {
                {
                    ".jpeg", new List<byte[]>
                    {
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 }
                    }
                },
                {
                    ".png", new List<byte[]>
                    {
                        new byte [] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                    }
                },
                {
                    ".jpg", new List<byte[]>
                    {
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
                    }
                },
                {
                    ".bmp", new List<byte[]>
                    {
                        new byte[] { 0x42, 0x4D }
                    }
                }
            };
        
        private const int FileSizeLimit = 2097152;
        
        private readonly BuurtApplicatieDbContext _context;
        private readonly ILogger<FileHelper> _logger;
        private readonly IConfiguration _configuration;

        public FileHelper(BuurtApplicatieDbContext context,
            ILogger<FileHelper> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ImageResult> GetImageFromFileAsync(IFormFile file)
        {
            return await GetImageFromFileAsync(file, new ImageResult());
        }
        
        private async Task<ImageResult> GetImageFromFileAsync(IFormFile file, ImageResult result)
        {
            result = IsFileAValidImage(file, result);
            if (!result.Succeeded) return result;

            return await ConvertFileToImageAsync(file, result);
        }
        
        public async Task<ImageResult> ReplaceImageAsync(IFormFile newFile, Image oldImage)
        {
            var result = new ImageResult();
            if (oldImage == null)
            {
                result.Error.Code = "UploadError";
                result.Error._description = _configuration["FileHelper:ErrorMessages:UploadError"];
                return result;
            }

            result = await GetImageFromFileAsync(newFile, result);
            if (!result.Succeeded) return result;
            
            _context.Images.Remove(oldImage);
            
            return result;
        }

        private ImageResult IsFileAValidImage(IFormFile file, ImageResult result)
        {
            if (file == null) return result;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var fileIsImage = IsFileAnImage(ext);

            if (!fileIsImage)
            {
                result.Error.Code = "InvalidFileExtension";
                result.Error._description = _configuration["FileHelper:ErrorMessages:FileInvalid"];
                return result;
            }

            var fileHasValidSignature = IsFileSignatureValid(file, ext);
            if (!fileHasValidSignature)
            {
                _logger.LogWarning("A file upload was attempted with an invalid file signature.");
                result.Error.Code = "InvalidFileSignature";
                result.Error._description = _configuration["FileHelper:ErrorMessages:FileInvalid"];
                return result;
            }

            if (file.Length > FileSizeLimit)
            {
                _logger.LogWarning("A file upload was attempted with a filesize greater than the filesize limit.");
                result.Error.Code = "InvalidFileSize";
                result.Error._description = _configuration["FileHelper:ErrorMessages:FileTooBig"];
                return result;
            }

            result.Succeeded = true;
            return result;
        }

        private async Task<ImageResult> ConvertFileToImageAsync(IFormFile fileToUpload, ImageResult result)
        {
            await using var memoryStream = new MemoryStream();
            await fileToUpload.CopyToAsync(memoryStream);

            var file = new Image { Data = memoryStream.ToArray() };
            
            result.Image = file;
            result.Succeeded = true;
            return result;
        }

        // TODO: Write tests to verify that this method works
        private static bool IsFileAnImage(string ext)
        {
            return !string.IsNullOrEmpty(ext) && PermittedImageExtensions.Contains(ext);
        }
        
        private static bool IsFileSignatureValid(IFormFile file, string ext)
        {
            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var signatures = FileSignatures[ext];
                var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

                return signatures.Any(signature =>
                    headerBytes.Take(signature.Length).SequenceEqual(signature));
            }
        }
    }

    public class ImageResult
    {
        public ImageResult()
        {
            Error = new ImageError();
        }
        public Image Image { get; set; }
        
        public bool Succeeded { get; set; }
        
        public ImageError Error { get; }
    }

    public class ImageError
    {
        public string Code { get; set; }
        
        public string _description { get; set; }

        public string Description => _description ?? Code;
    }
}