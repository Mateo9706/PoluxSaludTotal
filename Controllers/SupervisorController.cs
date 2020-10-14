using Microsoft.AspNet.Identity;
using Samico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Samico.Controllers
{
    public class SupervisorController : Controller
    {
        // GET: Supervisor
        [Authorize(Roles = "Supervisor")]
        public ActionResult Index()
        {
            var chat = new ChatViewModel
            {
                //Generate new connection ID
                ConnectionId = Guid.NewGuid().ToString(),
                //Set group value to current user's username
                Group = User.Identity.Name,
                //Message = User.Identity
                // Additional user details here
                IdUserAspNetUser = User.Identity.GetUserId()
            };

            return View(chat);
        }       
    }
}
