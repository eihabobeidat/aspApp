using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        
        private IMapper _mapper;
        public MessagesController( IUnitOfWork unitOfWork, IMapper mapper )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage( CreateMessageDTO messageDTO )
        {
            var username = User.GetUsername();

            if ( username == messageDTO.RecipientUsername.ToLower() )
                return BadRequest("You can't send messages to your self");

            var sender = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unitOfWork.UserRepository.GetUserByUsernameAsync(messageDTO.RecipientUsername);

            if ( recipient == null )
                return NotFound();

            var message = new Message
            {
                Content = messageDTO.Content,
                Sender = sender,
                Recipient = recipient,
                SenderUserName = username,
                RecipientUserName = messageDTO.RecipientUsername,
            };

            _unitOfWork.MessageRepository.AddMessage(message);

            if ( await _unitOfWork.Complete() )
            {
                return Ok(_mapper.Map<MessageDTO>(message));
            }
            return BadRequest("Failed to send Message");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MessageDTO>>> GetMessagesForUser( [FromQuery] MessageParams messageParams )
        {
            messageParams.Username = User.GetUsername();

            var messages = await _unitOfWork.MessageRepository.GetMessageForUserAsync(messageParams);

            Response.AddPaginationHeader(
                new PaginationHeader(
                    messages.CurrentPage,
                    messages.PageSize,
                    messages.TotalCount,
                    messages.TotalPages
                    )
                );

            return Ok(messages);
        }

        /*[HttpGet("thread/{recipient}")] not used any more => signalR took the responsability
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread( string recipient )
        {
            var currentUsername = User.GetUsername();
            return Ok(await _unitOfWork.MessageRepository.GetMessageThreadAsync(currentUsername, recipient));
        }*/

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage( int id )
        {
            var username = User.GetUsername();
            var message = await _unitOfWork.MessageRepository.GetMessageAsync(id);

            if ( message.SenderUserName != username && message.RecipientUserName != username )
                return Unauthorized();

            if ( message.SenderUserName == username )
                message.SenderDeleted = true;
            if ( message.RecipientUserName == username )
                message.RecipientDeleted = true;

            if ( message.SenderDeleted && message.RecipientDeleted )
                _unitOfWork.MessageRepository.RemoveMessage(message);

            if ( await _unitOfWork.Complete() )
                return Ok();
            return BadRequest("Could not delete the message");
        }
    }
}
