using System.Net;
using ChatApp.Bot;
using Polly;
using Polly.Extensions.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHttpClient(nameof(YahooFinanceClient))
    .AddPolicyHandler(GetYahooRetryPolicy());
builder.Services.AddHostedService<StockQuoteWorker>();

var host = builder.Build();
host.Run();

// Retries transient failures (network errors, timeouts, 5xx) and 429 rate-limit
// responses with exponential backoff + jitter. 404 (unknown ticker) is a valid
// response the parser handles, so it's deliberately not covered here.
static IAsyncPolicy<HttpResponseMessage> GetYahooRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt =>
                TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)));
