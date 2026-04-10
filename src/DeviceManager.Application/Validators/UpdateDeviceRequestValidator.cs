using DeviceManager.Application.DTOs;
using FluentValidation;

namespace DeviceManager.Application.Validators;

public sealed class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
{
    public UpdateDeviceRequestValidator()
    {
        RuleFor(x => x.Tag)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Manufacturer)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.OperatingSystem)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.OSVersion)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Processor)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RamAmount)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);

        RuleFor(x => x.AssignedUserId)
            .NotEqual(Guid.Empty)
            .When(x => x.AssignedUserId.HasValue);
    }
}