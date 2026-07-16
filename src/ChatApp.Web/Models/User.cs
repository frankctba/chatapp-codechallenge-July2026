namespace ChatApp.Web.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Well-known id for the synthetic bot "user" so ChatMessages.UserId can stay a real FK.
    public static readonly Guid StockBotId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");
    public const string StockBotUsername = "StockBot";
}
