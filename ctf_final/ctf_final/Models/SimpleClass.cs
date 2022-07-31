using System.Collections.Generic;
using XamarinFirebase.Model;

namespace ctf_final.Models
{
    public class SimpleClass
    {
        public List<int> StudentsIDs { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }

        public void FromSchedules(Schedule s, Schedule.Weekday wd, string date)
        {
            StudentsIDs = wd.StudentsList;
            Date = date;
            Time = s.Time;
            Type = s.Type;
        }
    }
}
