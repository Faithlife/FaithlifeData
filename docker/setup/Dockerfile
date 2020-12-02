FROM ubuntu:18.04

RUN apt-get update && apt-get install -y docker.io wget

ENV DOCKERIZE_VERSION v0.6.1
RUN wget https://github.com/jwilder/dockerize/releases/download/$DOCKERIZE_VERSION/dockerize-linux-amd64-$DOCKERIZE_VERSION.tar.gz \
    && tar -C /usr/local/bin -xzvf dockerize-linux-amd64-$DOCKERIZE_VERSION.tar.gz \
    && rm dockerize-linux-amd64-$DOCKERIZE_VERSION.tar.gz

COPY setup.sh /setup.sh
RUN chmod +x /setup.sh

CMD dockerize -wait tcp://postgres:5432 -wait tcp://mysql:3306 -wait tcp://mssql:1433 /setup.sh
