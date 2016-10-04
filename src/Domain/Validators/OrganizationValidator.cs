using System;
using FluentValidation;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Validators {
    public class OrganizationValidator : AbstractValidator<Organization> {
        public OrganizationValidator() {
            RuleFor(o => o.Name).NotEmpty().WithMessage("Please specify a valid name.");
            RuleFor(o => o.Invites).SetCollectionValidator(new InviteValidator());
        }
    }
}