using System;
using System.Collections.Generic;
using System.Text;
using XamarinFirebase.Model;

namespace ctf_final.Models
{
    public class SimplifiedUser
    {
        public int UserID { get; set; }
        public int PlanAbscence { get; set; }
        public string Name { get; set; }
        public string Birthday { get; set; }
        public string PictureToken { get; set; }
    }

    public class ExpiryResume
    {
        public class Resume
        {
            public int UserID { get; set; }
            public string ExpiryDate { get; set; }
            public string ExpiryDateYoga { get; set; }
            public string ExpiryDatePilates { get; set; }
        }
        public List<Resume> DateList { get; set; }
    }
}
