using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeReporter.Web.Extensions;

namespace TimeReporter.Web.Models
{
    public class TimeReport : TableEntity
    {
        public TimeReport() : base()
        { }

        public TimeReport(string conversationId) : base(conversationId, DateTime.UtcNow.ToString("u"))
        {
            Start = DateTime.UtcNow;
            Breaks = new List<Break>();
        }

        public DateTime Start { get; set; }

        public IList<Break> Breaks { get; set; }

        public DateTime End { get; set; }

        public string ToReportString(TimeZoneInfo timeZone)
        {
            string result = $"Start: {Start.ToTimeZoneString(timeZone)} <br/>" +
                $"Break: {BreakDuration:n2}h <br/>" +
                $"End: {End.ToTimeZoneString(timeZone)} <br/>" +
                $"Duration: {TotalDuration:n2}h";

            return result;
        }

        public double BreakDuration { get => Breaks.Sum(x => x.Duration); set { } }

        public double TotalDuration { get => (End - Start).TotalHours - BreakDuration; set { } }

        public string BreaksJSON { get => JsonConvert.SerializeObject(Breaks); set { } }
    }
}