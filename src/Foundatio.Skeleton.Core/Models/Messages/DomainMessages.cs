namespace Foundatio.Skeleton.Core.Models.Messages;

public record UserCreated(string UserId, string EmailAddress);
public record UserUpdated(string UserId);
public record OrganizationCreated(string OrganizationId, string Name);
