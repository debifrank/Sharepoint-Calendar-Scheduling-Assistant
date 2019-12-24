using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Collections.Generic;

using ContinentalApplication.Helpers;
using ContinentalApplication.Models;

namespace ContinentalApplication.Controllers
{
    public class CalendarController : BaseController
    {
        // GET: Calendar
        [Authorize]
        public async Task<ActionResult> Index()
        {
            var events = await GraphHelper.GetEventsAsync();

            // Change start and end dates from UTC to local time
            foreach (var ev in events)
            {
                ev.Start.DateTime = DateTime.Parse(ev.Start.DateTime).ToLocalTime().ToString();
                ev.Start.TimeZone = TimeZoneInfo.Local.Id;
                ev.End.DateTime = DateTime.Parse(ev.End.DateTime).ToLocalTime().ToString();
                ev.End.TimeZone = TimeZoneInfo.Local.Id;
            }

            return View(events);
        }

        public async Task<ActionResult> InvestigatorCalendar(string name, int month)
        {
            // Getting current date
            DateTime dt = DateTime.Now;

            // Shared Calendars ------------------------------------------------
            // var events = await GraphHelper.GetEventsForOrganizer(name, month);
            // -----------------------------------------------------------------

            // Group Calendar --------------------------------------------------
            var groupId = await GraphHelper.GetGroupId();
            var events = await GraphHelper.GetGroupEvents(groupId);
            var attendeeEvents = GraphHelper.GetGroupEventsForAttendee(events, name);
            // -----------------------------------------------------------------


            Calendar calendar = new Calendar();

            calendar.Investigator = name;

            // Setting the number of days in our month, the month (as an int) and the year
            calendar.Days = DateTime.DaysInMonth(dt.Year, dt.Month);
            calendar.Month = dt.Month;
            calendar.Year = dt.Year;

            // Getting the first day of our month to determine the day of the week we are starting on for calendar visualization
            dt = new DateTime(calendar.Year, calendar.Month, 1);

            // Setting our calendar's starting day of the week
            calendar.FirstDay = dt.DayOfWeek.ToString();

            // Shared Calendars --------------------------------------------------
            // calendar.BusyDays = new List<int>();
            // -------------------------------------------------------------------

            // Group Calendar ----------------------------------------------------
            calendar.BusyDays = GraphHelper.GetBusyDaysForPerson(attendeeEvents);
            // -------------------------------------------------------------------

            // Calendar object should have all the necessary information to populate a calendar view
            return View(calendar);
        }
    }
}