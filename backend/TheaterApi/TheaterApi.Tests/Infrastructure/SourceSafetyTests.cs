namespace TheaterApi.Tests.Infrastructure;

public sealed class SourceSafetyTests
{
    [Fact]
    public void Row_0_8_TestFactoryDoesNotContainAFixedJwtSigningKey()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TheaterApi.Tests", "Infrastructure", "TheaterApiFactory.cs");
        var source = File.ReadAllText(Path.GetFullPath(path));
        Assert.DoesNotContain("SigningKey\"] = \"", source, StringComparison.Ordinal);
    }
}
