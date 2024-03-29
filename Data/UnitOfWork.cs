﻿using API.Interfaces;
using API.Repository;
using AutoMapper;

namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IMapper _mapper;

        private readonly DataContext _context;
        public IUserRepository UserRepository => new UserRepository(_context, _mapper);

        public IMessageRepository MessageRepository => new MessageRepository(_context, _mapper);

        public ILikeRepository LikeRepository => new LikeRepository(_context);

        public UnitOfWork(IMapper mapper, DataContext dataContext)
        {
            _context = dataContext;
            _mapper = mapper;
        }

        public async Task<bool> Complete( )
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public bool HasChanges( )
        {
            return _context.ChangeTracker.HasChanges();
        }
    }
}
