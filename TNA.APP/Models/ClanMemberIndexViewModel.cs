using System.Collections.Generic;

namespace TNA.APP.Models
{
    public class ClanMemberIndexViewModel : PagedListViewModel<MemberViewModel>
    {
        public ClanMemberIndexViewModel() : base() { }
        public ClanMemberIndexViewModel(IEnumerable<MemberViewModel> items, int page, int pageSize, int totalItems)
            : base(items, page, pageSize, totalItems) { }
    }
}
