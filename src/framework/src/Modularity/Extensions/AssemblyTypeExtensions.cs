using System.Reflection;

namespace Light.Extensions;

internal static class AssemblyTypeExtensions
{
    internal static IEnumerable<Type> GetAssignableFrom<T>(Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            // get from all assembly if not define assemblies to scan
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        // get all type inherit from T
        return assemblies
            .Distinct()
            .SelectMany(s => s.GetLoadableTypes())
            .Where(x =>
                typeof(T).IsAssignableFrom(x)
                && x.IsClass && !x.IsAbstract && !x.IsGenericType)
            .Distinct();
    }

    /// <summary>
    /// Returns the types of <paramref name="assembly"/> that could be loaded. When some types fail to load
    /// (e.g. a missing optional dependency), <see cref="Assembly.GetTypes"/> throws
    /// <see cref="ReflectionTypeLoadException"/>; the successfully loaded types are returned instead.
    /// </summary>
    internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
