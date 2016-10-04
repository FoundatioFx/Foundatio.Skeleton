using FluentValidation;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Validators {
    public class InviteValidator : AbstractValidator<Invite> {
        public InviteValidator() {
            RuleFor(i => i.EmailAddress).NotEmpty().EmailAddress().WithMessage("Please specify a valid email address.");
            //  todo:  below needs to be validated eventually (field was just added so prior invites will have NULL id)
            //RuleFor(i => i.AddedByUserId).NotEmpty().WithMessage("Please specify the user that created the invite.");
        }
    }
}
