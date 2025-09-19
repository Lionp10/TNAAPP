using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TNA.BLL.DTOs;

namespace TNA.APP.Models
{
    public class HomeIndexViewModel
    {
        public ClanDTO? Clan { get; set; }
        public List<PlayerRankingDTO> Rankings { get; set; } = new();
        public string SelectedRange { get; set; } = "day";

        public List<SelectListItem> Ranges { get; set; } = new()
        {
            new SelectListItem("Último día", "day"),
            new SelectListItem("Última semana", "week"),
            new SelectListItem("Último mes", "month"),
            new SelectListItem("Histórico", "all")
        };

        public string RangeDisplay { get; set; } = string.Empty;
        public DateTimeOffset? RangeStartLocal { get; set; }
        public DateTimeOffset? RangeEndLocal { get; set; }
    }
}
