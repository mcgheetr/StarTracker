# StarTracker Design Document

## Overview

StarTracker is a portfolio backend service demonstrating production-ready API design, security best practices, and cloud-native architecture patterns. The service helps users locate celestial objects from their geographic location and optionally record observations with privacy-preserving field-level encryption.

## Architecture

### Layered Architecture

```text
┌─────────────────────────────────────────┐
│   StarTracker.Api (Presentation)        │
│   - Minimal API endpoints               │
│   - Request validation                  │
│   - API key authentication              │
└─────────────────────────────────────────┘
                   │
┌─────────────────────────────────────────┐
│   StarTracker.Core (Domain)             │
│   - Business logic                      │
│   - Domain models (DTOs)                │
│   - Service interfaces                  │
│   - NO infrastructure dependencies      │
└─────────────────────────────────────────┘
                   │
┌─────────────────────────────────────────┐
│   StarTracker.Infrastructure (Data)     │
│   - Repository implementations          │
│   - Encryption services                 │
│   - AWS SDK integrations                │
└─────────────────────────────────────────┘
```

**Key Principles:**

- **Dependency Inversion**: Core defines interfaces; Infrastructure implements them
- **Clean separation**: Core has zero cloud SDK dependencies
- **Testability**: All layers can be tested in isolation
- **Flexibility**: Swap implementations without changing domain logic

### Repository Pattern

The repository pattern abstracts data access behind `IObservationRepository`, enabling:

- **Multiple storage backends** (in-memory, DynamoDB, future SQL support)
- **Easy testing** with in-memory implementation
- **Configuration-driven** selection via `appsettings.json`

```csharp
public interface IObservationRepository
{
    Task<ObservationDto> CreateObservationAsync(ObservationDto observation, CancellationToken ct);
    Task<ObservationDto?> GetObservationAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<ObservationDto>> GetObservationsAsync(string target, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}
```

**Implementations:**

- `InMemoryObservationRepository`: Fast, ephemeral, development-friendly
- `DynamoDbObservationRepository`: Persistent, scalable, production-ready

## Key Design Decisions

### 1. Coordinate Normalization (5 Decimal Places)

**Decision**: Round lat/lon to 5 decimal places before storage.

**Rationale**:

- **Privacy**: Reduces precision from ~1cm to ~1.1 meters, obscuring exact user location
- **Sufficient accuracy**: Star observation doesn't require centimeter-level precision
- **Consistent storage**: Normalized keys prevent duplicate near-identical coordinates
- **Balances**: User privacy vs observational accuracy

**Implementation**: `CoordinateNormalizer.TryParseDecimalDegrees()`

### 2. Field-Level Encryption for Coordinates

**Decision**: Encrypt only RA/Dec coordinates, not entire records.

**Rationale**:

- **Least privilege**: Only sensitive data (location-derived coordinates) is encrypted
- **Performance**: Reduces encryption/decryption overhead
- **Queryability**: Non-encrypted fields (Target, ObservedAt, Observer) remain queryable
- **Privacy compliance**: Protects user-derived location data

**Security Model**:

```text
┌──────────────────────────────────────┐
│ Observation Record                   │
├──────────────────────────────────────┤
│ Id: guid (plaintext)                 │
│ Target: "Polaris" (plaintext)        │
│ ObservedAt: timestamp (plaintext)    │
│ Observer: "username" (plaintext)     │
│ EncryptedLocationPayload: <base64>   │  ← Contains RA/Dec
│ Notes: "clear sky" (plaintext)       │
└──────────────────────────────────────┘
```

### 3. Dual Encryption Providers

**Decision**: Support both Data Protection (dev) and AWS KMS (prod) via abstraction.

**Development (Data Protection)**:

- ✅ No AWS dependencies or credentials needed
- ✅ Fast local testing
- ✅ Built into ASP.NET Core
- ❌ Keys stored locally (not suitable for production)

**Production (AWS KMS with Envelope Encryption)**:

- ✅ Hardware security module (HSM) backed keys
- ✅ Centralized key management and rotation
- ✅ Audit logging via CloudTrail
- ✅ Fine-grained IAM permissions
- ❌ Requires AWS account and configuration

**Configuration Toggle**:

```json
{
  "Encryption": {
    "UseAwsSdk": false  // true for KMS, false for Data Protection
  }
}
```

**Why Envelope Encryption for KMS?**

- Reduces KMS API calls (cost optimization)
- Faster encryption/decryption (local data key usage)
- Industry standard pattern for encrypting large payloads
- Demonstrated in `FakeAwsEnvelopeEncryptor` (test) and `AwsSdkEnvelopeEncryptor` (scaffold)

### 4. DynamoDB as Primary Datastore

**Decision**: Use DynamoDB instead of SQL database.

**Rationale**:

- **Serverless**: No server provisioning or patching
- **Auto-scaling**: Handles variable load automatically
- **Global availability**: Multi-region replication support
- **Cost-effective**: Pay per request at low volumes
- **Portfolio value**: Demonstrates NoSQL proficiency

**Schema Design**:

- **Primary Key**: `Id` (HASH) - direct lookups by observation ID
- **GSI**: `TargetIndex` on `Target` (HASH) + `ObservedAt` (RANGE) - efficient queries by star name and time range
- **Encryption**: Server-side encryption with KMS key (configured in Terraform)

**Trade-offs**:

- ❌ No JOIN support (acceptable for this domain)
- ❌ Limited ad-hoc query flexibility (GSI handles primary access patterns)
- ✅ Single-digit millisecond latency
- ✅ Proven at massive scale

### 5. Astronomy Calculations (Simplified)

**Decision**: Use simplified celestial coordinate mapping, not full ephemeris calculations.

**Current Implementation**:

```csharp
// Simplified: Polaris is at ~90° declination, offset by observer latitude
var (ra, dec) = AstronomyMapper.FromLatLon(lat, lon);
```

**Rationale**:

- **Portfolio focus**: Demonstrates API design and security, not astronomy expertise
- **Proof of concept**: Real calculations would use libraries like NOVAS or web APIs
- **Extensibility**: Interface `IGuidanceService` allows swapping implementations

**Future Enhancement**:

- Integrate proper ephemeris library (SOFA, NOVAS)
- Account for Earth's precession, nutation, atmospheric refraction
- Support dynamic star catalogs (not just hardcoded logic)

### 6. API Key Authentication

**Decision**: Simple header-based API key (`X-API-Key`), not OAuth/JWT.

**Rationale**:

- **Simplicity**: Portfolio service, not multi-tenant SaaS
- **Sufficient**: Demonstrates auth middleware pattern
- **Upgradeable**: Can swap to OAuth 2.0 / JWT without API changes

**Security Notes**:

- Keys should be rotated regularly
- Use HTTPS in production (enforced by AWS ALB/CloudFront)
- Consider rate limiting per key (future enhancement)

### 7. Minimal API vs Controllers

**Decision**: Use ASP.NET Core Minimal APIs instead of MVC controllers.

**Rationale**:

- **Modern .NET pattern**: Recommended for new projects (.NET 6+)
- **Reduced boilerplate**: Less ceremony than controller classes
- **Performance**: Slightly faster routing
- **Simplicity**: Easier to understand for portfolio reviewers

**Endpoints organized** in `StarsEndpoints.cs`:

```csharp
app.MapGet("/api/v1/stars/{target}/position", GetStarPosition);
app.MapPost("/api/v1/stars/{target}/observations", CreateObservation);
app.MapGet("/api/v1/stars/{target}/observations", GetObservations);
app.MapGet("/api/v1/observations/{id}", GetObservationById);
```

## Testing Strategy

### Unit Tests

- **Coordinate normalization**: Edge cases (poles, meridian, precision)
- **Encryption services**: Round-trip, invalid input handling
- **Repository logic**: CRUD operations, filtering, encryption verification

### Integration Tests

- **API endpoints**: Using `WebApplicationFactory<Program>`
- **Request validation**: Bad inputs, missing headers
- **End-to-end flows**: Position lookup → observation creation → retrieval

### Local Development

- **DynamoDB Local**: Docker-based local testing without AWS account
- **In-memory repository**: Fast test execution
- **Fake encryption**: Deterministic behavior for test stability

## Infrastructure Decisions

### Docker Compose for Local Development

- **Full local stack**: API + UI + DynamoDB Local via `docker compose up --build`
- **UI/API wiring**: UI container proxies `/api/*` to API container, including Swagger path support
- **API key for compose**: `ApiKey=local-dev-key` for deterministic local testing
- **Persistent volume**: `./dynamodb-data` for DynamoDB Local retention between restarts
- **Init script**: `scripts/init-dynamodb-local.ps1` creates table with proper schema

### Terraform for AWS Deployment

- **Infrastructure as Code**: Repeatable, version-controlled deployments
- **Current cloud deployment model**:
  - API: Lambda container image behind API Gateway
  - UI: S3 static website hosting (no CloudFront requirement)
  - Data: DynamoDB observations table
  - Container registry: ECR
- **Workflow alignment**:
  - `deploy.yml` deploys Lambda API + S3 UI
  - `destroy.yml` performs teardown and post-destroy verification

### Kubernetes/Helm Scope (Current)

- **Helm chart exists** for API + UI deployment and local validation workflows
- **Primary use today**: local Docker Desktop Kubernetes testing and demo
- **Not in cloud deploy path**: EKS deployment is intentionally not part of default GitHub deploy to control baseline cost
- **Local validation gains**:
  - Verified chart install/upgrade flow
  - Verified API/UI service wiring in-cluster
  - Verified Swagger access through UI proxy path (`/api/swagger/index.html`)

### Teardown Verification

- Added `scripts/destroy-verify.ps1` to audit common cost-bearing leftovers:
  - Load balancers/target groups
  - EC2 instances/EBS/NAT/EIPs
  - CloudFormation stacks
  - ECR repositories
- `destroy.yml` runs verification after non-preview destroys to reduce surprise charges

### Launch Profiles

- **Development**: In-memory repository, Data Protection encryption
- **DynamoDBLocal**: Local DynamoDB, Data Protection encryption
- **Production** (future): AWS DynamoDB, KMS encryption, CloudWatch logging

## Security Best Practices

1. **Defense in Depth**:
   - API key authentication
   - Field-level encryption
   - DynamoDB server-side encryption
   - VPC isolation (when deployed)

2. **Least Privilege**:
   - Encrypt only sensitive fields
   - IAM roles with minimal permissions
   - No hardcoded secrets (use environment variables / Secrets Manager)

3. **Logging & Monitoring**:
   - Never log encrypted payloads
   - No PII in application logs
   - CloudWatch metrics for API usage
   - KMS key usage via CloudTrail

4. **Data Retention**:
   - DynamoDB TTL for observation expiration (future enhancement)
   - Backup strategy via point-in-time recovery

## Future Enhancements

### Short Term

- [ ] Real astronomical calculations using ephemeris library
- [ ] Rate limiting per API key
- [ ] Query observations by observer name

### Medium Term

- [ ] GraphQL API for flexible queries
- [ ] WebSocket support for real-time updates
- [ ] Image upload for observation photos (S3 + Rekognition)
- [ ] Weather API integration for observing conditions

### Long Term

- [ ] Multi-tenant architecture with organization isolation
- [ ] OAuth 2.0 / OpenID Connect
- [ ] Machine learning for star identification from photos
- [ ] Mobile app with AR star finder overlay

## Performance Considerations

### Expected Load

- **Target**: <100 req/s (single-region, hobby project scale)
- **Response time**: <200ms p99 for position lookups
- **Database**: On-demand billing for variable traffic

### Optimization Opportunities

- **Caching**: CloudFront for static responses, ElastiCache for hot data
- **Compression**: gzip/brotli for API responses
- **CDN**: Edge locations for global low-latency access
- **Connection pooling**: DynamoDB client reuse

## Lessons Demonstrated

This project showcases:

- ✅ **Clean Architecture**: Separation of concerns, dependency inversion
- ✅ **Security-first design**: Encryption, authentication, privacy
- ✅ **Cloud-native patterns**: Serverless, managed services, IaC
- ✅ **Testing discipline**: Unit + integration tests, local development
- ✅ **Production readiness**: Logging, configuration, error handling
- ✅ **Modern .NET**: Minimal APIs, nullable reference types, async/await
- ✅ **DevOps mindset**: Docker, Terraform, CI/CD readiness
- ✅ **API docs**: OpenAPI/Swagger for discoverability
- ✅ **Cost-aware operations**: deploy-path scoping, post-destroy verification, local-first Kubernetes validation

---

**Version**: 1.1  
**Last Updated**: February 20, 2026  
**Author**: Portfolio Project for Backend Engineering Role
