using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.AdminConfiguration;

public static class UpdateCategoryRequestValidator
{
    public static void Validate(string? name)
    {
        if (name is null) return;

        if (name.Trim().Length < 2 || name.Trim().Length > 50)
            throw new DomainException("Category name must be between 2 and 50 characters.");
    }
}
