using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Samico.Extensions;
using Samico.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Samico.Controllers
{
    public class AgentController : Controller
    {
        // Conexión a la base de datos
        private readonly SamiEntities _db = new SamiEntities();
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };

        /// <summary>
        /// Attribute to expose user related APIs which will automatically retrieve info and save changes to the UserStore.
        /// </summary>
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Agent
        /// <summary>
        /// Handle Chat form GET request
        /// </summary>
        /// <returns>Chat view</returns>
        [Authorize(Roles = "Agente")]
        public ActionResult Index()
        {
            var datos = (from user in _db.AspNetUsers
                               where user.UserName ==User.Identity.Name
                         select user).FirstOrDefault();

            
            datos.Status = 1;
            _db.AspNetUsers.AddOrUpdate(datos);
            _db.SaveChanges();
        
            
            var chat = new ChatViewModel
            {
                //Generate new connection ID
                ConnectionId = Guid.NewGuid().ToString(),
                //Set group value to current user's username
                Group = User.Identity.Name,
                //Set profile picture value from user data
                ProfilePictureLocation = User.Identity.GetProfilePictureLocation()
                //Message = User.Identity
                // Additional user details here
            };

            return View(chat);
        }


        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        /// <summary>
        /// Configuration Agent, change Photo and Password.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Agente")]
        public ActionResult Configuration()
        {
            var datosAgente = (from user in _db.AspNetUsers
                               where user.UserName == User.Identity.Name
                               select user).FirstOrDefault();

            var AgentConfiguration = new ConfigurationAgentCoachUsers
            {
                UserId = datosAgente.Id,
                PhotoUser = datosAgente.ProfilePictureLocation,
                UserName = User.Identity.Name,
                correoCorporativo = datosAgente.Email
            };

            return View(AgentConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Agente")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Configuration(ConfigurationAgentCoachUsers model, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(model.UserId);
                var token = await UserManager.GeneratePasswordResetTokenAsync(model.UserId);
                if (model.Password != null)
                {
                    var result = await UserManager.ResetPasswordAsync(model.UserId, token, model.Password);
                    if (result.Succeeded)
                    {
                        var email = await UserManager.FindByEmailAsync(model.correoCorporativo);

                        if (email == null)
                        {
                            user.Email = model.correoCorporativo;
                            //Block to try and upload user profile picture (if provided)
                            var result2 = await UserManager.UpdateAsync(user);
                        }
                    }

                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                    return RedirectToAction("Index", "Home");

                    TempData["Success"] = "Se ha modificado los datos correctamente.";
                    ViewBag.Message = "Se ha modificado los datos correctamente.";

                    AddErrors(result);
                }
                else
                {
                    var email = await UserManager.FindByEmailAsync(model.correoCorporativo);

                    if (email == null)
                    {
                        user.Email = model.correoCorporativo;
                        //Block to try and upload user profile picture (if provided)
                        var result2 = await UserManager.UpdateAsync(user);
                    }
                }
            }

            return Redirect("/Agent/Index");
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
    }
}
