using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Activities.Samples;

/// <summary>
/// Sample custom activity. Copy this pattern for your team's activities.
/// The activity type name is "Tajan.Samples.Greet" — a unique namespace keeps teams from colliding.
/// </summary>
[Activity("Tajan.Samples", "Samples", "Greet", Description = "Returns a greeting message.")]
public class Greet : CodeActivity<string>
{
    [Input(Description = "The name to greet.")]
    public Input<string> PersonName { get; set; } = new(string.Empty);

    protected override void Execute(ActivityExecutionContext context)
    {
        var name = PersonName.Get(context);
        context.SetResult($"Hello, {name}!");
    }
}
