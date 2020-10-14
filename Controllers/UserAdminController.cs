using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Samico.Models;

namespace Samico.Controllers
{
    public class UserAdminController : Controller
    {
        // GET: Supervisor
        [Authorize(Roles = "Administrador")]
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