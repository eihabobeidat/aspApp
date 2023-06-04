using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController( IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDTO>>> GetUsers([FromQuery] UserParams userParams )
        {
            var currentUser = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            userParams.CurrentUsername = currentUser.UserName;

            if ( !string.IsNullOrEmpty(userParams.Gender) )
                userParams.Gender = userParams.Gender == "male" ? "female" : "male";
            else
                userParams.Gender = "female";

            var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
            return Ok(users);
        }

        [HttpGet("id/{identity}")]
        public async Task<ActionResult<MemberDTO>> GetUser( int identity )
        {
            var user = await _unitOfWork.UserRepository.GetUserByIdAsync(identity);
            return Ok(_mapper.Map<MemberDTO>(user));
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<MemberDTO>> GetUser( string name )
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(name);
            return Ok(_mapper.Map<MemberDTO>(user));
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser( MemberUpdateDTO memberUpdate )
        {
            var username = User.GetUsername();
            if ( username != null )
            {
                var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
                if ( user != null )
                {
                    _mapper.Map(memberUpdate, user);
                    //_userRepository.Update(_mapper.Map(memberUpdate, user));
                    if ( await _unitOfWork.Complete() )
                    {
                        return NoContent();
                    }
                    return BadRequest("Failed to update database");
                }
                return NotFound();
            }
            return BadRequest("Failed to extract username");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto( IFormFile file )
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            if ( user == null )
                return NotFound();

            var result = await _photoService.AddPhotoAsync(file);

            if ( result.Error != null )
                return BadRequest(result.Error.Message);

            Photo photo = new()
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId,
                isMain = user.Photos.Count < 1,
            };

            user.Photos.Add(photo);

            if ( await _unitOfWork.Complete() )
                return CreatedAtAction( //this will return 201, with info about where to find the new resource & will send back the new created resource as well
                    nameof(GetUser),
                    new { name = user.UserName },
                    _mapper.Map<PhotoDTO>(photo)
                    );

            return BadRequest();
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto( int photoId )
        {
            var username = User.GetUsername();
            if ( username != null )
            {
                var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
                if ( user != null )
                {
                    var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
                    if ( photo == null )
                        return NotFound();

                    if ( photo.isMain )
                        return BadRequest("This is already the main photo");

                    var currentPhoto = user.Photos.FirstOrDefault(x => x.isMain);
                    if ( currentPhoto != null )
                        currentPhoto.isMain = false;

                    photo.isMain = true;

                    if ( await _unitOfWork.Complete() )
                        return NoContent();
                    return BadRequest("Could not set new main photo");
                }
                return NotFound();
            }
            return BadRequest("Failed to extract username");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto( int photoId )
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            if ( user == null )
                return NotFound();

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if ( photo == null )
                return NotFound();
            if ( photo.isMain )
                return BadRequest("Can not delete the main photo");
            user.Photos.Remove(photo);

            if ( photo.PublicId != null )
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if ( result.Error != null )
                    return BadRequest(result.Error.Message);
            }

            if ( await _unitOfWork.Complete() )
                return Ok(); //no content here is better
            return BadRequest("issue with deleting the photo from database");
        }
    }
}
