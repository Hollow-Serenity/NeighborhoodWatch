using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BuurtApplicatie.Areas.Identity.Pages.Account.Manage
{
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<BuurtApplicatieUser> _userManager;
        private readonly ILogger<DownloadPersonalDataModel> _logger;
        private readonly BuurtApplicatieDbContext _context;

        public DownloadPersonalDataModel(
            UserManager<BuurtApplicatieUser> userManager,
            ILogger<DownloadPersonalDataModel> logger,
            BuurtApplicatieDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation("User with ID '{UserId}' asked for their personal data.", _userManager.GetUserId(User));

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(BuurtApplicatieUser).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var posts = _context.Posts.Where(p => p.AuthorId == user.Id).ToList();
            var comments = _context.Comments.Where(p => p.AuthorId == user.Id).ToList();

            await _context.Entry(user).Reference(u => u.Address).LoadAsync();

            var address = user.Address;
            var DataRelatedToUser = new
            {
                PersonalData = personalData,
                Address = address,
                Meldingen = posts,
                Comments = comments
            };

            var logins = await _userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(DataRelatedToUser, options), "application/json");
        }
    }
}
