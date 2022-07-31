using System.Collections.Generic;

namespace ctf_final.PlanModels
{
    public class Plan
    {
        public bool IsFloating { get; set; }
        public bool IsYoga { get; set; }
        public bool IsPilates { get; set; }
        public string Type { get; set; }
        public int TimesPerWeek { get; set; }
        public string Duration { get; set; }
        public double Price { get; set; }
    }
    //[ID_02]
    public class PickedPlans
    {
        public Plan TrainPlan { get; set; }
        public Plan YogaPlan { get; set; }
        public Plan PilatesPlan { get; set; }

        public string TrainPlanExpiryDate { get; set; }
        public string YogaPlanExpiryDate { get; set; }
        public string PilatesPlanExpiryDate { get; set; }

        public int TrainAutoRenewal { get; set; }
        public int YogaAutoRenewal { get; set; }
        public int PilatesAutoRenewal { get; set; }
    }
    public class PlanList : List<Plan>
    {
        public string TimesPerWeekString { get; set; }
        public string Type { get; set; }
        public bool FirstOfItsType { get; set; }
        public string Color { get; set; }
        public PlanList(int tpw, string type = "", bool first = false, string color = null)
        {
            Type = type;
            TimesPerWeekString = tpw + "x POR SEMANA";
            FirstOfItsType = first;
            Color = color;
        }
    }
    public class TemporaryPlanPricesList
    {
        public List<string> PricesList { get; set; }
    }

    public class WLFilter
    {
        public List<string> Classes { get; set; }
    }
}

/*
public class test_Plan
{
    public enum PlanDuration
    {
        Mensal,
        Trimestral,
        Semestral,
        Anual
    }

    public enum PlanType
    {
        Train,
        Yoga,
        Pilates
    }

    public PlanType Type { get; set; }
    public int TimesPerWeek { get; set; }
    public PlanDuration Duration { get; set; }
    public double Price { get; set; }
}
*/
