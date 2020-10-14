using System;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System.Data.Entity.Migrations;
using Samico.Extensions;
using Samico.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Threading.Tasks;

namespace Samico.Hubs
{
    /// <summary>
    /// Class to handle Notification hub methods
    /// </summary>
    [HubName("SamiNotificationHub")]
    public class NotificationsHub : Hub
    {
        // Conexión a la base de datos
        private readonly SamiEntities _db = new SamiEntities();

        // Connect Agent Online

        public void ConnectUserAgent()
        {

            var scenario = 0;
            var dateAndTime = DateTime.Now;
            var date = dateAndTime.Day + "-" + dateAndTime.Month + "-" + dateAndTime.Year;

            var dateDB = (from connectAgent in _db.ConexionAgentes
                          where connectAgent.IdUser == Context.User.Identity.Name
                          orderby connectAgent.id descending
                          select connectAgent);

            /*
             * @Desc: Los escenarios fueron implementados para evitar multiple código similar, estos pasan a ser un case donde se define 
             *        si el agente ya se había conectado el mismo día y no repita los registros del mismo día.
             */

            if (dateDB.Any())
            {
                var fecha = dateDB.First();
                var fecha2 = fecha.FechaConexion;
                string dateOnly = fecha2.Value.Day.ToString() + "-" + fecha2.Value.Month.ToString() + "-" + fecha2.Value.Year.ToString();

                if (dateOnly == date.ToString())
                {
                    scenario = 1;
                }
                else
                {
                    scenario = 2;
                }

            }
            else
            {
                scenario = 2;
            }

            switch (scenario)
            {
                case 2:

                    /*var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                            where aspNetUser.UserName == Context.User.Identity.Name
                                            select aspNetUser).FirstOrDefault();

                    // Lo ponemos como desconectado

                    consultarUsuario.Status = 0;
                    _db.AspNetUsers.AddOrUpdate(consultarUsuario);
                    _db.SaveChanges();*/
                    // Se almacena el inicio de sesión del agente en la base de datos.
                    var countAgentConnect = new ConexionAgente
                    {
                        IdUser = Context.User.Identity.Name,
                        FechaConexion = DateTime.Now
                    };

                    _db.ConexionAgentes.Add(countAgentConnect);
                    _db.SaveChanges();
                    break;
            }
            
        }

        /// <summary>
        /// Method to send notifications from DB to client
        /// </summary>
        public void GetNotifications()
        {
            //Get agent company ID

            var companyId = Context.User.Identity.GetCompanyId();

            //Get unattended notifications by company ID ordered from newer to older
            var notifications = (from notification in _db.AgentRequestNotifications
                                 where notification.AttendedByAgent == false
                                 && notification.CompanyId == companyId
                                 && notification.AgentName == "no ha sido atendido"
                                 orderby notification.CreationDate
                                 select notification).ToList();

            //Send notifications to agent clients
            Clients.All.getNotifications(JsonConvert.SerializeObject(notifications));
        }

        /// <summary>
        /// Method to remove notifications from clients and DB whenever an agent clicks on the notification
        /// </summary>
        /// <param name="requestId">Notification ID</param>
        public void RemoveNotification(int requestId)
        { 

            //Get notification from DB by ID
            var request = (from notification in _db.AgentRequestNotifications
                           where notification.RequestId == requestId
                           select notification).First();

            //Mark the request as attended
            request.AttendedByAgent = true;
            request.AttendedByAgentDate = DateTime.Now;
            request.AgentName = Context.User.Identity.Name;

            //Save changes
            _db.AgentRequestNotifications.AddOrUpdate(request);
            _db.SaveChanges();
        }

        /// <summary>
        /// Método que trae si el usuario está esperando.
        /// </summary>
        /// <param name="requestId"></param>
        public void UserWaiting(string requestId)
        {
            //Get agent company ID
            var companyId = Context.User.Identity.GetCompanyId();

            //Get unattended notifications by company ID ordered from newer to older
            var notifications = (from notification in _db.AgentRequestNotifications
                                 where notification.AttendedByAgent == false
                                 && notification.CompanyId == companyId
                                 && notification.AgentName == "no ha sido atendido"
                                 && notification.RequesterName == requestId
                                 orderby notification.CreationDate
                                 select notification).ToList();

            //Send notifications to agent clients
            Clients.All.getAllUsers(JsonConvert.SerializeObject(notifications));
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {               
                var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                            where aspNetUser.UserName == Context.User.Identity.Name
                                            select aspNetUser).FirstOrDefault();

                    // Lo ponemos como desconectado

                    consultarUsuario.Status = 0;
                    _db.AspNetUsers.AddOrUpdate(consultarUsuario);
                    _db.SaveChanges();

            }
            else
            {
                // Mensaje
            }
            

            return base.OnDisconnected(stopCalled);
        }        
    }
}
