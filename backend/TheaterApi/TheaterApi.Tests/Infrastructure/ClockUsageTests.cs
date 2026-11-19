using System.Text.RegularExpressions;

namespace TheaterApi.Tests.Infrastructure;

public sealed class ClockUsageTests
{
    [Fact]
    public void ApplicationSource_HasNoDirectSystemClockCalls()
    {
        var root = FindRepositoryRoot(Directory.GetCurrentDirectory());
        var violations = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}TheaterApi.Tests{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}"))
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"\b(?:DateTime|DateTimeOffset)\.(?:UtcNow|Now)\b")
                .Select(match => $"{Path.GetRelativePath(root, path)}: {match.Value}"))
            .ToArray();

        Assert.Empty(violations);
    }

    private static string FindRepositoryRoot(string path)
    {
        var current = new DirectoryInfo(path);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TheaterApi.csproj"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate TheaterApi.csproj to run the clock source check.");
    }
}
