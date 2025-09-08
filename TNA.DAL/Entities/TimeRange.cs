using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TNA.DAL.Entities
{
    public enum TimeRange
    {
        LastDay,        // "Último día" = ayer (día calendario anterior, en zona GMT-3)
        PreviousWeek,   // "Última semana" = semana calendario anterior (Lun-Dom) en GMT-3
        LastMonth,      // "Último mes" = mes calendario anterior en GMT-3
        All             // Histórico (sin filtro de fecha)
    }
}
