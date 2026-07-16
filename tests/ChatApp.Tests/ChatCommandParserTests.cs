using ChatApp.Contracts;
using Xunit;

namespace ChatApp.Tests;

public class ChatCommandParserTests
{
    [Theory]
    [InlineData("/stock=aapl.us", "aapl.us")]
    [InlineData("/stock=AAPL.US", "aapl.us")]
    [InlineData("/stock=msft.us", "msft.us")]
    public void TryParseStockCommand_ValidCommand_ReturnsLowercasedCode(string input, string expected)
    {
        var result = ChatCommandParser.TryParseStockCommand(input, out var code);

        Assert.True(result);
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData("hello everyone")]
    [InlineData("/stock=")]
    [InlineData("/stock=aapl")]           // missing the ".us" exchange suffix
    [InlineData("/stocks=aapl.us")]       // wrong command name
    [InlineData("  ")]
    [InlineData(null)]
    public void TryParseStockCommand_InvalidInput_ReturnsFalse(string? input)
    {
        var result = ChatCommandParser.TryParseStockCommand(input, out var code);

        Assert.False(result);
        Assert.Equal(string.Empty, code);
    }

    [Fact]
    public void TryParseStockCommand_TrimsSurroundingWhitespace()
    {
        var result = ChatCommandParser.TryParseStockCommand("   /stock=aapl.us   ", out var code);

        Assert.True(result);
        Assert.Equal("aapl.us", code);
    }
}
