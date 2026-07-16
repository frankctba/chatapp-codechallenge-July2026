using System.Text;
using System.Text.Json;
using ChatApp.Contracts;
using ChatApp.Web.Data;
using ChatApp.Web.Hubs;
using ChatApp.Web.Models;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApp.Web.Services;

/// <summary>
/// Consumes ChatApp.Bot's replies from stock.responses, persists them as a chat message
/// owned by the StockBot user, and broadcasts them to every connected client.
/// </summary>
public sealed class StockResponseListener(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    IHubContext<ChatHub> hubContext,
    ILogger<StockResponseListener> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = connectionProvider.Connection.CreateModel();
        channel.QueueDeclare(QueueNames.StockResponses, durable: true, exclusive: false, autoDelete: false);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var response = JsonSerializer.Deserialize<StockQuoteReady>(json);
                if (response is not null)
                {
                    await HandleResponseAsync(response, stoppingToken);
                }
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process stock response, requeuing");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(QueueNames.StockResponses, autoAck: false, consumer);

        stoppingToken.Register(() => channel.Dispose());
        return Task.CompletedTask;
    }

    private async Task HandleResponseAsync(StockQuoteReady response, CancellationToken cancellationToken)
    {
        var text = response.Success
            ? $"{response.StockCode.ToUpperInvariant()} quote is ${response.Price:0.00} per share"
            : $"Sorry, I couldn't get a quote for {response.StockCode.ToUpperInvariant()} ({response.ErrorMessage})";

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = User.StockBotId,
            Username = User.StockBotUsername,
            RoomName = response.RoomName,
            Content = text,
            TimestampUtc = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group(response.RoomName).SendAsync("ReceiveMessage", ChatMessageDto.FromEntity(message), cancellationToken);
    }
}
