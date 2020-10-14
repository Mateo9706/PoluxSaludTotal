using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Samico.BotConnectJson;
using Samico.Extensions;
using Samico.Models;

namespace Samico.Hubs
{
    /// <summary>
    /// Class to handle Notification hub methods
    /// </summary>
    [HubName("CoachHub")]
    public class SupervisorHub : Hub
    {

        private readonly SamiEntities _db = new SamiEntities();
        QnAConnect qnaConn = new QnAConnect();

        

        ///<summary>
        ///
        /// ENTRENAMIENTO DEL BOT 
        /// 
        /// </summary>

        public void SendChatMessage2(ChatViewModel chat)
        {
            var sessionId = -1;

            //Query que verifica si ya existe una sesión en la base de datos
            var queryNewSession = from session in _db.HistoricoSesions
                                  where session.SessionConnectionId == chat.ConnectionId
                                  select session;

            //If not exists
            if (!queryNewSession.Any())
            {
                //Create new session
                var newSession = new HistoricoSesion
                {
                    SessionConnectionId = chat.ConnectionId,
                    Activa = true,
                    Identificacion = Context.User.Identity.GetUserId()
                };

                //Save changes to DB
                _db.HistoricoSesions.Add(newSession);
                _db.SaveChanges();

                sessionId = newSession.IdSesion;
            }
            else
            {
                sessionId = queryNewSession.ToList()[0].IdSesion;
            };

            Clients.Group(chat.ConnectionId).addChatMessage2(chat);

            //Otherwise, send user question to bot engine
            if (!chat.AttendedByAgent)
            {
                //Get current connection URI
                var uri = new Uri(Context.Request.Url.AbsoluteUri);
                //Generate URL to post data
                var url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/Coach/GenerateAnswer";
                //Generate post form from chat data
                var postData = $"IdUserAspNetUser={Context.User.Identity.GetUserId()}&IdSesionSaved={0}&ConnectionId={chat.ConnectionId}&Name={chat.Name}&Group={chat.Group}&Message={chat.Message}&CompanyId={Context.User.Identity.GetCompanyId()}&OS={chat.OS}&UriBaseQnA={Context.User.Identity.GetUriBaseQnA()}&SpecificKnowledgebaseId={Context.User.Identity.GetSpecificKnowledgebaseId()}&SpecificSubscriptionKey={Context.User.Identity.GetSpecificSubscriptionKey()}&EndPointApiLuis={Context.User.Identity.GetEndpointApiFromLuis()}&AuthoringKeyApiLuis={Context.User.Identity.GetAuthoringKeyApiFromLuis()}&UriBaseLuisApi={Context.User.Identity.GetUriBaseApiLuis()}&KeySpellCheckApiLuis={Context.User.Identity.GetKeySpellCheckLuis()}";
                //Get byte array from post string
                var postBytes = Encoding.UTF8.GetBytes(postData);
                //Create a POST WebRequest object
                var postRequest = (HttpWebRequest)WebRequest.Create(url);
                postRequest.Method = "POST";
                postRequest.ContentType = "application/x-www-form-urlencoded";
                postRequest.ContentLength = postBytes.Length;
                Stream newStream = postRequest.GetRequestStream();
                // Send the data.
                newStream.Write(postBytes, 0, postBytes.Length);
            }
        }

        /// <summary>
        /// Method to as the answer not necessary qualification
        /// </summary>
        public void NextQuery()
        {
            //Craft a chat model object
            var chat = new ChatViewModel
            {
                Name = "Pólux",
                Message = "en que puedo ser util",
                ProfilePictureLocation = "bot-avatar.png"
            };

            //Send welcome message to user
            Clients.Group(Context.ConnectionId).addChatMessage(chat);
        }

        /// <summary>
        /// Method to handle dynamic group joining
        /// </summary>
        /// <param name="group">Group name</param>
        public void JoinGroup(string group)
        {
            Groups.Add(Context.ConnectionId, group);
        }

        /// <summary>
        /// Method to as the answer not necessary qualification
        /// </summary>
        public void NextQuery2(ChatViewModel chat2)
        {
            var db = new SamiEntities();
            var claseHtml = 0;

            //Get notification from DB by ID
            var request = (from notification in db.AgentRequestNotifications
                           where notification.RequesterGroup == chat2.Group
                           && notification.AttendedByAgent == false
                           && notification.AttendedByAgentDate == null
                           select notification);
            if (request.Any())
            {
                var date = request.First();


                //Mark the request as attended
                date.AttendedByAgent = true;
                date.AttendedByAgentDate = DateTime.Now;

                //Save changes
                db.AgentRequestNotifications.AddOrUpdate(date);
                db.SaveChanges();

                if (chat2.Group.Count() > 1)
                {
                    claseHtml++;
                }

                //Craft a chat model object
                chat2.Message = $"<div id='validatortimeOutAgent'><span>Nuestros Agentes se encuentran ocupados, ¿deseas seguir esperando? o me autorizas para cancelar la petición y enviar el caso al correo de los agentes humanos</span><div class='form-inline'>" +
                                    "<button type='button' class='btn btn-sami-success mb-2 mr-sm-2 mb-sm-0 QueueForAgentChatAgain'><i class='fa fa-check'></i> Deseo Esperar</button>" +
                                           "<button type='button' class='btn btn-sami-success mb-2 mr-sm-2 mb-sm-0 CancelRequest'><i class='fa fa-close'></i> Deseo cancelar</button>" +
                                        "</div>" +
                                    "</div>";
                chat2.Name = "S@mi";
                chat2.ProfilePictureLocation = "bot-avatar.png";
                //Send message letting user know a agent has attended his request and connected to chat
                Clients.Client(chat2.ConnectionId).addChatMessage(chat2);
            }
        }
        public void Welcome2()
        {

            var consultaContextos = (from contextos in _db.Contextos where contextos.IdUser == Context.User.Identity.Name select contextos);

            if (consultaContextos.Any())
            {
                _db.Contextos.Remove(consultaContextos.First());
                _db.SaveChanges();
            }

            //Craft a chat model object
            var chat = new ChatViewModel
            {
                Name = "S@mi",
                Message = "Hola " + Context.User.Identity.Name + " puedes usarme para validar la pregunta simplificada por LUIS",
                ProfilePictureLocation = "bot-avatar.png"
            };

            //Send welcome message to user
            Clients.Group(Context.ConnectionId).addChatMessage2(chat);
        }

        /// <summary>
        /// Override method to add a group to hub groups/users collection when a user connects to chat
        /// </summary>
        /// <returns></returns>
        public override Task OnConnected()
        {
            //Add group to hub
            Groups.Add(Context.ConnectionId, Context.ConnectionId);

            return base.OnConnected();
        }

        /// <summary>
        /// Method to let users know when a user on the other end has disconnected from chat
        /// Either from an intentional disconnect or a timeout, internet problem, etc.
        /// </summary>
        /// <param name="stopCalled">Flag to control if a user has intentionally disconnected</param>
        public override Task OnDisconnected(bool stopCalled)
        {
            //Generate message depending on stopCalled value
            var msg = stopCalled ? $"El usuario <strong>{Context.User.Identity.Name}</strong> se ha desconectado" : $"El usuario <strong>{Context.User.Identity.Name}</strong> ha agotado el tiempo de espera y se ha desconectado automáticamente";

            //Craft a chat model object
            var chat = new ChatViewModel
            {
                Name = "S@mi",
                Message = msg,
                ProfilePictureLocation = "bot-avatar.png",
                ConnectionId = Context.ConnectionId
            };

            //Send message to user
            Clients.Group(Context.ConnectionId).addChatMessage(chat);

            return base.OnDisconnected(stopCalled);
        }
    }
}