using FluentValidation;
using LYBT.Module.MedicalCases.Application.Commands;

namespace LYBT.Module.MedicalCases.Application.Validators;

public class CreateMedicalCaseValidator : AbstractValidator<CreateMedicalCaseCommand>
{
    public CreateMedicalCaseValidator()
    {
        RuleFor(x => x.Input.PatientId).NotEmpty().WithMessage("患者ID不能为空");
        RuleFor(x => x.Input.UserId).NotEmpty().WithMessage("医生ID不能为空");
    }
}


