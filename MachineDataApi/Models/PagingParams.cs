using FluentValidation;

namespace MachineDataApi.Models
{
    public class PagingParams
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 10;
    }

    public class PagingParamsValidator : AbstractValidator<PagingParams>
    {
        public PagingParamsValidator()
        {
            RuleFor(p => p.Take).ExclusiveBetween(1, 1000);
            RuleFor(p => p.Skip).GreaterThanOrEqualTo(0);
        }
    }
}
