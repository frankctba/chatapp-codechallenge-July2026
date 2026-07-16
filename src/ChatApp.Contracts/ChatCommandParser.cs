using System.Text.RegularExpressions;

namespace ChatApp.Contracts;

/// <summary>
/// Detects the "/stock=CODE" command in a chat message. Kept dependency-free so it's
/// trivial to unit test without spinning up SignalR, RabbitMQ, or a database.
/// </summary>
public static partial class ChatCommandParser
{
    // Stock codes follow a "ticker.exchange" notation, e.g. "aapl.us" - letters, digits
    // and a single dot separator. ChatApp.Bot takes the part before the dot as the ticker.
    [GeneratedRegex(@"^/stock=([a-zA-Z0-9]+\.[a-zA-Z0-9]+)$")]
    private static partial Regex StockCommandRegex();

    public static bool TryParseStockCommand(string? message, out string stockCode)
    {
        stockCode = string.Empty;

        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var match = StockCommandRegex().Match(message.Trim());
        if (!match.Success)
        {
            return false;
        }

        stockCode = match.Groups[1].Value.ToLowerInvariant();
        return true;
    }
}
