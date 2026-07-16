namespace ChatApp.Web.Models;

public sealed record ChatMessageDto(Guid Id, string Username, string Content, DateTime TimestampUtc, bool IsBot)
{
    public static ChatMessageDto FromEntity(ChatMessage entity) =>
        new(entity.Id, entity.Username, entity.Content, entity.TimestampUtc, entity.UserId == User.StockBotId);
}
