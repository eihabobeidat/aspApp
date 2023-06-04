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
        private readonly IUnitOfWork _unitOfWork;
        
        public LikesController( IUnitOfWork unitOfWork )
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike( string username )
        {
            var sourceUserId = User.GetIdentity();
            var LikedUser = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var sourceUser = await _unitOfWork.LikeRepository.GetUserWithLikes(sourceUserId);

            if ( LikedUser == null )
                return NotFound();

            if ( sourceUser.UserName == username )
                return BadRequest("You cannot like yourself");


            var userLike = await _unitOfWork.LikeRepository.GetUserLike(sourceUserId, LikedUser.Id);

            if ( userLike != null )
                return BadRequest($"User with username {username} Has been liked");

            userLike = new UserLike { SourceUser = sourceUser, SourceUserId = sourceUserId, TargetUser = LikedUser, TargetUserId = LikedUser.Id };

            sourceUser.LikedUsers.Add(userLike);

            if ( await _unitOfWork.Complete() )
                return Ok();
            return BadRequest("Failed to like " + username);
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<LikeDTO>>> GetUserLikes([FromQuery] LikeParams likeParams )
        {
            likeParams.UserId = User.GetIdentity();
            var users = await _unitOfWork.LikeRepository.GetUserLikes(likeParams);
            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
            return Ok(users);
        }
    }
}
