# Contract Test Skeleton: BulkStudentsUpsert

```csharp
public class BulkStudentsUpsertContractTests
{
    [Fact]
    public async Task BulkStudentsUpsert_ShouldAcceptValidRows_AndReturn202()
    {
        // Arrange: craft bulk payload mixing existing studentIds and new creates
        // Act: POST /v1/bulk/students:upsert to planned API surface
        // Assert: verify 202 Accepted and Location header present per contract
        // Current status: EXPECTED TO FAIL until endpoint implemented
        Assert.Fail("Contract stub pending implementation");
    }
}
```
