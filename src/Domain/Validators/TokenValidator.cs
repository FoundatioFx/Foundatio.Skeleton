using System;
using Foundatio.Skeleton.Core.Extensions;
using FluentValidation;

namespace Foundatio.Skeleton.Domain.Validators {
    public class TokenValidator : AbstractValidator<Models.Token> {
        public TokenValidator() {
            RuleFor(t => t.Id).NotEmpty().WithMessage("Please specify a valid id.");
            RuleFor(t => t.OrganizationId).IsObjectId().When(p => !String.IsNullOrEmpty(p.OrganizationId)).WithMessage("Please specify a valid organization id.");
            RuleFor(t => t.CreatedUtc).NotEmpty().WithMessage("Please specify a valid created date.");
            RuleFor(t => t.UpdatedUtc).NotEmpty().WithMessage("Please specify a valid modified date.");
        }
    }
}