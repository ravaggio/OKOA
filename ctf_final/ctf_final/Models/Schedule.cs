using System;
using System.Collections.Generic;
using System.Text;

namespace XamarinFirebase.Model
{
    public class Schedule
    {
        public int Id { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }

        public class Weekday {
            public Weekday()
            {
                StudentsList = new List<int>();
            }
            public int Day { get; set; }
            public List<int> StudentsList { get; set; }
        }
        public List<Weekday> Classes { get; set; }

        public void FromOldSchedule(Schedule oldSchedule = null)
        {
            Id = oldSchedule.Id;
            List<Weekday> wds = new List<Weekday>();
            oldSchedule.Classes.ForEach(oldClasses =>
            {
                Weekday wd = new Weekday
                {
                    Day = oldClasses.Day,
                    StudentsList = new List<int>(oldClasses.StudentsList)
                };
                wds.Add(wd);
            });
            Classes = wds;
            Time = oldSchedule.Time;
            Type = oldSchedule.Type;
        }
    }

    public class ScheduleHistory
    {
        public List<string> History { get; set; }
    }
}
