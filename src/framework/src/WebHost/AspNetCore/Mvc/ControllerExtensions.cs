using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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
        string newValue = "";

        for (int i = 0; i < input.Length; i++)
            if (char.IsUpper(input[i]))
                newValue += i == 0 // first char
                    ? char.ToLower(input[i])
                    : separate + char.ToLower(input[i]); // add prefix to upper chars
            else
                newValue += input[i];

        return newValue;
    }
}

public class OrderedPropertiesJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var order = 0;
        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);
        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            foreach (JsonPropertyInfo property in typeInfo.Properties.OrderBy(a => a.Name))
            {
                property.Order = order++;
            }
        }
        return typeInfo;
    }
}


