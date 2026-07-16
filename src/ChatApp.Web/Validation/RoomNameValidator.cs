using System.Text.RegularExpressions;

namespace ChatApp.Web.Validation;

/// <summary>
/// Single source of truth for room name format rules. Room names double as SignalR
/// group names and a persisted column, so the character set is kept deliberately narrow.
/// </summary>
public static partial class RoomNameValidator
{
    public const string DefaultRoomName = "general";

    public const string RoomNameRulesDescription =
        "1-30 characters: letters, numbers, dots, underscores, or hyphens.";

    [GeneratedRegex(@"^[a-zA-Z0-9._-]{1,30}$")]
    private static partial Regex RoomNameRegex();

    public static bool IsValidRoomName(string? roomName) =>
        !string.IsNullOrEmpty(roomName) && RoomNameRegex().IsMatch(roomName);
}
