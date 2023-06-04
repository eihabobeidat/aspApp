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
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private IMapper _mapper;
        public MessagesController( IMessageRepository messageRepository, IUserRepository userRepository, IMapper mapper )
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage( CreateMessageDTO messageDTO )
        {
            var username = User.GetUsername();

            if ( username == messageDTO.RecipientUsername.ToLower() )
                return BadRequest("You can't send messages to your self");

            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipient = await _userRepository.GetUserByUsernameAsync(messageDTO.RecipientUsername);

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

            _messageRepository.AddMessage(message);

            if ( await _messageRepository.SaveAllAsync() )
            {
                return Ok(_mapper.Map<MessageDTO>(message));
            }
            return BadRequest("Failed to send Message");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MessageDTO>>> GetMessagesForUser( [FromQuery] MessageParams messageParams )
        {
            messageParams.Username = User.GetUsername();

            var messages = await _messageRepository.GetMessageForUserAsync(messageParams);

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

        [HttpGet("thread/{recipient}")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread( string recipient )
        {
            var currentUsername = User.GetUsername();
            return Ok(await _messageRepository.GetMessageThreadAsync(currentUsername, recipient));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage( int id )
        {
            var username = User.GetUsername();
            var message = await _messageRepository.GetMessageAsync(id);

            if ( message.SenderUserName != username && message.RecipientUserName != username )
                return Unauthorized();

            if ( message.SenderUserName == username )
                message.SenderDeleted = true;
            if ( message.RecipientUserName == username )
                message.RecipientDeleted = true;

            if ( message.SenderDeleted && message.RecipientDeleted )
                _messageRepository.RemoveMessage(message);

            if ( await _messageRepository.SaveAllAsync() )
                return Ok();
            return BadRequest("Could not delete the message");
        }
    }
}
