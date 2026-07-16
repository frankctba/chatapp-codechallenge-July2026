using System.Text.RegularExpressions;

namespace ChatApp.Web.Validation;

/// <summary>
/// Single source of truth for username/password format rules. The frontend mirrors
/// these same rules for instant feedback, but this is what's actually enforced -
/// never trust client-side validation alone.
/// </summary>
public static partial class CredentialValidator
{
    public const string UsernameRulesDescription =
        "3-20 characters. Start with a letter; letters, numbers, dots, underscores, or hyphens after that.";

    public const string PasswordRulesDescription =
        "At least 8 characters, including at least one letter and one number.";

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9._-]{2,19}$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$")]
    private static partial Regex PasswordRegex();

    public static bool IsValidUsername(string? username) =>
        !string.IsNullOrEmpty(username) && UsernameRegex().IsMatch(username);

    public static bool IsValidPassword(string? password) =>
        !string.IsNullOrEmpty(password) && PasswordRegex().IsMatch(password);
}
