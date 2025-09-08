using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TNA.BLL.DTOs;

namespace TNA.APP.Models
{
    public class HomeIndexViewModel
    {
        public ClanDTO? Clan { get; set; }
        public List<PlayerRankingDTO> Rankings { get; set; } = new();
        // valores: "day", "week", "month", "all"
        public string SelectedRange { get; set; } = "day";

        public List<SelectListItem> Ranges { get; set; } = new()
        {
            new SelectListItem("Último día", "day"),
            new SelectListItem("Última semana", "week"),
            new SelectListItem("Último mes", "month"),
            new SelectListItem("Histórico", "all")
        };

        // Texto para mostrar en la vista: p. ej. "05/09/2025" o "29/08/2025 - 04/09/2025" o "Histórico"
        public string RangeDisplay { get; set; } = string.Empty;

        // Opcionales: fechas calculadas en zona (GMT-3) si las necesitas en la vista
        public DateTimeOffset? RangeStartLocal { get; set; }
        public DateTimeOffset? RangeEndLocal { get; set; }
    }
}
