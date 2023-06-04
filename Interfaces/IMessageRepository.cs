using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IMessageRepository
    {
        public void AddMessage( Message message );
        public void RemoveMessage( Message message );
        public Task<Message> GetMessageAsync( int id );
        public Task<PagedList<MessageDTO>> GetMessageForUserAsync( MessageParams messageParams );
        public Task<IEnumerable<MessageDTO>> GetMessageThreadAsync( string currentUsername, string recipientUsername );
        void AddGroup( Group group );
        void RemoveConnection( Connection connection );
        Task<Connection> GetConnectionAsync( string connectionId );
        Task<Group> GetMessageGroupAsync( string groupName );
        Task<Group> GetGroupForConnectionAsync( string connectionId );
    }
}
