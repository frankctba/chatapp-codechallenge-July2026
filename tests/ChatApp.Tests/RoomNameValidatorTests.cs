using ChatApp.Web.Validation;
using Xunit;

namespace ChatApp.Tests;

public class RoomNameValidatorTests
{
    [Theory]
    [InlineData("general")]
    [InlineData("Off-Topic")]
    [InlineData("room_42")]
    [InlineData("a")]
    public void IsValidRoomName_AcceptsWellFormedNames(string roomName)
    {
        Assert.True(RoomNameValidator.IsValidRoomName(roomName));
    }

    [Theory]
    [InlineData("has spaces")]
    [InlineData("has@symbol")]
    [InlineData("this-room-name-is-way-too-long-to-be-allowed")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidRoomName_RejectsMalformedNames(string? roomName)
    {
        Assert.False(RoomNameValidator.IsValidRoomName(roomName));
    }
}
