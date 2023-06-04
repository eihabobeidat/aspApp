using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _token;
        private readonly IMapper _mapper;
        public AccountController( UserManager<AppUser> userManager, ITokenService token, IMapper mapper )
        {
            _userManager = userManager;
            _token = token;
            _mapper = mapper;
        }
        [HttpPost("register")] //api/account/register
        public async Task<ActionResult<UserDTO>> Register( RegisterDTO newUser )
        {
            if ( await IsUserExist(newUser.username) )
            {
                return BadRequest("Username already exist");
            }

            var user = _mapper.Map<AppUser>(newUser);

            user.UserName = newUser.username.ToLower();

            var identityResult = await _userManager.CreateAsync(user, newUser.password);

            //using var hmac = new HMACSHA512();
            //user.Password = hmac.ComputeHash(Encoding.UTF8.GetBytes(newUser.password));
            //user.PasswordSalt = hmac.Key;

            //_context.Users.Add(user); // user object is ready till this moment
            //await _context.SaveChangesAsync();

            if ( identityResult.Succeeded )
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Member");

                if ( !roleResult.Succeeded )
                    return BadRequest(roleResult.Errors);

                return new UserDTO()
                {
                    token = await _token.CreateToken(user),
                    username = user.UserName,
                    knownAs = user.KnownAs,
                    gender = user.Gender,
                };
            }
            return BadRequest(identityResult.Errors);
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login( LoginDTO loginDTO )
        {
            var user = await _userManager.Users.Include(x => x.Photos).SingleOrDefaultAsync(x => x.UserName == loginDTO.username.ToLower());

            if ( user == null )
                return Unauthorized("Invalid username");

            return await _userManager.CheckPasswordAsync(user, loginDTO.password) ?
                   new UserDTO()
                   {
                       token = await _token.CreateToken(user),
                       username = user.UserName,
                       knownAs = user.KnownAs,
                       gender = user.Gender,
                       photoUrl = user.Photos.FirstOrDefault(x => x.isMain)?.Url
                   }
                 :
                  Unauthorized("Invalid Credintials");
        }

        private async Task<bool> IsUserExist( string username )
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }

        /*private bool ValidatePassword( string password, AppUser user )
        {
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var a = user.Password;
            var b = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            var c = Encoding.UTF8.GetString(a);
            var d = Encoding.UTF8.GetString(b);

            return c == d;
        }*/
    }
}
