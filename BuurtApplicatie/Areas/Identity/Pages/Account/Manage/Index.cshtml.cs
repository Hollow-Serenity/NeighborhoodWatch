using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Helpers;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuurtApplicatie.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<BuurtApplicatieUser> _userManager;
        private readonly SignInManager<BuurtApplicatieUser> _signInManager;
        private readonly IFileHelper _fileHelper;
        private readonly BuurtApplicatieDbContext _context;

        public IndexModel(
            UserManager<BuurtApplicatieUser> userManager,
            SignInManager<BuurtApplicatieUser> signInManager,
            IFileHelper fileHelper,
            BuurtApplicatieDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _fileHelper = fileHelper;
            _context = context;
        }

        public string Username { get; set; }

        public Image ProfilePicture { get; set; }

        [Display(Name = "Verander profielfoto (Max. 2MB)")]
        public IFormFile UploadedFile { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
            public Address Address { get; set; }
        }

        private async Task LoadAsync(BuurtApplicatieUser user)
        {
            await _context.Entry(user).Reference(u => u.Address).LoadAsync();
            await _context.Entry(user).Reference(u => u.ProfilePicture).LoadAsync();

            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var address = user.Address;

            Username = userName;

            ProfilePicture = user.ProfilePicture;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Address = address
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var result = await TryUploadImage(user);
            if (!result.Succeeded && !string.IsNullOrEmpty(result.Error.Code))
            {
                ModelState.AddModelError(string.Empty, result.Error.Description);
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            await _context.Entry(user).Reference(u => u.Address).LoadAsync();
            var address = user.Address ?? new Address();
            if (address.UserId == null)
                _context.Addresses.Add(address);
            address.User = user;

            address.Addition = Input.Address.Addition;
            address.City = Input.Address.City;
            address.HouseNr = Input.Address.HouseNr;
            address.PostCode = Input.Address.PostCode;
            address.StreetName = Input.Address.StreetName;

            await _context.SaveChangesAsync();
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        private async Task<ImageResult> TryUploadImage(BuurtApplicatieUser user)
        {
            ImageResult result;
            var currentProfilePicture = await _context.Images.FirstOrDefaultAsync(i => i.UserId == user.Id);
            if (currentProfilePicture != null)
                result = await _fileHelper.ReplaceImageAsync(UploadedFile, currentProfilePicture);
            else
                result = await _fileHelper.GetImageFromFileAsync(UploadedFile);

            if (!result.Succeeded) return result;

            user.ProfilePicture = result.Image;
            await _context.SaveChangesAsync();
            return result;
        }
    }
}
