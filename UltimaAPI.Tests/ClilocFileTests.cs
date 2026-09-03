using UltimaAPI.Cliloc;
using Xunit;

namespace UltimaAPI.Tests;

public sealed class ClilocFileTests
{
    private static readonly string? ClilocPath = OperatingSystem.IsWindows()
        ? @"C:\Program Files (x86)\Electronic Arts\Ultima Online Classic\cliloc.enu"
        : null;

    [Fact]
    public void WhenClilocIsAvailableThenCompressedFileLoads()
    {
        if (ClilocPath is null || !File.Exists(ClilocPath))
        {
            return;
        }

        ClilocFile cliloc = ClilocFile.Load("enu", ClilocPath);

        Assert.True(cliloc.Entries.Count > 100_000);
        ClilocEntry? entry = cliloc.TryGetEntry(500000);
        Assert.NotNull(entry);
        Assert.False(string.IsNullOrWhiteSpace(entry.Text));
    }
}
