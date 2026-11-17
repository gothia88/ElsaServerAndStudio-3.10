using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace Tajan.Workflows.Host.PluginSystem;

/// <summary>
/// Loads activity plugin assemblies from a directory so teams can ship activity DLLs
/// without rebuilding the host. Shared Elsa/ASP.NET dependencies resolve from the host's
/// default load context.
/// </summary>
public static class ActivityPluginLoader
{
    /// <summary>
    /// Loads every *.dll in <paramref name="directory"/> into a dedicated load context.
    /// </summary>
    public static IReadOnlyList<Assembly> Load(string directory)
    {
        var fullPath = Path.GetFullPath(directory);

        if (!Directory.Exists(fullPath))
            return [];

        var loadContext = new AssemblyLoadContext("ActivityPlugins", isCollectible: false);

        // Resolve shared dependencies (Elsa, ASP.NET) from the host first.
        loadContext.Resolving += (_, name) =>
            AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a => a.GetName().Name == name.Name);

        // Skip assemblies the host already provides (e.g. Elsa.Workflows.Core) to avoid duplicate type identity.
        var alreadyLoaded = new HashSet<string>(
            AssemblyLoadContext.Default.Assemblies.Select(a => a.GetName().Name ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        var assemblies = new List<Assembly>();

        foreach (var file in Directory.GetFiles(fullPath, "*.dll"))
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(file);
                if (alreadyLoaded.Contains(assemblyName.Name ?? string.Empty))
                    continue;

                var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(file));
                assemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load activity plugin {File}.", file);
            }
        }

        return assemblies;
    }
}
