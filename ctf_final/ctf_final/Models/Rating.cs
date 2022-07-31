using System;
using System.Collections.Generic;
using System.Text;

namespace ctf_final
{
    public class Rating
    {
        public string Date { get; set; }
        public string Weight { get; set; }
        public string Height { get; set; }
        public string Mass { get; set; }
        public string Fat { get; set; }
        public string Mobility { get; set; }

        public Rating()
        {
            Fat = "";
            Mass = "";
            Height = "";
            Weight = "";
            Mobility = "";
        }
    }
}
