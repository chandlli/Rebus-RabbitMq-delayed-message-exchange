FROM rabbitmq:3.7.8-management

RUN apt-get update

RUN apt-get install -y curl

RUN apt-get update && apt-get install -y unzip && apt-get install wget -y

RUN wget "https://dl.bintray.com/rabbitmq/community-plugins/3.7.x/rabbitmq_delayed_message_exchange/rabbitmq_delayed_message_exchange-20171201-3.7.x.zip"

RUN unzip rabbitmq_delayed_message_exchange-20171201-3.7.x.zip

RUN cp rabbitmq_delayed_message_exchange-20171201-3.7.x.ez $RABBITMQ_HOME/plugins/rabbitmq_delayed_message_exchange-20171201-3.7.x.ez

RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange

RUN rabbitmq-plugins enable --offline rabbitmq_consistent_hash_exchange

EXPOSE 5672
EXPOSE 15672