using FluentValidation;

namespace Birds.Application.Commands.UpdateBirdDescription
{
    public class UpdateBirdDescriptionCommandValidator : AbstractValidator<UpdateBirdDescriptionCommand>
    {
        public UpdateBirdDescriptionCommandValidator()
        {
            RuleFor(x => x.Description)
                .MaximumLength(100).WithMessage("Description must not exceed 100 characters");
        }
    }
}