using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContinentalApplication.Models
{
    public class Calendar
    {
        public int Days { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string FirstDay { get; set; }
        public List<int> BusyDays { get; set; } // busy days marked with 1, availble days 0
        public string Investigator { get; set; }
    }
}