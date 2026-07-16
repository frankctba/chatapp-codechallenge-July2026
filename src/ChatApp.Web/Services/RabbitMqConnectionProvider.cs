using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ChatApp.Web.Services;

/// <summary>
/// Owns a single long-lived RabbitMQ connection for the whole app. Channels are cheap
/// and are created per use on top of this shared connection.
/// </summary>
public sealed class RabbitMqConnectionProvider : IDisposable
{
    private readonly Lazy<IConnection> _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options)
    {
        var opts = options.Value;
        _connection = new Lazy<IConnection>(() =>
        {
            var factory = new ConnectionFactory
            {
                HostName = opts.HostName,
                Port = opts.Port,
                UserName = opts.UserName,
                Password = opts.Password,
                DispatchConsumersAsync = true
            };
            return factory.CreateConnection("chatapp-web");
        });
    }

    public IConnection Connection => _connection.Value;

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
