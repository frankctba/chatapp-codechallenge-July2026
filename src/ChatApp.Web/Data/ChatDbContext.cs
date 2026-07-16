using ChatApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            // Last-50-by-timestamp-per-room is the app's core read query - index it.
            entity.HasIndex(m => new { m.RoomName, m.TimestampUtc });
        });

        // Seed the synthetic bot user so ChatMessage.UserId can stay a real, enforced FK.
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = User.StockBotId,
            Username = User.StockBotUsername,
            PasswordHash = "not-a-real-login", // bot never logs in through the API
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
