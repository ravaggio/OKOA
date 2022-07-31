using System.Collections.Generic;
using ctf_final.PlanModels;

namespace XamarinFirebase.Model
{
    public class Events
    {
        public int ID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }

        public List<int> ConfirmedUsers { get; set; }
    }
}

