using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using WebAPI.Context;
using WebAPI.Entities;

namespace WebAPI.UnitTests.Security;

public class MigrationTests
{
    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=metadata_only").Options);

    [Test]
    public void MigrationOperations_CreateEveryTableAndColumnRequiredByTheModel()
    {
        using var context = Context();
        var migrations = context.GetService<IMigrationsAssembly>();
        var columns = new HashSet<(string Table, string Column)>();
        foreach (var type in migrations.Migrations.OrderBy(pair => pair.Key).Select(pair => pair.Value))
        {
            var migration = migrations.CreateMigration(type, context.Database.ProviderName!);
            foreach (var operation in migration.UpOperations)
            {
                if (operation is CreateTableOperation table)
                    foreach (var column in table.Columns) columns.Add((table.Name, column.Name));
                if (operation is AddColumnOperation added) columns.Add((added.Table, added.Name));
                if (operation is DropColumnOperation dropped) columns.Remove((dropped.Table, dropped.Name));
                if (operation is DropTableOperation removed) columns.RemoveWhere(c => c.Table == removed.Name);
            }
        }
        var model = context.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var expected = model.Tables.SelectMany(table => table.Columns.Select(column => (table.Name, column.Name))).ToHashSet();
        Assert.That(columns, Is.EquivalentTo(expected), "Snapshots alone do not prove that a clean database contains the required schema.");
    }

    [Test]
    public void PriceTimestampConversion_PreservesUtcValueWithoutChangingTheColumnType()
    {
        using var context = Context();
        var property = context.Model.FindEntityType(typeof(RegistosPreco))!.FindProperty(nameof(RegistosPreco.DataRegisto))!;
        var converter = property.GetValueConverter()!;
        var utc = new DateTime(2026, 9, 7, 12, 30, 0, DateTimeKind.Utc);
        var stored = (DateTime)converter.ConvertToProvider(utc)!;
        Assert.That(property.GetColumnType(), Is.EqualTo("timestamp without time zone"));
        Assert.That(stored.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        Assert.That(stored.Ticks, Is.EqualTo(utc.Ticks));
        var restored = (DateTime)converter.ConvertFromProvider(stored)!;
        Assert.That(restored.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(restored, Is.EqualTo(utc));
    }
}
