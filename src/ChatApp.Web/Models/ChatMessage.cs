namespace ChatApp.Web.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Denormalized on write so history reads never need a join, and so a display
    // name still makes sense even if a username were ever changed later.
    public required string Username { get; set; }
    public required string Content { get; set; }
    public DateTime TimestampUtc { get; set; }
}
