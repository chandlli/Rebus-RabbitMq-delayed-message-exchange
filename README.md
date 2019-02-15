# Rebus with RabbitMq delayed message exchange plugin

Two sample strategies to Rebus work together with plugin [RabbitMQ Delayed Message Plugin](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)

## Getting Started

Build `Dockerfile` in root folder or run command:

```
docker run -p 5672:5672 -p 8080:15672 chandlli/rabbitmq-with-delayed-plugin:latest
```

RabbitMq UI exposed on `http://localhost:8080`

## Strategies

### Custom `ITimeoutManager`

[Code](https://github.com/chandlli/Rebus-RabbitMq-delayed-message-exchange/blob/master/src/Timeout/README.md)

### Custom `ITransport`

Working ...