using System;

namespace TimeReporter.Web.Models
{
    public class Break
    {
        public Break()
        {
            Start = DateTime.UtcNow;
        }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public double Duration => (End - Start).TotalHours;
    }
}