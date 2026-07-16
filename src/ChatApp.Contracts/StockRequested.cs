namespace ChatApp.Contracts;

/// <summary>
/// Published to <see cref="QueueNames.StockRequests"/> by ChatApp.Web whenever a user
/// posts a "/stock=code" command. Consumed by one of the (possibly many) ChatApp.Bot instances.
/// </summary>
public sealed record StockRequested(
    Guid RequestId,
    string StockCode,
    string RequestedByUsername,
    DateTime RequestedAtUtc);
