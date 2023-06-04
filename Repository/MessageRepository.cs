using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository( DataContext context, IMapper mapper )
        {
            _context = context;
            _mapper = mapper;
        }

        public void AddMessage( Message message )
        {
            _context.Messages.Add(message);

        }

        public void RemoveMessage( Message message )
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessageAsync( int id )
        {
            return await _context.Messages.FindAsync(id);
        }

        public async Task<PagedList<MessageDTO>> GetMessageForUserAsync( MessageParams messageParams )
        {
            var query = _context.Messages.OrderByDescending(x => x.MessageSent).AsQueryable();
            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipientUserName == messageParams.Username && !u.RecipientDeleted),
                "Outbox" => query.Where(u => u.SenderUserName == messageParams.Username && !u.SenderDeleted),
                _ => query.Where(u => u.RecipientUserName == messageParams.Username && !u.RecipientDeleted && u.DateRead == null),
            };
            var messages = query.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider);
            return await PagedList<MessageDTO>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDTO>> GetMessageThreadAsync( string currentUsername, string recipientUsername )
        {
            var query = _context.Messages
                .Where(m =>
                    ( m.RecipientUserName == currentUsername
                    && !m.RecipientDeleted
                    && m.SenderUserName == recipientUsername )
                    ||
                    ( m.RecipientUserName == recipientUsername
                    && !m.SenderDeleted
                    && m.SenderUserName == currentUsername )
                ).OrderBy(x => x.MessageSent)
                .AsQueryable();

            //var messages = await query.ToListAsync();

            // now we will mark these messages as read from the current user side
            var unreadMessages = query
                .Where(x =>
                    x.DateRead == null &&
                    x.RecipientUserName == currentUsername
                ).ToList();

            if ( unreadMessages.Any() )
            {
                unreadMessages.ForEach(x =>
                                {
                                    x.DateRead = DateTime.UtcNow;
                                });
                //await _context.SaveChangesAsync(); this will destroy the unit of work concept
            }
            // till here __!
            return await query.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public void AddGroup( Group group )
        {
            _context.Groups.Add(group);
        }

        public void RemoveConnection( Connection connection )
        {
            _context.Connections.Remove(connection);
        }

        public async Task<Connection> GetConnectionAsync( string connectionId )
        {
            return await _context.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetMessageGroupAsync( string groupName )
        {
            return await _context.Groups
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<Group> GetGroupForConnectionAsync( string connectionId )
        {
            return await _context.Groups
                .Include(x => x.Connections)
                .Where(x => x.Connections.Any(c => c.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }
    }
}
