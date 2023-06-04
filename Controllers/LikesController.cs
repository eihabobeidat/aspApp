using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IUserRepository _userRepository;
        public LikesController( IUserRepository userRepository, ILikeRepository likeRepository )
        {
            _likeRepository = likeRepository;
            _userRepository = userRepository;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike( string username )
        {
            var sourceUserId = User.GetIdentity();
            var LikedUser = await _userRepository.GetUserByUsernameAsync(username);
            var sourceUser = await _likeRepository.GetUserWithLikes(sourceUserId);

            if ( LikedUser == null )
                return NotFound();

            if ( sourceUser.UserName == username )
                return BadRequest("You cannot like yourself");


            var userLike = await _likeRepository.GetUserLike(sourceUserId, LikedUser.Id);

            if ( userLike != null )
                return BadRequest($"User with username {username} Has been liked");

            userLike = new UserLike { SourceUser = sourceUser, SourceUserId = sourceUserId, TargetUser = LikedUser, TargetUserId = LikedUser.Id };

            sourceUser.LikedUsers.Add(userLike);

            if ( await _userRepository.SaveAllAsync() )
                return Ok();
            return BadRequest("Failed to like " + username);
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<LikeDTO>>> GetUserLikes([FromQuery] LikeParams likeParams )
        {
            likeParams.UserId = User.GetIdentity();
            var users = await _likeRepository.GetUserLikes(likeParams);
            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
            return Ok(users);
        }
    }
}
