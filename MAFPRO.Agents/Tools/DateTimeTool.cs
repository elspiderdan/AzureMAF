using System.ComponentModel;

namespace MAFPRO.Agents.Tools;

public class DateTimeTool
{
    [Description("Obtiene la fecha y hora actual del sistema. Útil para responder preguntas sobre la hora o el día actual.")]
    public string GetCurrentDateTime(
        [Description("Zona horaria en formato IANA, por ejemplo 'America/Mexico_City'. Si no se especifica, se usará UTC.")] 
        string? timeZoneId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                var utcNow = DateTime.UtcNow;
                return $"Fecha y hora actual (UTC): {utcNow:dddd, dd 'de' MMMM 'de' yyyy, HH:mm:ss} UTC";
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return $"Fecha y hora actual en {tz.DisplayName}: {localTime:dddd, dd 'de' MMMM 'de' yyyy, HH:mm:ss}";
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback si no se encuentra la zona horaria
            var now = DateTime.UtcNow;
            return $"Fecha y hora actual (UTC): {now:dddd, dd 'de' MMMM 'de' yyyy, HH:mm:ss} UTC (zona horaria '{timeZoneId}' no encontrada)";
        }
    }
}
