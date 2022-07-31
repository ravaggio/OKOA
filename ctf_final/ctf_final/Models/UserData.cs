using System.Collections.Generic;

namespace ctf_final.Models
{
    public class UserData
    {
        public UserData()
        {
            UserClasses = new List<SimpleClass>();
        }
        public List<SimpleClass> UserClasses { get; set; }
    }
}
