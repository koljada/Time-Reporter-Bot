using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TimeReporter.Web.Extensions;
using TimeReporter.Web.Models;

namespace TimeReporter.Web.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        #region Constants

        private readonly string[] _startCommands = new[] { "start", "-s", "in" };
        private readonly string[] _startBreakCommands = new[] { "break", "-b", "break start" };
        private readonly string[] _edBreakCommands = new[] { "work", "-w", "break end" };
        private readonly string[] _endCommands = new[] { "end", "-e", "out" };

        private const string CURRENT_REPORT = "CurrentReport";
        private const string CURRENT_TIMEZONE = "CurrentTimezone";

        public const string HELP_TEXT = "Here are my commands:<br/>" +
            "- type `start` or `in` to save start working time;<br/>" +
            "- type `break` or `break start` or `-b` to save break start time;<br/>" +
            "- type `work` or `break end` or `-w` to save break end time;<br/>" +
            "- type `end` or `out` to save end working time and print your report;<br/>" +
            "- type `timezone` to select your timezone<br/>" +
            "- type `help` to get help";

        #endregion

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;

            string text = activity.Text.Trim();

            string textInLowerCase = text.ToLower();

            TimeReport current = context.PrivateConversationData.GetValueOrDefault<TimeReport>(CURRENT_REPORT);

            TimeZoneInfo tz = context.PrivateConversationData.GetValueOrDefault<TimeZoneInfo>(CURRENT_TIMEZONE);

            if (_startCommands.Contains(textInLowerCase))
            {
                if (current != null)
                {
                    await context.SayAsync("You have not finished the previos report. The data will be lost.");
                }

                Activity reply = activity.CreateReply($"You started working at {DateTime.UtcNow.ToTimeZoneString(tz)}");

                reply.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction> {
                                new CardAction("imBack", "Start break", null, "break start"),
                                new CardAction("imBack", "End working", null, "end")
                            }
                };

                context.PrivateConversationData.SetValue(CURRENT_REPORT, new TimeReport(reply.Conversation.Id));

                await context.PostAsync(reply);
            }
            else if (_endCommands.Contains(textInLowerCase))
            {
                if (current != null)
                {
                    current.End = DateTime.UtcNow;

                    await SaveTimeReport(current);

                    await context.SayAsync("Here is your report:");

                    Activity reply = activity.CreateReply(current.ToReportString(tz));

                    reply.SuggestedActions = new SuggestedActions
                    {
                        Actions = new List<CardAction> {
                                new CardAction("imBack", "Start working", null, "in")
                            }
                    };

                    context.PrivateConversationData.RemoveValue(CURRENT_REPORT);

                    await context.PostAsync(reply);
                }
                else
                {
                    await PostNotStartedReportReply(context, activity);
                }
            }
            else if (_startBreakCommands.Contains(textInLowerCase))
            {
                if (current != null)
                {
                    Activity reply = activity.CreateReply();

                    reply.SuggestedActions = new SuggestedActions
                    {
                        Actions = new List<CardAction> { new CardAction("imBack", "End break", null, "break end") }
                    };

                    if (current.Breaks.Any(b => b.End == default(DateTime)))
                    {
                        reply.Text = "You have not finished the previos break";
                    }
                    else
                    {
                        current.Breaks.Add(new Break());
                        reply.Text = $"You started a break at {DateTime.UtcNow.ToTimeZoneString(tz)}";
                        context.PrivateConversationData.SetValue(CURRENT_REPORT, current);
                    }

                    await context.PostAsync(reply);
                }
                else
                {
                    await PostNotStartedReportReply(context, activity);
                }
            }
            else if (_edBreakCommands.Contains(textInLowerCase))
            {
                if (current != null)
                {
                    Activity reply = activity.CreateReply();

                    reply.SuggestedActions = new SuggestedActions
                    {
                        Actions = new List<CardAction> { new CardAction("imBack", "Start break", null, "break") }
                    };

                    var lastBreak = current.Breaks.LastOrDefault();
                    if (lastBreak == null || lastBreak.End != default(DateTime))
                    {
                        reply.Text = "You have not started a break yet. Type `break` to start the break";
                    }
                    else
                    {
                        lastBreak.End = DateTime.UtcNow;
                        context.PrivateConversationData.SetValue(CURRENT_REPORT, current);
                        reply.Text = $"You ended a break at {DateTime.UtcNow.ToTimeZoneString(tz)}. Duration: {lastBreak.Duration:n2}h";
                        reply.SuggestedActions.Actions.Add(new CardAction("imBack", "End working", null, "end"));
                    }

                    await context.PostAsync(reply);
                }
                else
                {
                    await PostNotStartedReportReply(context, activity);
                }
            }
            else if (text.Contains("timezone"))
            {
                Activity reply = activity.CreateReply();
                reply.SuggestedActions = new SuggestedActions
                {
                    Actions = TimeZoneInfo
                        .GetSystemTimeZones()
                        .Select(x => new CardAction("imBack", x.DisplayName, null, "timezone " + x.Id))
                        .ToList()
                };

                Match match = Regex.Match(text, "timezone (.+)");
                if (match.Success)
                {
                    string zoneId = match.Groups[1].Value;
                    try
                    {
                        TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
                        context.PrivateConversationData.SetValue(CURRENT_TIMEZONE, timezone);
                        reply.Text = $"Timezone {timezone.DisplayName} is selected";
                        reply.SuggestedActions = null;
                    }
                    catch (Exception ex)
                    {
                        reply.Text = $"Timezone {zoneId} was not found. Please select an appropriate timezone";
                    }
                }
                else
                {
                    reply.Text = "Please select timezone:";
                }

                await context.PostAsync(reply);
            }
            else if (text == "help")
            {
                await context.SayAsync(HELP_TEXT);
            }
            else
            {
                await context.SayAsync("Sorry, I didn't get. <br/>" + HELP_TEXT);
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task PostNotStartedReportReply(IBotContext context, Activity activity)
        {
            Activity reply = activity.CreateReply("You have not started a report yet");

            reply.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction> { new CardAction("imBack", "Start working", null, "in") }
            };

            await context.PostAsync(reply);
        }

        private async Task SaveTimeReport(TimeReport report)
        {
            try
            {
                string connectionString = ConfigurationManager.AppSettings["TableStorage"];
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("TimeReport");
                table.CreateIfNotExists();

                await table.ExecuteAsync(TableOperation.Insert(report));
            }
            catch (Exception ex)
            { }
        }
    }
}