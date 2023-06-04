using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;
        public MessageHub( IMessageRepository messageRepository, IUserRepository userRepository, IMapper mapper, IHubContext<PresenceHub> presenceHub )
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _presenceHub = presenceHub;
        }

        public override async Task OnConnectedAsync( )
        {
            var currentUser = Context.User.GetUsername();
            var otherUser = Context.GetHttpContext().Request.Query [ "user" ];
            var groupName = GetGroupName(currentUser, otherUser);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroupForThread", group);

            var messages = await _messageRepository.GetMessageThreadAsync(currentUser, otherUser);

            await Clients.Caller.SendAsync("RecieveMessageThread", messages);
        }

        public async Task SendMessage( CreateMessageDTO messageDTO )
        {
            var username = Context.User.GetUsername();

            if ( username == messageDTO.RecipientUsername.ToLower() )
                throw new HubException("You can't send messages to your self");

            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipient = await _userRepository.GetUserByUsernameAsync(messageDTO.RecipientUsername);

            if ( recipient == null )
                throw new HubException("recipient not found");

            var message = new Message
            {
                Content = messageDTO.Content,
                Sender = sender,
                Recipient = recipient,
                SenderUserName = username,
                RecipientUserName = messageDTO.RecipientUsername,
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _messageRepository.GetMessageGroupAsync(groupName);

            if ( group.Connections.Any(x => x.Username == recipient.UserName) )
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
                if ( connections != null )
                {
                    await _presenceHub.Clients.Clients(connections)
                        .SendAsync("NewMessageNotification", new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _messageRepository.AddMessage(message);

            if ( await _messageRepository.SaveAllAsync() )
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
                return;
            }
            throw new HubException("Failed to send Message");
        }

        public override async Task OnDisconnectedAsync( Exception exception )
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroupForThread");
            await base.OnDisconnectedAsync(exception);
        }

        private string GetGroupName( string caller, string other )
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";

        }

        private async Task<Group> AddToGroup( string groupName )
        {
            var group = await _messageRepository.GetMessageGroupAsync(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if ( group == null )
            {
                group = new Group(groupName);
                _messageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if ( await _messageRepository.SaveAllAsync() )
                return group;

            throw new HubException("Unable to add group to database");
        }

        private async Task<Group> RemoveFromMessageGroup( )
        {
            var group = await _messageRepository.GetGroupForConnectionAsync(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            
            _messageRepository.RemoveConnection(connection);

            if ( await _messageRepository.SaveAllAsync() )
                return group;

            throw new HubException("Failed to remove from group");
        }
    }
}
