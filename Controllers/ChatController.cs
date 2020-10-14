﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Samico.Models;

namespace Samico.Controllers
{
    public class ChatController : Controller
    {
        // GET: Chat
        /// <summary>
        /// Handle Chat form GET request
        /// </summary>
        /// <returns>Chat view</returns>
        [Authorize(Roles = "Agente, Usuario")]
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

#pragma warning disable 1591
        public ActionResult _ProceedToAgentPartial()
        {
            // Scripts para mantener desactivado los botones.
            string script =
                "<script>" +
                "//Disable message text area and sendiing button" +
                "$('#Message').prop('disabled', false);" +
                "$('#Send').prop('disabled', false)" +
                "$('.QueueForAgentChat').prop('disabled', false);" +
                "$('.OtherAnswer').prop('disabled', false);" +
                "$('.NegativeAnswer').prop('disabled', false);" +
                "</script>";
            return View();
        }

#pragma warning restore 1591

        /// <summary>
        /// Generate chat history file from database chat history by ID
        /// </summary>
        /// <param name="id">Chat connection ID</param>
        /// <returns>Text file containing chat history</returns>
        public ActionResult GenerateHistory(string id)
        {
            var db = new SamiEntities();

            //Load messages from database
            var chatMessages = (from chat in db.Chats
                                join session in db.HistoricoSesions
                                on chat.IdSesion equals session.IdSesion
                                where session.SessionConnectionId == id
                                select chat).ToList();

            //Get user manager object from HTTP context
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            var sb = new StringBuilder();

            foreach (var chatMessage in chatMessages)
            {
                string texto = chatMessage.Texto.Replace("<script>AutoQueueForAgentChat();</script>", "");
                if (chatMessage.Fecha != null)
                    /*
                     * Generate a text line with date, user and chat text, this line uses a ternary resolution 
                     * to find out if a message was sent by a human user or was generated by the system / bot
                     */
                    sb.AppendLine(!string.IsNullOrEmpty(chatMessage.UserID)
                        ? $"[{chatMessage.Fecha.Value}] {userManager.FindById(chatMessage.UserID).UserName}: {chatMessage.Texto}"
                        : $"[{chatMessage.Fecha.Value}] Pólux: {texto}");
            }

            //Encode generated string to byte array
            var byteArray = Encoding.UTF8.GetBytes(sb.ToString());

            //Return file from memory, file will download automatically on user's browser
            return File(new MemoryStream(byteArray), "text/plain", $"ChatHistory-{DateTime.Now:yyyy-MM-dd hhmmss}.txt");
        }
    }
}