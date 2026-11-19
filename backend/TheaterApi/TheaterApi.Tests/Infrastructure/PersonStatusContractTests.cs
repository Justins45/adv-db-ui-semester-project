using TheaterApi.Services;

namespace TheaterApi.Tests.Infrastructure;

public sealed class PersonStatusContractTests
{
    [Fact]
    public void Row_9_6_PatronServiceExposesPersonStatusThroughInterface()
    {
        Assert.NotNull(typeof(PatronService).GetMethod("PersonStatusAsync"));
        Assert.Contains(typeof(PatronService).GetInterfaces(), service => service.Name == "IPatronService");
    }

    [Theory]
    [InlineData(false, "Patron Only")]
    [InlineData(true, "Staff member")]
    public void Row_9_6_StatusMatchesPhase3SubclassResults(bool hasStaffRecord, string expected)
    {
        Assert.Equal(expected, PatronService.PersonStatus(hasStaffRecord));
    }
}
