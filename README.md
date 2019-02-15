# Rebus with RabbitMq delayed message exchange plugin

Two examples strategies to Rebus work together with plugin [RabbitMQ Delayed Message Plugin](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)

## Getting Started

Build `Dockerfile` in root folder or run command:

```
docker run -p 5672:5672 -p 8080:15672 chandlli/rabbitmq-with-delayed-plugin:latest
```
This image contains RabbitMQ Delayed Message Plugin

RabbitMq UI exposed on `http://localhost:8080`

## Strategies

#### Custom `ITimeoutManager`

[Code](https://github.com/chandlli/Rebus-RabbitMq-delayed-message-exchange/blob/master/src/Timeout)

#### Custom `ITransport`

Working ...