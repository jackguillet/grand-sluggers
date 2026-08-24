using System.Diagnostics;
using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class UnityCompileTests
{
    [Fact]
    public void UnitySimRuntimeAndEditorCompile()
    {
        var data = ContentCatalog.Load().Root;
        var repo = Directory.GetParent(data)?.FullName
            ?? throw new InvalidOperationException("no repo root");
        var sh = Path.Combine(repo, "tools", "unity-compile.sh");
        Assert.True(File.Exists(sh), sh);
        var psi = new ProcessStartInfo("/bin/zsh", sh)
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("unity-compile failed to start");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        Assert.True(p.WaitForExit(120000), "unity-compile timed out");
        Assert.True(p.ExitCode == 0, stdout + stderr);
        Assert.Contains("OK", stdout);
    }
}
