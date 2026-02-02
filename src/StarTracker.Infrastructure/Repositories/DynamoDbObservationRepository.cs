using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using StarTracker.Core.DTOs;

namespace StarTracker.Infrastructure.Repositories;

/// <summary>
/// DynamoDB-backed observation repository with encrypted coordinate storage.
/// </summary>
public class DynamoDbObservationRepository : IObservationRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly Table _table;
    private readonly IEncryptionService _encryption;
    private const string TableNameDefault = "observations";

    public DynamoDbObservationRepository(
        IAmazonDynamoDB dynamoDb,
        IEncryptionService encryption,
        string? tableName = null)
    {
        _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        var actualTableName = tableName ?? TableNameDefault;
        _table = Table.LoadTable(_dynamoDb, actualTableName);
    }

    public async Task<ObservationDto> CreateObservationAsync(ObservationDto observation, CancellationToken cancellationToken = default)
    {
        var id = observation.Id == Guid.Empty ? Guid.NewGuid() : observation.Id;

        // Serialize RA/Dec into a small JSON payload and encrypt it
        var locationPayload = System.Text.Json.JsonSerializer.Serialize(
            new { observation.RightAscensionDegrees, observation.DeclinationDegrees });
        var encrypted = _encryption.Protect(locationPayload);

        // Build DynamoDB item
        var item = new Document
        {
            ["Id"] = id.ToString(),
            ["Target"] = observation.Target,
            ["ObservedAt"] = observation.ObservedAt.UtcTicks,
            ["Observer"] = observation.Observer,
            ["EncryptedLocationPayload"] = encrypted
        };

        if (!string.IsNullOrWhiteSpace(observation.Notes))
        {
            item["Notes"] = observation.Notes;
        }

        await _table.PutItemAsync(item, cancellationToken);

        return observation with { Id = id };
    }

    public async Task<ObservationDto?> GetObservationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await _table.GetItemAsync(id.ToString(), cancellationToken);
            if (doc == null)
                return null;

            return DocumentToDto(doc);
        }
        catch (Exception ex) when (ex.GetType().Name == "ResourceNotFoundException")
        {
            // DynamoDB table not found or resource error
            return null;
        }
    }

    public async Task<IEnumerable<ObservationDto>> GetObservationsAsync(
        string target,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        // Query by Target (GSI required for efficient querying by target)
        // For now, use Scan with filter (not efficient for large data, but works)
        var scanFilter = new ScanFilter();
        scanFilter.AddCondition("Target", ScanOperator.Equal, target);

        if (from.HasValue)
            scanFilter.AddCondition("ObservedAt", ScanOperator.GreaterThanOrEqual, from.Value.UtcTicks);

        if (to.HasValue)
            scanFilter.AddCondition("ObservedAt", ScanOperator.LessThanOrEqual, to.Value.UtcTicks);

        var search = _table.Scan(scanFilter);
        var documents = new List<Document>();

        do
        {
            documents.AddRange(await search.GetNextSetAsync(cancellationToken));
        } while (!search.IsDone);

        return documents
            .OrderBy(d => d["ObservedAt"].AsLong())
            .Select(DocumentToDto)
            .ToList();
    }

    private ObservationDto DocumentToDto(Document doc)
    {
        var id = Guid.Parse(doc["Id"].AsString());
        var target = doc["Target"].AsString();
        var observedAt = new DateTimeOffset(doc["ObservedAt"].AsLong(), TimeSpan.Zero);
        var observer = doc["Observer"].AsString();
        var notes = doc.ContainsKey("Notes") ? doc["Notes"].AsString() : null;
        var encryptedPayload = doc["EncryptedLocationPayload"].AsString();

        // Decrypt and deserialize location
        var locJson = _encryption.Unprotect(encryptedPayload);
        var loc = System.Text.Json.JsonSerializer.Deserialize<LocationPayload>(locJson)
            ?? new LocationPayload { RightAscensionDegrees = 0, DeclinationDegrees = 0 };

        return new ObservationDto(
            id,
            target,
            observedAt,
            loc.RightAscensionDegrees,
            loc.DeclinationDegrees,
            observer,
            notes);
    }

    private class LocationPayload
    {
        public double RightAscensionDegrees { get; set; }
        public double DeclinationDegrees { get; set; }
    }
}
