using FluentValidation;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Validators {
    public class EmailValidator : AbstractValidator<Email> {
        public EmailValidator() {
            RuleFor(e => e.Address).NotNull().NotEmpty().EmailAddress();
        }
    }
}
