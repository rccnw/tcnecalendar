using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TcneCalendar.Data
{
    public class ScheduleData
    {
        public class AppointmentData
        {
            public int Id { get; set; }
            public string Subject { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public Nullable<bool> IsAllDay { get; set; }
            public string CategoryColor { get; set; } = string.Empty;
            public string RecurrenceRule { get; set; } = string.Empty;
            public string EventType { get; set; } = string.Empty;
            public Nullable<int> RecurrenceID { get; set; }
            public string RecurrenceException { get; set; } = string.Empty;
            public string StartTimezone { get; set; } = string.Empty;
            public string EndTimezone { get; set; } = string.Empty;
            public string CssClass { get; set; } = string.Empty;
        }
        public class ResourceData : AppointmentData
        {
            public int ProjectId { get; set; }
            public int CategoryId { get; set; }
        }
    }
}
