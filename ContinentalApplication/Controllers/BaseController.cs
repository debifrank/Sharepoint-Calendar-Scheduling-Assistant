using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using System.Web;

using Microsoft.Owin.Security.Cookies;

using ContinentalApplication.Models;
using ContinentalApplication.TokenStorage;


namespace ContinentalApplication.Controllers

{
    public abstract class BaseController : Controller
    {
        protected void Flash(string message, string debug=null)
        {
            var alerts = TempData.ContainsKey(Alert.AlertKey) ?
                (List<Alert>)TempData[Alert.AlertKey] :
                new List<Alert>();

            alerts.Add(new Alert
            {
                Message = message,
                Debug = debug
            });

            TempData[Alert.AlertKey] = alerts;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Request.IsAuthenticated)
            {
                //Get user's token cache
                var tokenStore = new SessionTokenStore(null,
                    System.Web.HttpContext.Current, ClaimsPrincipal.Current);

                if (tokenStore.HasData())
                {
                    // Add user to the view bag
                    ViewBag.User = tokenStore.GetUserDetails();
                }
                else
                {
                    // Session has lost data. Happens during debugging
                    // Log out so user can sign back in
                    Request.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                    filterContext.Result = RedirectToAction("Index", "Home");
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}