using System.Reflection;

namespace Light.Extensions;

internal static class AsemblyTypeExtensions
{
    internal static IEnumerable<Type> GetAssignableFrom<T>(Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            // get from all assembly if not define assemblies to scan
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
        }

        // get all type inherit from T
        return assemblies
            .Distinct()
            .SelectMany(s => s.GetTypes())
            .Where(x =>
                typeof(T).IsAssignableFrom(x)
                && x.IsClass && !x.IsAbstract && !x.IsGenericType)
            .Distinct();
    }
}
