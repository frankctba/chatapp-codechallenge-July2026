using System.Text;
using System.Text.Json;
using ChatApp.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApp.Bot;

public sealed class StockQuoteWorker(
    IHttpClientFactory httpClientFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<StockQuoteWorker> logger) : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = opts.HostName,
            Port = opts.Port,
            UserName = opts.UserName,
            Password = opts.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection("chatapp-bot");
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(QueueNames.StockRequests, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QueueNames.StockResponses, durable: true, exclusive: false, autoDelete: false);

        // Small prefetch so multiple bot instances share load roughly evenly (competing consumers).
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var request = JsonSerializer.Deserialize<StockRequested>(json);
                if (request is not null)
                {
                    await HandleRequestAsync(request, stoppingToken);
                }
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process stock request, requeuing");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(QueueNames.StockRequests, autoAck: false, consumer);

        stoppingToken.Register(() =>
        {
            _channel?.Dispose();
            _connection?.Dispose();
        });

        return Task.CompletedTask;
    }

    private async Task HandleRequestAsync(StockRequested request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling stock request {StockCode} from {User}", request.StockCode, request.RequestedByUsername);

        StockQuoteReady response;
        try
        {
            var client = httpClientFactory.CreateClient(nameof(YahooFinanceClient));
            var yahoo = new YahooFinanceClient(client);
            var json = await yahoo.GetQuoteJsonAsync(request.StockCode, cancellationToken);
            var price = YahooQuoteParser.ParseClosePrice(json);

            response = price is null
                ? new StockQuoteReady(request.RequestId, request.StockCode, false, null, "unknown stock code", DateTime.UtcNow)
                : new StockQuoteReady(request.RequestId, request.StockCode, true, price, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch quote for {StockCode}", request.StockCode);
            response = new StockQuoteReady(request.RequestId, request.StockCode, false, null, "temporarily unavailable", DateTime.UtcNow);
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(response);
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        _channel.BasicPublish(exchange: string.Empty, routingKey: QueueNames.StockResponses, basicProperties: properties, body: body);
    }
}
