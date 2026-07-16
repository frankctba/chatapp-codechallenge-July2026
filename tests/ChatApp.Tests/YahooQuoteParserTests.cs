using ChatApp.Bot;
using Xunit;

namespace ChatApp.Tests;

public class YahooQuoteParserTests
{
    [Fact]
    public void ParseClosePrice_ValidJson_ReturnsClosePrice()
    {
        const string json = """
        {"chart":{"result":[{"meta":{"symbol":"AAPL","regularMarketPrice":93.42}}],"error":null}}
        """;

        var price = YahooQuoteParser.ParseClosePrice(json);

        Assert.Equal(93.42m, price);
    }

    [Fact]
    public void ParseClosePrice_UnknownStockCode_ReturnsNull()
    {
        const string json = """
        {"chart":{"result":null,"error":{"code":"Not Found","description":"No data found, symbol may be delisted"}}}
        """;

        var price = YahooQuoteParser.ParseClosePrice(json);

        Assert.Null(price);
    }

    [Fact]
    public void ParseClosePrice_MissingChartObject_ThrowsFormatException()
    {
        const string json = "{}";

        Assert.Throws<FormatException>(() => YahooQuoteParser.ParseClosePrice(json));
    }

    [Fact]
    public void ParseClosePrice_MissingRegularMarketPrice_ThrowsFormatException()
    {
        const string json = """
        {"chart":{"result":[{"meta":{"symbol":"AAPL"}}],"error":null}}
        """;

        Assert.Throws<FormatException>(() => YahooQuoteParser.ParseClosePrice(json));
    }
}
