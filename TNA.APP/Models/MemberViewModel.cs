using TNA.BLL.DTOs;

namespace TNA.APP.Models
{
    public class MemberViewModel
    {
        public ClanMemberDTO Member { get; set; }
        public List<ClanMemberSocialMediaDTO>? SocialMedias { get; set; }
    }
}
