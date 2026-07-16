using ChatApp.Contracts;

namespace ChatApp.Web.Services;

public interface IStockRequestPublisher
{
    Task PublishAsync(StockRequested request, CancellationToken cancellationToken = default);
}
