using System;
using ServiceStack.RabbitMq;
using ServiceStack.Messaging;
using ServiceStack;
using ServiceStack.Logging;
using CSharpFunctionalExtensions;
using MessageQueuePlugin.ServiceModel;
using MessageQueuePlugin.ServiceInterface;
using ServiceStack.Configuration;
using System.Collections.Generic;

namespace MessageQueuePlugin.ServiceModel
{
    public class VAExecCommand
    {
        public string CommandName { get; set; }
    }

    public class QueueCommandRequest
    {
        public string CommandName { get; set; }
    }

    public class QueueCommandResponse
    {
        public Result Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}

namespace MessageQueuePlugin.ServiceInterface
{
    /// <summary>
    /// This is a web service that will be hosted on localhost
    /// </summary>
    public class VoiceAttackMQServices : Service
    {
        public QueueCommandResponse Any(QueueCommandRequest request)
        {
            using (var mqClient = TryResolve<IMessageService>().CreateMessageQueueClient())
            {
                mqClient.Publish(new VAExecCommand { CommandName = request.CommandName });
            }

            return new QueueCommandResponse()
            {
                Result = Result.Success()
            };
        }
    }
}


namespace MessageQueuePlugin
{

    public class AppHost : AppSelfHostBase
    {
        public AppHost()
          : base("Lerk's MessageQueue Plugin Self-Host", typeof(VoiceAttackMQServices).Assembly) { }

        public override void Configure(Funq.Container container) 
        {

            AppSettings = new MultiAppSettingsBuilder()
                .AddEnvironmentalVariables()
                .AddTextFile("~/settings.txt".MapAbsolutePath())
                .Build();


            var useRabbitMq = AppSettings.Get("rabbitmq:enable", false);
            if (useRabbitMq)
            {
                var rabbitMqConnectionString = AppSettings.Get("rabbitmq:connectionString", "localhost:5672");
                var rabbitMqUserName = AppSettings.Get<string>("rabbitmq:userName", null);
                var rabbitMqPassword = AppSettings.Get<string>("rabbitmq:passWord", null);
                container.Register<IMessageService>(c => new RabbitMqServer(rabbitMqConnectionString, rabbitMqUserName, rabbitMqPassword));
            } 
            else
            {
                container.Register<IMessageService>(c => new BackgroundMqService());
            }

        }
    }


    public class VoiceAttackPlugin
    {

        const string C_APP_NAME = "Lerk's VoiceAttack MessageQueue Plugin";
        const string C_APP_VERSION = "v0.2";

        public static ILog Logger = LogManager.GetLogger(typeof(VoiceAttackPlugin));
        static AppHost _appHost = null;

        public static string VA_DisplayName()
        {
            return $"{C_APP_NAME} {C_APP_VERSION}";
        }

        public static string VA_DisplayInfo()
        {
            return $"{C_APP_NAME} allows VoiceAttack to trigger commands by consuming messages from a message queue";  
        }

        public static Guid VA_Id()
        {
            return new Guid("{E51D41B3-3D1F-4E27-8976-874BA8DF37B5}");  
        }

        public static void VA_StopCommand()
        {

        }

        public static void VA_Init1(dynamic vaProxy)
        {
            _appHost = new AppHost();
            _appHost.Init();

            var listeners = _appHost.AppSettings.Get<List<string>>("config:listeners");
            _appHost.Start(listeners);

            var mqServer = _appHost.Container.Resolve<IMessageService>();

            mqServer.RegisterHandler<VAExecCommand>(m =>
            {
                VAExecCommand request = m.GetBody();

                //do not run the command if it does not exist
                if (!vaProxy.Command.Exists(request.CommandName))
                {
                    return null;
                }

                //execute command. if you don't want it to execute the same command twice
                //you should set that up in the command advanced options
                vaProxy.Command.Execute(request.CommandName);
                return null;

            });
            mqServer.Start();
        }


        public static void VA_Exit1(dynamic vaProxy)
        {
            _appHost.Dispose();
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
       
        }

    }

}