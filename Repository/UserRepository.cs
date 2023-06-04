using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Repository
{
    public class UserRepository : IUserRepository
    {
        private DataContext _dataContext;
        private IMapper _mapper;
        public UserRepository( DataContext dataContext, IMapper mapper )
        {
            _dataContext = dataContext;
            _mapper = mapper;
        }

        public async Task<MemberDTO> GetMemberAsync( string username )
        {
            //here we will intreduce two ways to do this, (without mapper) braa
            return await _dataContext.Users
                .Where(x => x.UserName == username) //after the where will be project to in case of mapper
                .Select(user => new MemberDTO
                {
                    UserName = user.UserName,
                    Age = user.GetAge(),
                    City = user.City,
                    Country = user.Country,
                    Created = user.Created,
                    Gender = user.Gender,
                    Id = user.Id,
                    Interests = user.Interests,
                    Introduction = user.Introduction,
                    KnownAs = user.KnownAs,
                    LastActive = user.LastActive,
                    LookingFor = user.LookingFor,
                    PhotoUrl = user.Photos.Find(photo => photo.isMain).Url
                }).SingleOrDefaultAsync();
        }

        public async Task<PagedList<MemberDTO>> GetMembersAsync( UserParams userParams )
        {
            //here we will intreduce two ways to do this, (with mapper) ;) how cool
            var minimumDateOfBirth = DateOnly.FromDateTime( DateTime.Today.AddYears(-userParams.MaximumAge -1));
            var maximumDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MinimumAge));

            var query = _dataContext.Users
                .AsQueryable();
            query = query.Where(u => u.UserName != userParams.CurrentUsername);
            query = query.Where(u => u.Gender == userParams.Gender);
            query = query.Where(u => u.DateOfBirth >= minimumDateOfBirth && u.DateOfBirth <= maximumDateOfBirth);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive),
            };

            return await PagedList<MemberDTO>
                .CreateAsync(
                    query.AsNoTracking()
                        .ProjectTo<MemberDTO>(_mapper.ConfigurationProvider),
                    userParams.PageNumber,
                    userParams.PageSize
                );
        }

        public async Task<AppUser> GetUserByIdAsync( int id )
        {
            return await _dataContext.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync( string username )
        {
            return await _dataContext.Users.Include(p => p.Photos).SingleOrDefaultAsync(x => x.UserName == username.ToLower());
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync( )
        {
            return await _dataContext.Users.Include(p => p.Photos).ToListAsync();
        }

        public void Update( AppUser user )
        {
            _dataContext.Entry(user).State = EntityState.Modified;
        }
    }
}
