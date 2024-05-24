using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Models;
using BuurtApplicatie.Models.ImageViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuurtApplicatie.ViewComponents
{
    public class ProfilePictureViewComponent : ViewComponent
    {
        private readonly BuurtApplicatieDbContext _context;
        private readonly UserManager<BuurtApplicatieUser> _userManager;


        public ProfilePictureViewComponent(BuurtApplicatieDbContext context,
            UserManager<BuurtApplicatieUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(Image image, string altText)
        {
            var viewModel = new ImageViewModel { AltText = altText };
            if (image != null)
            {
                viewModel.Data = image.Data;
            }
            else
            {
                var user = await _userManager.GetUserAsync((ClaimsPrincipal)User);
                // [Dino] we still return the model here, if we don't, the model of the parent view will get passed to
                // the view, causing an InvalidOperationException. See https://stackoverflow.com/a/40373596
                if (!_context.Images.Any(i => i.UserId == user.Id)) return View(viewModel);
                await _context.Entry(user).Reference(u => u.ProfilePicture).LoadAsync();
                viewModel.Data = user.ProfilePicture.Data;
            }
            return View(viewModel);
        }
    }
}