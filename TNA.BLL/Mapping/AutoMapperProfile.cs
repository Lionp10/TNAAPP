using AutoMapper;
using TNA.BLL.DTOs;
using TNA.DAL.Entities;

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
        }
    }
}
