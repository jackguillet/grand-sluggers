using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A writer, not a check: it rewrites a checked-in fixture or exports evidence from the current code, and runs only
/// when its environment variable is set. Otherwise xUnit reports it skipped, so a writer never counts as a pass.
/// Fixture writers run through <c>tools/regenerate.sh</c>.
/// </summary>
sealed class WriterFactAttribute : FactAttribute
{
    public WriterFactAttribute(string variable)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            Skip = "a writer: set " + variable + " to run it (tools/regenerate.sh)";
    }
}
