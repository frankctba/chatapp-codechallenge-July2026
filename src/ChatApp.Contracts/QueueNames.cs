namespace ChatApp.Contracts;

/// <summary>
/// Central place for the RabbitMQ queue names so Web and Bot never drift apart.
/// </summary>
public static class QueueNames
{
    public const string StockRequests = "stock.requests";
    public const string StockResponses = "stock.responses";
}
