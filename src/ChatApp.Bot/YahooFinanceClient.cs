namespace ChatApp.Bot;

public sealed class YahooFinanceClient(HttpClient httpClient)
{
    public async Task<string> GetQuoteJsonAsync(string stockCode, CancellationToken cancellationToken)
    {
        var symbol = ToYahooSymbol(stockCode);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=5d";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");

        var response = await httpClient.SendAsync(request, cancellationToken);

        // Yahoo returns 404 with a JSON body ("result": null) for unknown tickers -
        // that's a valid response the parser understands, not a transport failure.
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string ToYahooSymbol(string stockCode)
    {
        // Stock codes follow the "ticker.exchange" notation from the /stock= command
        // (e.g. "aapl.us"); Yahoo Finance addresses US tickers by symbol alone.
        var dotIndex = stockCode.IndexOf('.');
        var ticker = dotIndex < 0 ? stockCode : stockCode[..dotIndex];
        return ticker.ToUpperInvariant();
    }
}
