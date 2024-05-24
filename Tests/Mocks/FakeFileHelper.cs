using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Mocks
{
    public class FakeFileHelper : FileHelper
    {
        public FakeFileHelper(BuurtApplicatieDbContext context) : base(context, new Mock<ILogger<FileHelper>>().Object, new Mock<IConfigurationSection>().Object)
        { }
    }
}