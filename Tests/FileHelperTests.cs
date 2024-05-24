using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuurtApplicatie.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests
{
    public class FileHelperTests
    {
        private readonly FileHelper _fileHelper;
        
        public FileHelperTests()
        {
            var context = new InMemoryDbHelper().GetNewInMemoryDatabase(true);
            _fileHelper = new FileHelper(context, 
                new Mock<ILogger<FileHelper>>().Object, new Mock<IConfiguration>().Object);
        }
        

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".png")]
        [InlineData(".jpeg")]
        [InlineData(".bmp")]
        public async void Valid_file_extensions(string extension)
        {
            var mockFile = new Mock<IFormFile>();
            var ms = new MemoryStream();
            ms.Position = 0;
            mockFile.Setup(_ => _.OpenReadStream()).Returns(ms);
            mockFile.Setup(_ => _.FileName).Returns($"file{extension}");
            mockFile.Setup(_ => _.Length).Returns(ms.Length);
            
            var result = await _fileHelper.GetImageFromFileAsync(mockFile.Object);
            Assert.NotEqual("InvalidFileExtension", result.Error.Code);
        }
        
        [Theory]
        [InlineData(".docx")]
        [InlineData(".apng")]
        [InlineData(".webp")]
        [InlineData(".pdf")]
        [InlineData(".gif")]
        public async void Invalid_file_extensions(string extension)
        {
            var mockFile = new Mock<IFormFile>();
            var ms = new MemoryStream();
            ms.Position = 0;
            mockFile.Setup(_ => _.OpenReadStream()).Returns(ms);
            mockFile.Setup(_ => _.FileName).Returns($"file{extension}");
            mockFile.Setup(_ => _.Length).Returns(ms.Length);
            
            var result = await _fileHelper.GetImageFromFileAsync(mockFile.Object);
            Assert.Equal("InvalidFileExtension", result.Error.Code);
        }
    }
}