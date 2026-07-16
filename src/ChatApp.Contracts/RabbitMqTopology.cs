using RabbitMQ.Client;

namespace ChatApp.Contracts;

/// <summary>
/// Declares the stock.requests/stock.responses queues (plus their dead-letter queues)
/// identically everywhere they're touched (ChatApp.Web and ChatApp.Bot both declare
/// them). RabbitMQ rejects a redeclare whose arguments differ from the original, so
/// this is kept in one place - same reasoning as QueueNames itself - to stop Web and
/// Bot from drifting apart on queue arguments.
/// </summary>
public static class RabbitMqTopology
{
    public static void DeclareStockQueues(IModel channel)
    {
        channel.ExchangeDeclare(QueueNames.DeadLetterExchange, ExchangeType.Direct, durable: true);

        DeclareQueueWithDeadLetter(channel, QueueNames.StockRequests, QueueNames.StockRequestsDeadLetter);
        DeclareQueueWithDeadLetter(channel, QueueNames.StockResponses, QueueNames.StockResponsesDeadLetter);
    }

    private static void DeclareQueueWithDeadLetter(IModel channel, string queueName, string deadLetterQueueName)
    {
        channel.QueueDeclare(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);

        // Messages are published to the main queue with a routing key equal to the
        // queue name (default exchange), and RabbitMQ preserves that original routing
        // key when dead-lettering - so binding with the same key routes it here.
        channel.QueueBind(deadLetterQueueName, QueueNames.DeadLetterExchange, routingKey: queueName);

        var arguments = new Dictionary<string, object> { ["x-dead-letter-exchange"] = QueueNames.DeadLetterExchange };
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
    }
}
