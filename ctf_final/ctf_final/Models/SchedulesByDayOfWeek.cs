using System.Collections.Generic;
namespace ctf_final.Models
{
    public class SchedulesByDayOfWeek
    {
        public class Times
        {
            public string Date { get; set; }
            public string Time { get; set; }
            public string Type { get; set; }
            public List<int> StudentsList { get; set; }
        }
        public int DayOfWeek { get; set; }
        public List<Times> Classes { get; set; }

        public List<string> ClassesTimeAndType { get; set; }

        //Deprecated
        public List<string> TimesOverview { get; set; }
    }
}
