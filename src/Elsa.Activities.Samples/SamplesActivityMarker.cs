namespace Elsa.Activities.Samples;

/// <summary>
/// Marker type used for assembly scanning:
/// <c>services.AddActivitiesFrom&lt;SamplesActivityMarker&gt;()</c> (compile-time)
/// or <c>services.AddActivitiesFrom(typeof(SamplesActivityMarker).Assembly)</c> (runtime).
/// </summary>
public static class SamplesActivityMarker
{
}
