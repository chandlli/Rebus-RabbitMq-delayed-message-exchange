# Transport strategy

`MyTransport` implements `ITransport, IDisposable, IInitializable, ISubscriptionStorage`

When `Send` method is called and the message contains the header `rbs2-deferred-until`, the message is sent to exchange with type `x-delayed-message` and the exchange send message to Rebus queue endpoint on schedule time. `Send` add header `x-delay` with milliseconds value to delay message.

[RabbitMQ Delayed Message Plugin](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange) reference.