namespace Foundatio.Skeleton.Core.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public bool IsEmailAddressVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<string> Roles { get; set; } = [];
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
