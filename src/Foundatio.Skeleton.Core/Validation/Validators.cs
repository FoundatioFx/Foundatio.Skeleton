using FluentValidation;
using Foundatio.Skeleton.Core.Models;

namespace Foundatio.Skeleton.Core.Validation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.FullName).NotEmpty().MaximumLength(256);
        RuleFor(u => u.EmailAddress).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public class OrganizationValidator : AbstractValidator<Organization>
{
    public OrganizationValidator()
    {
        RuleFor(o => o.Name).NotEmpty().MaximumLength(256);
    }
}
