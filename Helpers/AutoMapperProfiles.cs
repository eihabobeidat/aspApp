using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles( )
        {
            CreateMap<AppUser, MemberDTO>()
                .ForMember(
                    destination => destination.PhotoUrl,
                    option => option
                        .MapFrom(source => source.Photos
                            .FirstOrDefault(photo => photo.isMain).Url
                    )
                );

            CreateMap<Photo, PhotoDTO>();

            CreateMap<MemberUpdateDTO, AppUser>();

            CreateMap<RegisterDTO, AppUser>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.username))
                //.ForMember(d => d.Password, o => o.MapFrom(s => new Byte [ 1 ]))
                .ForMember(d => d.DateOfBirth, o => o.MapFrom(s => s.birthOfDate))
                .ForMember(d => d.City, o => o.MapFrom(s => s.city))
                .ForMember(d => d.Country, o => o.MapFrom(s => s.country))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.gender))
                .ForMember(d => d.KnownAs, o => o.MapFrom(s => s.knownAs));

            CreateMap<Message, MessageDTO>()
                /*.ForMember(m => m.RecipientUserName, o => o.MapFrom(s => s.Recipient.KnownAs))
                .ForMember(m => m.SenderUserName, o => o.MapFrom(s => s.Sender.KnownAs))*/
                .ForMember(m => m.RecipientPhotoUrl, o => o.MapFrom(s => s.Recipient.Photos.FirstOrDefault(x => x.isMain).Url))
                .ForMember(m => m.SenderPhotoUrl, o => o.MapFrom(s => s.Sender.Photos.FirstOrDefault(x => x.isMain).Url));

            CreateMap<DateTime, DateTime>().ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
            CreateMap<DateTime?, DateTime?>()
                .ConvertUsing(d => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null);


        }
    }
}
