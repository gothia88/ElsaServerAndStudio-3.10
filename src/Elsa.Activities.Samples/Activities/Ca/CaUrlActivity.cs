using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Activities.Samples.Ca;

/// <summary>
/// Converted from the "TJ.CA Url" workflow. Returns the application, CNF and MMG base URLs.
/// </summary>
[Activity("Tajan.CA", "Tajan.CA", "TJ.CA Url", Description = "Returns the application, CNF and MMG base URLs.")]
public class CaUrlActivity : CodeActivity
{
    [Input(Description = "The application base URL.")]
    public Input<string> AppUrl { get; set; } = new("http://req.test.twp.tajan.local");

    [Input(Description = "The CNF base URL.")]
    public Input<string> CnfUrl { get; set; } = new("http://cnf.test.twp.tajan.local");

    [Input(Description = "The MMG base URL.")]
    public Input<string> MmgUrl { get; set; } = new("http://mmg.test.isms.tajan.local");

    [Output] public Output<string> App { get; set; } = default!;
    [Output] public Output<string> Cnf { get; set; } = default!;
    [Output] public Output<string> Mmg { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        App.Set(context, AppUrl.Get(context));
        Cnf.Set(context, CnfUrl.Get(context));
        Mmg.Set(context, MmgUrl.Get(context));
    }
}
