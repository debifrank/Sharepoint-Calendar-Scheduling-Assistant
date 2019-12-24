using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;

using ContinentalApplication.Helpers;
using ContinentalApplication.Models;

namespace ContinentalApplication.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Investigators", "Home");
            }

            else
            {
                return View();
            }
        }

        [Authorize]
        public async Task<ActionResult> Investigators()
        {
            string groupId = await GraphHelper.GetGroupId();
            var investigators = await GraphHelper.GetGroupMembers(groupId);

            var events = await GraphHelper.GetGroupEvents(groupId);

            for (int i = 0; i<investigators.Count; i++)
            {
                var attendeeEvents = GraphHelper.GetGroupEventsForAttendee(events, investigators[i].Name);
                var busyDaysForAttendee = GraphHelper.GetBusyForSortingPerson(attendeeEvents);
                Tuple<string, int, string> tup = GraphHelper.GetSortingStatusForPerson(busyDaysForAttendee);
                investigators[i].Status = tup.Item1;
                investigators[i].Rank = tup.Item2;
                investigators[i].Word = tup.Item3;
            }

            // Sort investigators based on their rank:
            var sortedInvestigators = investigators.OrderBy(Investigator => Investigator.Rank);

            return View(sortedInvestigators);
        }

        public ActionResult Error(string message, string debug)
        {
            Flash(message, debug);
            return RedirectToAction("Index");
        }
    }
}