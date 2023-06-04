using API.Entities;

namespace API.Interfaces
{
    public interface ITokenService
    {
        public Task<string> CreateToken( AppUser user );
    }
}
