using System.ComponentModel;

namespace MAFPRO.Agents.Tools;

public class MathTool
{
    [Description("Suma dos numeros y devuelve el resultado.")]
    public double Add(
        [Description("Primer numero")] double a,
        [Description("Segundo numero")] double b)
    {
        return a + b;
    }
}
