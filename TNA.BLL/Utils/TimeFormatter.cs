using System;

namespace TNA.BLL.Utils
{
    public static class TimeFormatter
    {
        /// <summary>
        /// Convierte segundos (puede ser decimal) a formato "HH:mm:ss".
        /// Usa horas totales (no reinicia a 00 cada 24h).
        /// </summary>
        public static string FormatSecondsToHms(decimal seconds)
        {
            if (seconds <= 0m)
                return "00:00:00";

            var ts = TimeSpan.FromSeconds((double)seconds);
            int hours = (int)ts.TotalHours;
            return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, ts.Minutes, ts.Seconds);
        }
    }
}
