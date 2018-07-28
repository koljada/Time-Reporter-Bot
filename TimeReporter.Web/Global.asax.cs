using System.Web.Http;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using Microsoft.Bot.Connector;
using System.Reflection;
using System.Configuration;
using System.Web;

namespace TimeReporter.Web
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            Conversation.UpdateContainer(builder =>
            {
                builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

#if DEBUG
                var store = new InMemoryDataStore(); // volatile in-memory store
#else
                string connectionString = ConfigurationManager.AppSettings["TableStorage"];
                TableBotDataStore store = new TableBotDataStore(connectionString);
#endif

                builder.Register(c => store)
                    .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                    .AsSelf()
                    .SingleInstance();
            });
        }
    }
}
