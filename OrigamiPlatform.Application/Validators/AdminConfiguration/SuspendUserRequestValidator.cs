using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.AdminConfiguration;

public static class SuspendUserRequestValidator
{
    public static void Validate(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Suspension reason is required.");

        if (reason.Trim().Length < 10)
            throw new DomainException("Suspension reason must be at least 10 characters.");
    }
}
