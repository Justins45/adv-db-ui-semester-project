using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TheaterApi.Data;
using TheaterApi.Models;

namespace TheaterApi.Tests.Orders;

public sealed class XminMappingTests
{
    [Theory]
    [InlineData(typeof(Cart))]
    [InlineData(typeof(PerformanceSeat))]
    public void Row_6_3_XminProperty_IsConcurrencyTokenAndMapsToSystemColumn(Type entityType)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata_only;Username=none;Password=none")
            .Options;
        using var db = new AppDbContext(options);
        var entity = db.Model.FindEntityType(entityType)!;
        var property = entity.FindProperty(nameof(Cart.Xmin))!;
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal("xmin", property.GetColumnName(table));
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }
}
