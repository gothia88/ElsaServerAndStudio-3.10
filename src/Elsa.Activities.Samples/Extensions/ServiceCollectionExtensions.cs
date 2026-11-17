using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Samples;

/// <summary>
/// Compile-time registration helper (used when the plugin is a ProjectReference or a NuGet
/// package referenced directly by the host). For runtime loading (plugins/ folder) the host
/// scans the assembly automatically; this method is only called for the compile-time path.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSamplesActivities(this IServiceCollection services)
    {
        // Register any services the activities depend on here (e.g. HttpClient, repositories).
        // Activity type scanning itself is done by the host via AddActivitiesFrom<TMarker>().
        return services;
    }
}
