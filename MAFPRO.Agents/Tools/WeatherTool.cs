using System.ComponentModel;

namespace MAFPRO.Agents.Tools;

public class WeatherTool
{
    [Description("Obtiene el clima actual para una ciudad especificada.")]
    public string GetWeather([Description("La ciudad, por ejemplo, Seattle")] string city)
    {
        // En un escenario real esto consumiría una API externa
        return $"El clima en {city} es de 25 grados y soleado.";
    }
}
