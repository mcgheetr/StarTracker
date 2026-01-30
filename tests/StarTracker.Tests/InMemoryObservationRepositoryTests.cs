namespace StarTracker.Tests;

public class InMemoryObservationRepositoryTests
{
    [Fact]
    public async Task CreateAndGetObservation_Works()
    {
        var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("StarTracker.Tests");
        var enc = new StarTracker.Infrastructure.DataProtectionEncryptionService(provider);
        var repo = new InMemoryObservationRepository(enc);

        var dto = new ObservationDto(Guid.Empty, "Vega", DateTimeOffset.UtcNow, 80.0, 38.78, "test-observer", "note");

        var saved = await repo.CreateObservationAsync(dto);
        Assert.NotEqual(Guid.Empty, saved.Id);

        var fetched = await repo.GetObservationAsync(saved.Id);
        Assert.NotNull(fetched);
        Assert.Equal(saved.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetObservations_FilterByTargetAndTimeRange()
    {
        var repo = new InMemoryObservationRepository();
        var now = DateTimeOffset.UtcNow;
        var a = new ObservationDto(Guid.Empty, "Sirius", now.AddMinutes(-10), 101, -16, "o1", null);
        var b = new ObservationDto(Guid.Empty, "Sirius", now.AddMinutes(-5), 102, -16, "o2", null);
        var c = new ObservationDto(Guid.Empty, "Betelgeuse", now, 88, 7, "o3", null);

        await repo.CreateObservationAsync(a);
        await repo.CreateObservationAsync(b);
        await repo.CreateObservationAsync(c);

        var list = (await repo.GetObservationsAsync("Sirius", now.AddMinutes(-7), now)).ToList();
        Assert.Single(list);
        Assert.Equal(102, list[0].RightAscensionDegrees);
    }

    [Fact]
    public async Task StoredPayloads_AreEncrypted_WhenEncryptionServiceProvided()
    {
        var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("StarTracker.Tests");
        var enc = new StarTracker.Infrastructure.DataProtectionEncryptionService(provider);
        var repo = new InMemoryObservationRepository(enc);

        var now = DateTimeOffset.UtcNow;
        var dto = new ObservationDto(Guid.Empty, "TestStar", now, 12.34567, -45.6789, "tester", null);

        var saved = await repo.CreateObservationAsync(dto);

        var raw = repo.GetRawEncryptedPayloads().ToList();
        Assert.Single(raw);
        // Ensure the raw encrypted payload does not contain plaintext RA/Dec values
        Assert.DoesNotContain("12.34567", raw[0]);
        Assert.DoesNotContain("-45.6789", raw[0]);
    }

    [Fact]
    public async Task PolarisObservation_IsStoredAndRetrieved()
    {
        var repo = new InMemoryObservationRepository();
        var observedAt = DateTimeOffset.UtcNow;
        var polaris = new ObservationDto(Guid.Empty, "Polaris", observedAt, 37.95, 89.26, "north-observer", "near north pole");

        var saved = await repo.CreateObservationAsync(polaris);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal("Polaris", saved.Target);

        var fetched = await repo.GetObservationAsync(saved.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Polaris", fetched!.Target);
        Assert.Equal(89.26, fetched.DeclinationDegrees);

        var list = (await repo.GetObservationsAsync("Polaris", null, null)).ToList();
        Assert.Single(list);
        Assert.Equal(saved.Id, list[0].Id);
    }
}