using FluentValidation;

namespace MachineDataApi.Configuration;

public class ApplicationConfiguration
{
    public string MachineStreamEndPointUrl { get; set; }

    public void Validate()
    {
        var validator = new ApplicationConfigurationValidator();
        var result = validator.Validate(this);
        if (!result.IsValid)
            throw new Exception($"Invalid configuration: {result}");
    }
}

public class ApplicationConfigurationValidator : AbstractValidator<ApplicationConfiguration>
{
    public ApplicationConfigurationValidator()
    {
        RuleFor(p => p.MachineStreamEndPointUrl).NotEmpty();
    }
}

