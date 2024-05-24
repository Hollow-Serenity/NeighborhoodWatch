using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using BuurtApplicatie.Helpers;
using BuurtApplicatie.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace BuurtApplicatie.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<BuurtApplicatieUser> _signInManager;
        private readonly UserManager<BuurtApplicatieUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAddressValidator _addressValidator;
        private readonly BuurtApplicatieDbContext _context;

        public RegisterModel(
            UserManager<BuurtApplicatieUser> userManager,
            SignInManager<BuurtApplicatieUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            IAddressValidator addressValidator,
            BuurtApplicatieDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _addressValidator = addressValidator;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vul een e-mailadres in.")]
            [EmailAddress(ErrorMessage = "Dit is geen geldig e-mailadres.")]
            [Display(Name = "E-mailadres")]
            public string Email { get; set; }
            
            [Required(ErrorMessage = "Vul een gebruikersnaam in.")]
            [MaxLength(20, ErrorMessage = "Je gebruikersnaam mag niet langer zijn dan 20 karakters.")]
            [Display(Name = "Gebruikersnaam")]
            [Remote("UsernameIsValid", "Account", AdditionalFields = nameof(Username), ErrorMessage = "Deze gebruikersnaam is al in gebruik.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Vul een wachtwoord in.")]
            [StringLength(100, ErrorMessage = "Je wachtwoord moet minimaal {2} en maximaal {1} karakters bevatten.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord bevestigen")]
            [Compare("Password", ErrorMessage = "Wachtwoorden komen niet overeen.")]
            public string ConfirmPassword { get; set; }
            
            [Required]
            public Address Address { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            var usernameExists = await _userManager.FindByNameAsync(Input.Username) != null;
            if (usernameExists)
            {
                ModelState.AddModelError(string.Empty, "De opgegeven gebruikersnaam is al in gebruik.");
                return Page();
            }
            
            if (ModelState.IsValid)
            {
                var addressValidationResult = await _addressValidator.ValidateAddressAsync(Input.Address);
                if (!addressValidationResult.Valid)
                {
                    ModelState.AddModelError(string.Empty, addressValidationResult.Error);
                    return Page();
                }
                
                var user = new BuurtApplicatieUser
                {
                    UserName = Input.Username, 
                    Email = Input.Email,
                    Address = Input.Address
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                
                if (result.Succeeded)
                {
                    await EnsureResidentRoleExists();
                    await _userManager.AddToRoleAsync(user, "Resident");
                    
                    _context.AnonUsers.Add(new AnonymousUser {User = user});
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Bevestig uw email",
                        $"Bevestig uw email door op <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>deze email bevestigingslink</a> te klikken.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateEmail")
                    {
                        error.Description = "Het opgegeven e-mailadres is al ingebruik.";
                    }
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private async Task EnsureResidentRoleExists()
        {
            if (!await _roleManager.RoleExistsAsync("Resident"))
                await _roleManager.CreateAsync(new IdentityRole { Name = "Resident" });

        }
    }
}
