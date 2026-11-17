using KiraTakip.Data;
using KiraTakip.Migrations;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseUnitSnapshotBackfillTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public LeaseUnitSnapshotBackfillTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrationSql_ShouldBackfillAllSnapshotsIncludingDeletedLeases(bool isDeleted)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var unitType = new UnitType
        {
            Name = $"Snapshot type {suffix}",
            Code = $"SNAP_{suffix}",
            Usage = UnitTypeUsage.Reservable
        };
        var property = new Property { Name = $"Snapshot property {suffix}" };
        var tenant = new Tenant { Name = $"Snapshot tenant {suffix}", TenantNo = $"SNAP-{suffix}" };
        var unit = new Unit
        {
            Name = $"Snapshot unit {suffix}",
            Property = property,
            UnitType = unitType,
            Area = 73.25m
        };
        var lease = new Lease
        {
            LeaseNo = $"SNAP-{suffix}",
            Unit = unit,
            Tenant = tenant,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            IsDeleted = isDeleted,
            UnitTypeNameSnapshot = string.Empty,
            UnitTypeUsageSnapshot = 0,
            RentalModeSnapshot = 0,
            UnitAreaSnapshot = 0m
        };
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();

        var sql = Assert.Single(new AddSharedAndCapacityUnits().UpOperations.OfType<SqlOperation>()).Sql;
        await _context.Database.ExecuteSqlRawAsync(sql);
        _context.ChangeTracker.Clear();

        var backfilled = await _context.Leases.IgnoreQueryFilters().SingleAsync(s => s.Id == lease.Id);
        Assert.Equal(unitType.Name, backfilled.UnitTypeNameSnapshot);
        Assert.Equal(UnitTypeUsage.Reservable, backfilled.UnitTypeUsageSnapshot);
        Assert.Equal(UnitRentalMode.UnitBased, backfilled.RentalModeSnapshot);
        Assert.Equal(unit.Area, backfilled.UnitAreaSnapshot);
        Assert.Equal(isDeleted, backfilled.IsDeleted);

        var invalidCount = await _context.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS [Value]
            FROM Sozlesmeler s
            JOIN Birimler b ON b.Id = s.BirimId
            WHERE s.IsDeleted = 0
              AND (s.AnlikBirimTuruAdi = '' OR s.AnlikBirimYuzolcumu <> b.Yuzolcumu)
            """).SingleAsync();
        Assert.Equal(0, invalidCount);
    }
}
