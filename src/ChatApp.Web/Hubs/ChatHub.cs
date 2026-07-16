using System.Collections.Concurrent;
using ChatApp.Contracts;
using ChatApp.Web.Data;
using ChatApp.Web.Models;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Hubs;

[Authorize]
public sealed class ChatHub(ChatDbContext db, IStockRequestPublisher publisher, ILogger<ChatHub> logger) : Hub
{
    private const int HistorySize = 50;

    // Simple in-memory per-connection flood guard: at most MaxMessagesPerWindow
    // SendMessage calls per Window. Deliberately basic - fine for a single Web
    // instance; if this app is ever scaled to multiple instances (see the Redis
    // SignalR backplane note in the architecture discussion), this would need to
    // move to a shared store (e.g. Redis) to stay effective across instances.
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> RateLimitState = new();
    private const int MaxMessagesPerWindow = 10;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    private bool IsRateLimited()
    {
        var connectionId = Context.ConnectionId;
        var now = DateTime.UtcNow;

        var entry = RateLimitState.AddOrUpdate(
            connectionId,
            _ => (1, now),
            (_, existing) => now - existing.WindowStart > Window
                ? (1, now)
                : (existing.Count + 1, existing.WindowStart));

        return entry.Count > MaxMessagesPerWindow;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        RateLimitState.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        var history = await db.Messages
            .OrderByDescending(m => m.TimestampUtc)
            .Take(HistorySize)
            .OrderBy(m => m.TimestampUtc)
            .Select(m => ChatMessageDto.FromEntity(m))
            .ToListAsync();

        await Clients.Caller.SendAsync("MessageHistory", history);
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string content)
    {
        var username = Context.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (IsRateLimited())
        {
            logger.LogWarning("Rate limit exceeded for {User} on connection {ConnectionId}", username, Context.ConnectionId);
            await Clients.Caller.SendAsync("RateLimited", "You're sending messages too quickly - please slow down.");
            return;
        }

        if (ChatCommandParser.TryParseStockCommand(content, out var stockCode))
        {
            // Stock commands are never persisted as chat posts - only forwarded to the bot.
            var request = new StockRequested(Guid.NewGuid(), stockCode, username, DateTime.UtcNow);
            logger.LogInformation("Publishing stock request {StockCode} for {User}", stockCode, username);
            await publisher.PublishAsync(request);
            return;
        }

        var user = await db.Users.SingleAsync(u => u.Username == username);
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Username = user.Username,
            Content = content,
            TimestampUtc = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        await Clients.All.SendAsync("ReceiveMessage", ChatMessageDto.FromEntity(message));
    }
}
