using System.Text.Json;

namespace ChatApp.Bot;

public static class YahooQuoteParser
{
    /// <summary>
    /// Parses the JSON Yahoo Finance's chart endpoint returns. Returns null if the
    /// stock code was unknown (Yahoo reports this as a null "result" array).
    /// </summary>
    public static decimal? ParseClosePrice(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("chart", out var chart))
        {
            throw new FormatException("Unexpected Yahoo Finance response: no 'chart' object found.");
        }

        if (!chart.TryGetProperty("result", out var result) ||
            result.ValueKind != JsonValueKind.Array ||
            result.GetArrayLength() == 0)
        {
            return null;
        }

        if (!result[0].TryGetProperty("meta", out var meta) ||
            !meta.TryGetProperty("regularMarketPrice", out var price))
        {
            throw new FormatException("Unexpected Yahoo Finance response: no 'regularMarketPrice' field found.");
        }

        return price.GetDecimal();
    }
}
