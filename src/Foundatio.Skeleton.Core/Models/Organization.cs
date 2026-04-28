namespace Foundatio.Skeleton.Core.Models;

public class Organization
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
