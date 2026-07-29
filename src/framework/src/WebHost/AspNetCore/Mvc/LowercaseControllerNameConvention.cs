using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Text;

namespace Light.AspNetCore.Mvc;

internal class LowercaseControllerNameConvention(string separate = "_") : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var controllerName = controller.ControllerName;

        if (controllerName != null)
        {
            controller.ControllerName = Convert(controllerName);
        }
    }

    private string Convert(string input)
    {
        var newValue = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
            if (char.IsUpper(input[i]))
                newValue.Append(i == 0 // first char
                    ? char.ToLower(input[i])
                    : separate + char.ToLower(input[i])); // add prefix to upper chars
            else
                newValue.Append(input[i]);

        return newValue.ToString();
    }
}
