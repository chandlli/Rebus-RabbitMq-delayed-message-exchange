# Timeout strategy

`RabbitMqDelayedMessageTimeout` implements `ITimeoutManager`

When `Defer` method is called, the message is sent to exchange with type `x-delayed-message` and the exchange send message to Rebus queue endpoint on schedule time.

[RabbitMQ Delayed Message Plugin](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange) reference.