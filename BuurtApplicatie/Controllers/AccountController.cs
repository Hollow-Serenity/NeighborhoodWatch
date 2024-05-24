using System;
using System.Linq;
using System.Threading.Tasks;
using BuurtApplicatie.Areas.Identity.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuurtApplicatie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<BuurtApplicatieUser> _userManager;
        private readonly BuurtApplicatieDbContext _context;

        public AccountController(UserManager<BuurtApplicatieUser> userManager,
            BuurtApplicatieDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        
        // DELETE: api/Account/ProfilePicture
        [HttpDelete("ProfilePicture")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_context.Images.Any(i => i.UserId == user.Id)) return NotFound();
            
            try
            {
                await _context.Entry(user).Reference(u => u.ProfilePicture).LoadAsync();
                _context.Images.Remove(user.ProfilePicture);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
        
        [AcceptVerbs("GET", "POST")]
        public async Task<bool> UsernameIsValid([FromQuery(Name = "Input.Username")] string username)
        {
            return !string.IsNullOrEmpty(username) && await _userManager.FindByNameAsync(username) == null;
        }
    }
}