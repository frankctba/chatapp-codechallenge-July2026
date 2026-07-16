namespace ChatApp.Contracts;

/// <summary>
/// Published to <see cref="QueueNames.StockResponses"/> by ChatApp.Bot once it has
/// fetched and parsed the quote (or failed to). Consumed by ChatApp.Web, which turns
/// it into a chat message posted by the bot.
/// </summary>
public sealed record StockQuoteReady(
    Guid RequestId,
    string StockCode,
    bool Success,
    decimal? Price,
    string? ErrorMessage,
    DateTime RespondedAtUtc);
