using AutoMapper;
using TNA.BLL.DTOs;
using TNA.DAL.Entities;
using System;

namespace TNA.BLL.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Clan, ClanDTO>().ReverseMap();
            CreateMap<ClanMember, ClanMemberDTO>().ReverseMap();
            CreateMap<ClanMemberSocialMedia, ClanMemberSocialMediaDTO>().ReverseMap();
            CreateMap<Match, MatchDTO>().ReverseMap();
            CreateMap<PlayerMatch, PlayerMatchDTO>().ReverseMap();

            CreateMap<User, UserDTO>().ReverseMap();

            CreateMap<UserCreateDTO, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UserUpdateDTO, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); 
        }
    }
}
