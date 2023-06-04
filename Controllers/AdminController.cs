
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        public AdminController( UserManager<AppUser> userManager )
        {
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles( )
        {
            var users = await _userManager.Users
                .OrderBy(x => x.UserName)
                .Select(x => new
                {
                    x.Id,
                    Username = x.UserName,
                    Roles = x.UserRoles.Select(x => x.Role.Name).ToList()
                }
                ).ToListAsync();
            
            return Ok(users);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery]string roles )
        {
            if(string.IsNullOrEmpty(username)) return BadRequest("No username where included");

            if ( string.IsNullOrEmpty(roles) )
                return BadRequest("No Roles where included");

            var user = await _userManager.FindByNameAsync(username.ToLower());

            if ( user == null )
                return NotFound("user not found");
            
            var selectedRoles = roles.Split(',').ToArray();

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if ( !result.Succeeded )
                return BadRequest("Failed to add the specified roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if ( !result.Succeeded )
                return BadRequest("Failed to remove the specified roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModerator( )
        {
            return Ok("Admins and moderators can see this response");

        }
    }
}
