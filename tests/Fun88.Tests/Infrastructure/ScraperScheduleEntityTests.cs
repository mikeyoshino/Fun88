namespace Fun88.Tests.Infrastructure;

using Fun88.Web.Infrastructure.Data.Entities;
using Postgrest.Attributes;
using System.Reflection;

public class ScraperScheduleEntityTests
{
    [Theory]
    [InlineData("LastRunAt", "last_run_at")]
    [InlineData("NextRunAt", "next_run_at")]
    public void ScraperSchedule_HasExpectedColumnMappings(string propertyName, string columnName)
    {
        var property = typeof(ScraperSchedule).GetProperty(propertyName);

        Assert.NotNull(property);

        var columnAttribute = property!.GetCustomAttribute<ColumnAttribute>();

        Assert.NotNull(columnAttribute);

        var nameProperty = typeof(ColumnAttribute).GetProperty("ColumnName")
            ?? typeof(ColumnAttribute).GetProperty("Name");

        Assert.NotNull(nameProperty);

        var mappedName = nameProperty!.GetValue(columnAttribute) as string;

        Assert.Equal(columnName, mappedName);
    }
}
