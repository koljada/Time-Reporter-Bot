using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TimeReporter.Web.Dialogs;

namespace TimeReporter.Web
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<IHttpActionResult> Post([FromBody]Activity activity)
        {
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }

            return Ok();
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            string messageType = message.GetActivityType();
            if (messageType == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (messageType == ActivityTypes.ConversationUpdate)
            {
                ChannelAccount newMember = message.MembersAdded?.FirstOrDefault();

                if (newMember?.Id != message.Recipient.Id)
                {
                    ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl));

                    Activity reply = message.CreateReply();

                    reply.Text = "Hello, I'm Time Reporter Bot.<br/>I can help you to create the time report.<br/>" + RootDialog.HELP_TEXT;

                    await client.Conversations.ReplyToActivityAsync(reply);
                }
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (messageType == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (messageType == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (messageType == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}