# Lerk's Voice Attack API Plugin
Allows voice attack to trigger commands from an api and/or message queue


# How to use
Download release ZIP, unzip directly into your VoiceAttack folder, in the Apps directory. For example: C:\Program Files (x86)\VoiceAttack\Apps\

After launching VoiceAttack, Call any command by posting or doing a get request at http://localhost:55569/json/reply/QueueCommandRequest?CommandName={your-command-name-here}

# Configuration
Check the configuration file to change what port your api runs on, or whether or not to use RabbitMQ, and change rabbitmq connection string options

# Rabbit MQ
You can use RabbitMQ and publish messages directly to that, or thru the web api and it will be stored in your RabbitMQ queue instead of an in-memory message queue.

Here is a guide on how to install rabbit mq on windows: https://github.com/ServiceStack/rabbitmq-windows
