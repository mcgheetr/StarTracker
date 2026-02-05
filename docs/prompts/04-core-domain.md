Implement the Core layer for StarTracker.

Requirements:
- No AWS references.
- Interfaces:
  - IStarPositionService
  - IObservationRepository
  - IApiKeyValidator (optional, if placed in Core)
  - ITimeProvider
  - IIdGenerator
- Domain models:
  - Observation
  - StarTarget enum or value object
- Star position calculation: implement Polaris only to start (simple approximation is fine, but document assumptions).

Output:
1) File list in src/StarTracker.Core
2) C# code for interfaces/models
3) A small unit test for core logic
4) Notes on where future stars would plug in
