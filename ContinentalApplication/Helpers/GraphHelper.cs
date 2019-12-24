using Microsoft.Graph;
using Microsoft.Identity.Client;

using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using System.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;

using ContinentalApplication.TokenStorage;
using ContinentalApplication.Models;
using System;

namespace ContinentalApplication.Helpers
{
    public static class GraphHelper
    {
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string graphScopes = ConfigurationManager.AppSettings["ida:AppScopes"];

        public static async Task<IEnumerable<Event>> GetEventsAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var events = await graphClient.Me.Events.Request()
                .Select("subject,organizer,start,end")
                .OrderBy("createdDateTime DESC")
                .GetAsync();

            return events.CurrentPage;

        }

        // -------------------------------------------------------------------------------------------------------
        // Group Calendar Methods

        private static string groupName = "Air Safety";
        public static async Task<string> GetGroupId()
        {
            var graphClient = GetAuthenticatedClient();

            var groups = await graphClient.Groups.Request().GetAsync();

            foreach (var group in groups)
            {
                if (group.DisplayName == groupName)
                {
                    return group.Id;
                }
            }

            return null;
        }

        public static async Task<List<Investigator>> GetGroupMembers(string id)
        {
            var graphClient = GetAuthenticatedClient();

            var members = await graphClient.Groups[id].Members
                .Request()
                .GetAsync();

            List<Investigator> investigators = new List<Investigator>();

            foreach (User us in members.CurrentPage)
            {
                investigators.Add(new Investigator
                {
                    Name = us.DisplayName,
                    Number = us.MobilePhone,
                    Status = "",
                    Rank = 0
                });
            }

            return investigators;
        }

        public static async Task<IEnumerable<Event>> GetGroupEvents(string id)
        {
            var graphClient = GetAuthenticatedClient();

            return await graphClient.Groups[id].Calendar.Events.Request().GetAsync();
        }

        public static IEnumerable<Event> GetGroupEventsForAttendee(IEnumerable<Event> events, string attendee)
        {
            List<Event> toReturn = new List<Event>();
            foreach(var ev in events){
                foreach(var person in ev.Attendees)
                {
                    if(person.EmailAddress.Name == attendee)
                    {
                        toReturn.Add(ev);
                        break;
                    }
                }
            }

            return toReturn;
        }

        public static List<int> GetBusyDaysForPerson(IEnumerable<Event> events)
        {
            // This method is really only for the Calendar View.

            List<int> busyDays = new List<int>();
            int daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            for (int i = 0; i < daysInMonth; i++)
            { // Initializing a list of the correct size with all zero (not busy)
                busyDays.Add(0);
            }

            foreach(var ev in events)
            {
                // Not bothering with events outside the current year. May need to be tweaked
                // to account for beginning/end of year event overlaps.
                // Not bothering with start dates after the current month.
                // We need start dates from this year, before this month for any long term vacations or unavilabilities that may span weeks/months
                if(DateTime.Parse(ev.Start.DateTime).Year != DateTime.Today.Year || DateTime.Parse(ev.Start.DateTime).Month > DateTime.Today.Month || DateTime.Parse(ev.End.DateTime).Month < DateTime.Today.Month)
                {
                    continue;
                }

                // Start Date Management
                int start = 0;
                if(DateTime.Parse(ev.Start.DateTime).Month < DateTime.Today.Month && DateTime.Parse(ev.Start.DateTime).Year == DateTime.Today.Year && DateTime.Parse(ev.End.DateTime).Month >= DateTime.Today.Month)
                {
                    // Our start date precedes the current month
                    start = 0;
                }
                else
                {
                    start = DateTime.Parse(ev.Start.DateTime).Day - 1;
                }

                // End Date Management
                int end = 0;
                if(DateTime.Parse(ev.End.DateTime).Month > DateTime.Today.Month && DateTime.Parse(ev.End.DateTime).Year == DateTime.Today.Year)
                { // end date is into the next month
                    end = daysInMonth;
                }
                else if (ev.IsAllDay != true)
                { // When set to all day, the end time ends up being 12:00:00AM on the following day
                    end = DateTime.Parse(ev.End.DateTime).Day;
                }
                else
                {
                    end = DateTime.Parse(ev.End.DateTime).Day - 1;
                }

                for(int i = start; i < end; i++)
                {
                    busyDays[i] = 1;
                }
            }

            return busyDays;
        }

        public static List<Event> GetBusyForSortingPerson(IEnumerable<Event> events)
        {
            List<Event> sortingDays = events.ToList();
            DateTime today = DateTime.Today;
            DateTime oneWeekForward = today.AddDays(7);
            DateTime oneWeekBackward = today.AddDays(-7);

            foreach(var ev in events)
            {
                if (DateTime.Parse(ev.End.DateTime).CompareTo(oneWeekBackward) < 0)
                {  // Events ended more than a week ago
                    sortingDays.Remove(ev);
                }    
                else if(DateTime.Parse(ev.Start.DateTime).CompareTo(oneWeekForward) > 0)
                { // Events start more than a week from now
                    sortingDays.Remove(ev);
                }
                else { // do nothing, the event is within our window of interest
                }
            }

            return sortingDays;
        }

        public static Tuple<string, int, string> GetSortingStatusForPerson(IEnumerable<Event> events)
        {
            // bootstrap styling usage
            // currently busy = 'danger'
            // soon to be busy = 'warning'
            // not busy = 'success'
            foreach (var ev in events)
            {
                if (DateTime.Parse(ev.Start.DateTime).CompareTo(DateTime.Today) <= 0 && DateTime.Parse(ev.End.DateTime).CompareTo(DateTime.Today) >= 0)
                { // We are in the middle of an event, currently busy
                    return new Tuple<string, int, string>("card-unavailable", 3, "Unavailable");
                }
            }

            // We are not currently busy

            foreach (var ev in events)
            {
                if (DateTime.Parse(ev.Start.DateTime).CompareTo(DateTime.Today) > 0)
                {
                    // We will be busy in the next seven days
                    return new Tuple<string, int, string>("card-may-be-available", 2, "May Be Available");
                }
            }

            int days = -7;
            // We will not be busy soon and are not currently busy
            foreach (var ev in events)
            {
                if (DateTime.Parse(ev.End.DateTime).CompareTo(DateTime.Today) < 0)
                {
                    days = ((DateTime.Parse(ev.End.DateTime) - DateTime.Today).Days);

                }
            }
            return new Tuple<string, int, string>("card-available", days, "Available");
        }

// End Group Calendar Methods
// ----------------------------------------------------------------------------------------------------------        

// ----------------------------------------------------------------------------------------------------------
// Methods for working with Organizer data - Non Group Calendar Methods

        private static GraphServiceClient GetAuthenticatedClient()
        {
            return new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        var idClient = ConfidentialClientApplicationBuilder.Create(appId)
                            .WithRedirectUri(redirectUri)
                            .WithClientSecret(appSecret)
                            .Build();

                        var tokenStore = new SessionTokenStore(idClient.UserTokenCache,
                            HttpContext.Current, ClaimsPrincipal.Current);

                        var accounts = await idClient.GetAccountsAsync();

                        // By calling this here, token can be refreshed
                        // if it's expired right before the graph call is made

                        var scopes = graphScopes.Split(' ');
                        var result = await idClient.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }));
        }

        public static async Task<User> GetUserDetailsAsync(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
                    }));

            return await graphClient.Me.Request().GetAsync();
        }
    }
}