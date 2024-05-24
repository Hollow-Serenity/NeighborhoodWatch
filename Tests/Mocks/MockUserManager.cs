using BuurtApplicatie.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Tests.Mocks
{
    public class MockUserManager
    {
        public static Mock<UserManager<BuurtApplicatieUser>> GetMockUserManager()
        {
            var userStoreMock = new Mock<IUserStore<BuurtApplicatieUser>>();
            return new Mock<UserManager<BuurtApplicatieUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
        }
    }
}