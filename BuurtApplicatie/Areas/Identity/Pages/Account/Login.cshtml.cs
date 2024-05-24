using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BuurtApplicatie.Helpers;
using BuurtApplicatie.Models;

namespace BuurtApplicatie.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<BuurtApplicatieUser> _userManager;
        private readonly SignInManager<BuurtApplicatieUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private RecaptchaService recaptchaService;
        private readonly BuurtApplicatieDbContext _context;
        public LoginModel(SignInManager<BuurtApplicatieUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<BuurtApplicatieUser> userManager,
            RecaptchaService service, 
            BuurtApplicatieDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            recaptchaService = service;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vul een e-mailadres in.")]
            [EmailAddress(ErrorMessage = "Dit is geen geldig e-mailadres.")]
            [Display(Name = "E-mailadres")]
            public string Email { get; set; }
            
            [BindProperty(Name = "g-recaptcha-response")]
            public string Token { get; set; }
            
            [Required(ErrorMessage = "Vul een wachtwoord in.")]
            [DataType(DataType.Password)]
            [Display(Name = "Wachtwoord")]
            public string Password { get; set; }

            [Display(Name = "Ingelogd blijven")]
            public bool RememberMe { get; set; }
            public int InvalidAttempts { get; set; } = 0;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            HttpContext.Session.SetString("InvalidAttempts", "0");
            ViewData["InvalidAttempts"] = 0;
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            string token = Request.Form["g-recaptcha-response"].ToString();

            if (HttpContext.Session.GetInt32("Wrong") != null && HttpContext.Session.GetInt32("Wrong") > 2)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    ModelState.AddModelError(string.Empty, "Prove you are not robot.");
                    return Page();
                }
                var r = recaptchaService.Verification(token);

                if (!r.Result.success)
                {
                    ModelState.AddModelError(string.Empty, "Prove you are not robot.");
                    return Page();
                }
            }

            var i = Input.Token;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Mislukte inlogpoging.");
                    return Page();
                }
                
                var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    HttpContext.Session.Remove("Wrong");

                    await EnsureUserHasAnonymousId(user);

                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    if (HttpContext.Session.GetInt32("Wrong") == null)
                    {
                        HttpContext.Session.SetInt32("Wrong", 1);
                    }
                    else
                    {
                        var count = HttpContext.Session.GetInt32("Wrong");

                        count += 1;
                        HttpContext.Session.SetInt32("Wrong", count.Value);

                    }
                    ModelState.AddModelError(string.Empty, "Mislukte inlogpoging.");
                    var currentEmail = HttpContext.Session.GetString("CurrentEmail");

                    var currentAttemptCount = HttpContext.Session.GetString("InvalidAttempts");
                    var resultCount = 0;
                    if (!string.IsNullOrEmpty(currentAttemptCount))
                    {
                        if (!string.IsNullOrEmpty(currentEmail) && currentEmail.Equals(Input.Email))
                        {
                            resultCount = Convert.ToInt32(currentAttemptCount) + 1;
                            HttpContext.Session.SetString("InvalidAttempts", resultCount.ToString());
                            ViewData["InvalidAttempts"] = resultCount;
                        }
                        else
                        {
                            HttpContext.Session.SetString("InvalidAttempts", "1");
                            ViewData["InvalidAttempts"] = 1;
                        }

                    }
                    else
                    {
                        HttpContext.Session.SetString("InvalidAttempts", "1");
                        ViewData["InvalidAttempts"] = 1;
                    }
                    HttpContext.Session.SetString("CurrentEmail", Input.Email);
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private async Task EnsureUserHasAnonymousId(BuurtApplicatieUser user)
        {
            var anonUser = _context.AnonUsers.SingleOrDefault(u => u.UserId == user.Id);
            if (anonUser == null)
            {
                _logger.LogWarning("User did not have an anonymous user id upon login.");
                anonUser = new AnonymousUser {UserId = user.Id};
                _context.AnonUsers.Add(anonUser);
                await _context.SaveChangesAsync();
            }
        }
    }
}
