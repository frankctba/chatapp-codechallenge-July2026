namespace ChatApp.Contracts;

/// <summary>
/// Central place for the RabbitMQ queue names so Web and Bot never drift apart.
/// </summary>
public static class QueueNames
{
    public const string StockRequests = "stock.requests";
    public const string StockResponses = "stock.responses";

    // Poison messages (e.g. malformed JSON that will never deserialize) are routed
    // here instead of being dropped or requeued forever - see RabbitMqTopology.
    public const string DeadLetterExchange = "chatapp.dlx";
    public const string StockRequestsDeadLetter = "stock.requests.dlq";
    public const string StockResponsesDeadLetter = "stock.responses.dlq";
}
