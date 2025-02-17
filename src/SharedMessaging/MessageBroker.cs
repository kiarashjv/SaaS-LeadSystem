using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedModels;

namespace SharedMessaging;

public interface IMessageBroker : IDisposable
{
    void PublishMessage<T>(string queue, T message);
    void Subscribe<T>(string queue, Func<T, Task> handler) where T : class;
}

public class MessageBroker : IMessageBroker
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<MessageBroker> _logger;
    private readonly Dictionary<string, IModel> _consumerChannels;

    public MessageBroker(string hostName, ILogger<MessageBroker> logger)
    {
        _logger = logger;
        _consumerChannels = new Dictionary<string, IModel>();

        var factory = new ConnectionFactory { HostName = hostName };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("Successfully connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public void PublishMessage<T>(string queue, T message)
    {
        try
        {
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: queue,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published message to queue: {Queue}", queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to queue: {Queue}", queue);
            throw;
        }
    }

    public void Subscribe<T>(string queue, Func<T, Task> handler) where T : class
    {
        try
        {
            var channel = _connection.CreateModel();
            _consumerChannels[queue] = channel;

            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    var message = JsonSerializer.Deserialize<T>(json);
                    if (message != null)
                    {
                        await handler(message);
                        channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message from queue: {Queue}", queue);
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
            _logger.LogInformation("Subscribed to queue: {Queue}", queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to queue: {Queue}", queue);
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var channel in _consumerChannels.Values)
        {
            if (channel.IsOpen)
                channel.Close();
        }

        if (_channel.IsOpen)
            _channel.Close();

        if (_connection.IsOpen)
            _connection.Close();

        _connection.Dispose();
        _logger.LogInformation("RabbitMQ connection closed");
    }
}