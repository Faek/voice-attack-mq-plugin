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

    public class VASmallIntVariable
    {
        public string Name { get; set; }
        public short? Value { get; set; }
    }

    public class VAIntVariable
    {
        public string Name { get; set; }
        public int? Value { get; set; }
    }

    public class VATextVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class VADecimalVariable
    {
        public string Name { get; set; }
        public decimal? Value { get; set; }
    }

    public class VABooleanVariable
    {
        public string Name { get; set; }
        public bool? Value { get; set; }
    }

    public class VADateVariable
    {
        public string Name { get; set; }
        public DateTime? Value { get; set; }
    }

    //we are reusing the command request object 
    //as the message queue request object
    //because they are identical
    public class QueueCommandRequest
    {
        public string CommandName { get; set; }
        public List<VASmallIntVariable> SmallInts { get; set; }
        public List<VAIntVariable> Ints { get; set; }
        public List<VADecimalVariable> Decimals { get; set; }
        public List<VATextVariable> Strings { get; set; }
        public List<VABooleanVariable> Booleans { get; set; }
        public List<VADateVariable> Dates { get; set; }
    }

    public class QueueCommandResponse
    {
        public Result Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public enum AlertType
    {
        Follower,
        Sub,
        Gift,
        MassGift,
        Bits,
        Donation
    }

    public class AlertQueueRequest
    {
        public AlertType Type { get; set; }
        public string CommandName { get; set; }
        public string User { get; set; }
        public string Gifter { get; set; }
        public decimal? Amount { get; set; }
        public int? Pause { get; set; }
    }


    public class AlertQueueResponse
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
            using (var mqClient = TryResolve<ICommandQueueService>().CreateMessageQueueClient())
            {
                mqClient.Publish(request);
            }

            return new QueueCommandResponse()
            {
                Result = Result.Success()
            };
        }

        public AlertQueueResponse Any(AlertQueueRequest request)
        {
            using (var mqClient = TryResolve<IAlertQueueService>().CreateMessageQueueClient())
            {
                mqClient.Publish(request);
            }

            return new AlertQueueResponse()
            {
                Result = Result.Success()
            };
        }
    }
}


namespace MessageQueuePlugin
{

    public interface ICommandQueueService : IMessageService
    {

    }

    public class CommandQueueService : BackgroundMqService, ICommandQueueService
    {

    }

    public class RabbitCommandQueueService : RabbitMqServer, ICommandQueueService
    {
        public RabbitCommandQueueService(string connectionString = "localhost", string username = null, string password = null) : base(connectionString, username, password)
        {
        }
    }

    public interface IAlertQueueService : IMessageService
    {

    }

    public class AlertQueueService : BackgroundMqService, IAlertQueueService
    {

    }

    public class RabbitAlertQueueService : RabbitMqServer, IAlertQueueService
    {
        public RabbitAlertQueueService(string connectionString = "localhost", string username = null, string password = null) : base(connectionString, username, password)
        {
        }
    }

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
                container.Register<ICommandQueueService>(c => new RabbitCommandQueueService(rabbitMqConnectionString, rabbitMqUserName, rabbitMqPassword));

                var rabbitMqAlertConnectionString = AppSettings.Get("rabbitmqalert:connectionString", "localhost:5673");
                var rabbitMqAlertUserName = AppSettings.Get<string>("rabbitmqalert:userName", null);
                var rabbitMqAlertPassword = AppSettings.Get<string>("rabbitmqalert:passWord", null);
                container.Register<IAlertQueueService>(c => new RabbitAlertQueueService(rabbitMqAlertConnectionString, rabbitMqAlertUserName, rabbitMqAlertPassword));
            } 
            else
            {
                container.Register<ICommandQueueService>(c => new CommandQueueService());
                container.Register<IAlertQueueService>(c => new AlertQueueService());
            }

        }
    }


    public class VoiceAttackPlugin
    {

        const string C_APP_NAME = "Lerk's VoiceAttack HTTP Plugin";
        const string C_APP_VERSION = "v0.4";

        public static ILog Logger = LogManager.GetLogger(typeof(VoiceAttackPlugin));
        static AppHost _appHost = null;

        public static string VA_DisplayName()
        {
            return $"{C_APP_NAME} {C_APP_VERSION}";
        }

        public static string VA_DisplayInfo()
        {
            return $"{C_APP_NAME} allows VoiceAttack to trigger commands by HTTP (Any Verb)";  
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

            var commandQueueService = _appHost.Container.Resolve<ICommandQueueService>();

            //wire handler for receiving a message from the message queue
            commandQueueService.RegisterHandler<QueueCommandRequest>(m =>
            {
                QueueCommandRequest request = m.GetBody();

                //loop thru all of the variables of each data type
                //and store them inside of voice attack
                request.SmallInts?.Each(v => vaProxy.SetSmallInt(v.Name, v.Value));
                request.Ints?.Each(v => vaProxy.SetInt(v.Name, v.Value));
                request.Strings?.Each(v => vaProxy.SetText(v.Name, v.Value));
                request.Decimals?.Each(v => vaProxy.SetDecimal(v.Name, v.Value));
                request.Booleans?.Each(v => vaProxy.SetBoolean(v.Name, v.Value));
                request.Dates?.Each(v => vaProxy.SetDate(v.Name, v.Value));

                //do not run the command if it does not exist
                if (!vaProxy.Command.Exists(request.CommandName))
                    return null;
                
                //execute command. if you don't want it to execute the same command twice
                //you should set that up in the command advanced options
                vaProxy.Command.Execute(request.CommandName);

                return null;

            });

            commandQueueService.Start();


            var alertQueueService = _appHost.Container.Resolve<IAlertQueueService>();

            //wire handler for receiving a message from the message queue
            alertQueueService.RegisterHandler<AlertQueueRequest>(m =>
            {
                AlertQueueRequest request = m.GetBody();

                //do not run the command if it does not exist
                if (!vaProxy.Command.Exists(request.CommandName))
                    return null;

                if (!string.IsNullOrWhiteSpace(request.User))
                    vaProxy.SetText("TwitchAlertUser", request.User);

                if (!string.IsNullOrWhiteSpace(request.Gifter))
                    vaProxy.SetText("TwitchAlertGifter", request.Gifter);

                if (request.Amount.HasValue)
                    vaProxy.SetDecimal("TwitchAlertAmount", request.Amount.Value);

                //execute command. if you don't want it to execute the same command twice
                //you should set that up in the command advanced options
                vaProxy.Command.Execute(request.CommandName);

                if (request.Pause.HasValue)
                    System.Threading.Thread.Sleep(request.Pause.Value);

                return null;

            });

            alertQueueService.Start();

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