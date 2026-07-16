using System.Text;
using System.Text.Json;
using ChatApp.Contracts;
using RabbitMQ.Client;

namespace ChatApp.Web.Services;

public sealed class RabbitMqStockRequestPublisher(RabbitMqConnectionProvider connectionProvider) : IStockRequestPublisher
{
    public Task PublishAsync(StockRequested request, CancellationToken cancellationToken = default)
    {
        using var channel = connectionProvider.Connection.CreateModel();
        channel.QueueDeclare(QueueNames.StockRequests, durable: true, exclusive: false, autoDelete: false);

        var body = JsonSerializer.SerializeToUtf8Bytes(request);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(exchange: string.Empty, routingKey: QueueNames.StockRequests, basicProperties: properties, body: body);

        return Task.CompletedTask;
    }
}
