# Lerk's Voice Attack HTTP Plugin
Allows voice attack to trigger commands from an http get (or any other VERB) and/or rabbit message queue


# How to use
Download release ZIP, unzip directly into your VoiceAttack folder, in the Apps directory. For example: C:\Program Files (x86)\VoiceAttack\Apps\

After launching VoiceAttack, Call any command by posting or doing a get request at http://localhost:55569/json/reply/QueueCommandRequest?CommandName={your-command-name-here}

You can also set voiceattack variables by using the SmallInts, Ints, Decimals, Strings, Booleans, and Dates request variables. This will set a voiceattack variable with the name and value you specify. Example: http://localhost:55569/json/reply/QueueCommandRequest?CommandName={your-command-name-here}&SmallInts={Name:UserCount,Value:25}

# Alerts Queue

There is also now a separate queue for alerts that you can put a pause after (streamelements has a 10 second delay by default)

You can add an alert to the alert queue by hitting this URL:  http://localhost:55569/json/reply/AlertQueueRequest?CommandName={your-alert-command-name-here}&Pause=10000&User={TwitchSubscriber}

Supported request params for AlertQueueRequest: User, Gifter, Amount, Pause

This will execute the command specified, and set variables in VoiceAttack that correspond to each of those properties.

 -  User : {TXT:TwitchAlertUser}
 -  Gifter : {TXT:TwitchAlertGifter}
 -  Amount: {DEC:TwitchAlertAmount}

# Browsing the API

Visit http://localhost:55569/metadata to browse the API endpoint definitions

# Configuration File
This plugin works out of the box with 0 configuration, however there are a few options in the config file.

You can change or add different hostnames or ports to listen on, for example, if you want to allow other computers on your network to access the VoiceAttack API on your machine, you can add a listener to listen on your "LAN" ip, instead of localhost/127.0.0.1. For Example: http://192.168.1.105:55569/

Note: Listeners must end in "/" or the plugin will malfunction (just use the current listeners in the config file as an example of how one should look)

Check the configuration file to change or add the urls/listeners you'd like to listen on for the VA API (if you'd like to listen on a different host name than localhost, or a different IP address than 127.0.0.1), or whether or not to use RabbitMQ, and change rabbitmq connection string options

There is also an untested option to use RabbitMQ if you want to have a persistent queue off commands (in case you have a huge queue of commands and need them to stay in the queue if you turn off voice attack, or if you want to have many instances of voice attack reading from a single queue for load balancing)

# Rabbit MQ
You can use RabbitMQ and publish messages directly to that, or thru the web api and it will be stored in your RabbitMQ queue instead of an in-memory message queue.

Here is a guide on how to install rabbit mq on windows: https://github.com/ServiceStack/rabbitmq-windows
