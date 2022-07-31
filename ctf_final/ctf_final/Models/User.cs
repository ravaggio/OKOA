using System.Collections.Generic;
using ctf_final.PlanModels;

namespace XamarinFirebase.Model
{
    public class User
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string PictureToken { get; set; }
        public string Function { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Birthday { get; set; }
        public int Gender { get; set; }

        //Schedule ID + @ + DayOfWeek + @ + Time/Type
        public List<string> ScheduleReferences { get; set; }

        //Date/Time/Type + @ + Command
        public List<string> ClassesExceptions { get; set; }

        public List<string> MCTrainDates { get; set; }
        public List<string> MCYogaDates { get; set; }
        public List<string> MCPilatesDates { get; set; }

        public int MakeupClasses { get; set; }
        public int MakeupClassesYoga { get; set; }
        public int MakeupClassesPilates { get; set; }

        public PickedPlans UserPlan { get; set; }
        public int PlanAbscence { get; set; }
        public string PlanAbscenceDate { get; set; }
        public List<ctf_final.Rating> Ratings { get; set; }
    }
}
