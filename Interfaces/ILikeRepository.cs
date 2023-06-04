using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface ILikeRepository
    {
        public Task<UserLike> GetUserLike( int sourceUserId, int targetUserId );
        public Task<AppUser> GetUserWithLikes( int userId );
        public Task<PagedList<LikeDTO>> GetUserLikes(LikeParams likeParams);
    }
}
