using System;
using FluentValidation.Validators;

namespace Foundatio.Skeleton.Core.Validators {
    public class IsObjectIdValidator : PropertyValidator {
        public IsObjectIdValidator() : base(() => "Value is not a valid object id.") {}

        protected override bool IsValid(PropertyValidatorContext context) {
            var value = context.PropertyValue as string;
            if (String.IsNullOrEmpty(value))
                return false;

            return value.Length == 24;
        }
    }
}