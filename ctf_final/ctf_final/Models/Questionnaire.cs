using System;
using System.Collections.Generic;
using System.Text;

namespace ctf_final.Models
{
    public class Questionnaire
    {
        public int QuestionnaireID { get; set; }
        public string QuestionnaireTitle { get; set; }
        public string CreationDate { get; set; }

        public Question Q1 { get; set; }
        public Question Q2 { get; set; }
        public Question Q3 { get; set; }

        public List<int> ReplyIDs { get; set; }
        public int Closed { get; set; }
    }

    public class Question
    {
        public int QuestionID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public List<Reply> ReplyList { get; set; }
    }

    public class Reply
    {
        public int UserID { get; set; }
        public string Answer { get; set; }
    }
}
