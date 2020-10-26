
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using RazorEngine;
using RazorEngine.Templating;
using Samico.BotConnectJson;
using Samico.CaConexionSaludTotal;
using Samico.Controllers;
using Samico.Extensions;
using Samico.Models;
using Samico.Utilities;

namespace Samico.Hubs
{
    [HubName("SamiChatHub")]
    public class ChatHub : Hub
    {

        //CaConexionPruebas.USD_WebServiceSoapClient ca = new CaConexionPruebas.USD_WebServiceSoapClient();
        CaConexionSaludTotal.USD_WebServiceSoapClient ca = new CaConexionSaludTotal.USD_WebServiceSoapClient();
        // Conexión a la base de datos
        private readonly SamiEntities _db = new SamiEntities();
        // Variables
        //public string respuestasfinales = "";
        public List<string> respuestasfinales = new List<string>();
        List<string> description = new List<string>();
        string nullado = "";
        string[] nullado1 = new string[0];
        string[] nullado3 = new string[0];
        int login = 0;
        private string rating = null;
        public string num = "";
        List<string> ticketsNumbers = new List<string>();
        public string numHnadle = "";

        /// <summary>
        /// Return a view as HTML by compiling it using Razor Engine
        /// </summary>
        /// <returns></returns>
        private string ReturnView()
        {
            //Read a view file from project
            var template = System.IO.File.ReadAllText(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"Views\Chat\_ProceedToAgentPartial.cshtml"));

            //Return view as HTML string

            return Engine.Razor.RunCompile(template, "templateKey");
        }

        ///<summary>
        ///     HubMethod que permite enviar el mensaje entre los clientes
        /// </summary>    

        public async Task SendChatMessage(ChatViewModel chat)
        {
            var sessionId = -1;
            var texto = "";
            string[] separarBD;

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

            if (chat.Message.Contains("<script>nextQuery();</script>"))
            {
                separarBD = chat.Message.Split(new[] { "<script>nextQuery();</script>" }, StringSplitOptions.None);
                texto = separarBD[0];
            }
            else if (chat.Message.Contains("<script>AutoQueueForAgentChat();</script>"))
            {
                separarBD = chat.Message.Split(new[] { "<script>AutoQueueForAgentChat();</script>" }, StringSplitOptions.None);
                texto = separarBD[0];
            }
            else
                texto = chat.Message;

            var newMessage = new Chat
            {
                IdSesion = sessionId,
                Texto = texto,
                Fecha = DateTime.Now,
                UserID = Context.User.Identity.GetUserId()
            };

            //Save chat message to DB
            _db.Chats.Add(newMessage);
            _db.SaveChanges();

            //Call client method and deliver message to specific connection ID
            Clients.Group(chat.ConnectionId).addChatMessage(chat);

            //If bot already replied on chat, exit
            if (chat.RepliedByBot)
            {
                if (chat.AttendedByAgent)
                {
                    var casoGenerado = (from casoGenerados in _db.CasosGenerados
                                        where casoGenerados.IdConexion == chat.ConnectionId
                                        orderby casoGenerados.Id descending
                                        select casoGenerados).FirstOrDefault();

                    var messageAgent = new ChatReportAgent
                    {
                        IdSesion = sessionId,
                        ConnectionId = chat.ConnectionId,
                        Texto = texto,
                        Fecha = DateTime.Now,
                        UserID = Context.User.Identity.GetUserId(),
                        Id_Caso = casoGenerado.Numero_Caso
                    };

                    //Save chat message to DB
                    _db.ChatReportAgents.Add(messageAgent);
                    _db.SaveChanges();
                }
                return;
            }


            //Otherwise, send user question to bot engine
            if (!chat.AttendedByAgent)
            {
                var bot = new ConnectToBot();

                var respuestaFinal = "";

                var operacionMatematica = 0;

                var datosSamiApi = (from samiapi in _db.ApiConversacions orderby samiapi.IdSamiApi descending select samiapi).First();

                var resultadosApi = await bot.GetAnswersAsync(datosSamiApi.SamiApiKey
                    , datosSamiApi.SamiApiLink /*"https://localhost:44300/api/samiapiv2/"*/
                    , texto
                    , sessionId
                    , Context.User.Identity.GetUserId()
                    , chat.OS
                    , "Web");

                var casoJson = resultadosApi.TipoCaso;
                var respuesta = resultadosApi.Respuesta;
                respuestasfinales.Add(respuesta);

                switch (casoJson)
                {
                    case "1":
                        // Escenario sin paso de QnA: ¿Que necesitas de....?, ¿Qué necesitas ...?
                        respuestaFinal = respuesta + "<script>nextQuery();</script>";
                        rating = null;

                        break;

                    case "2":

                        var consultaCa = new ConsultaCA();

                        // Aquí se guarda la petición en consultar un ticket o caso.

                        string numericPhone = new string(texto.ToCharArray().Where(c => Char.IsDigit(c)).ToArray());

                        // Pasa por el método para conectarse con CA y consulta el ticket solicitado.
                        respuestaFinal = await consultaCa.GetTicketCa(numericPhone, 3) + "<script>nextQuery();</script>";
                        //respuestaFinal = await consultaCa.GetTicketCa(numericPhone, chat.CompanyId) + "<script>nextQuery();</script>";

                        break;

                    case "3":

                        //Escenario respuesta de QnA sin FeedBack

                        respuestaFinal = respuesta + "<script>nextQuery();</script>";

                        // Se desactiva el panel de calificación.
                        rating = null;
                        
                        var dbs = new SamiEntities();

                        var updateReport = (from report in dbs.ChatReportSamis
                                            where report.UserName == Context.User.Identity.Name
                                            orderby report.Id descending
                                            select report).FirstOrDefault();

                        updateReport.Fecha_Registro = DateTime.Now;
                        

                        dbs.ChatReportSamis.AddOrUpdate(updateReport);
                        dbs.SaveChanges();

                        break;

                    case "4":
                        var db = new SamiEntities();
                        string textos = "";
                        string userName = "";

                        /*var userList = (from users in db.AspNetUsers
                                        where users.UserName == userName
                                        select users.Id).First();


                        // Get Chat with IdUser

                        var idSesion = (from chat1 in db.Chats
                                        where chat1.UserID == userList
                                        orderby chat1.IdChat descending
                                        select chat1).FirstOrDefault();*/

                        respuestaFinal = respuesta;
                        textos = respuestaFinal;
                        //Escenario de QnA con FeedBack
                        var newMessages = new Chat
                        {
                            IdSesion = sessionId,
                            Texto = textos,
                            Fecha = DateTime.Now,
                            UserID = "Respuesta automática"
                        };

                        //Save chat message to DB
                        db.Chats.Add(newMessages);
                        db.SaveChanges();




                        // Activa el panel de calificación
                        rating = ReturnView();

                        break;

                    case "5":
                        // Escenario pasa al agente automático.
                        respuestaFinal = "No tengo una respuesta a tu pregunta en este momento.<script>AutoQueueForAgentChat();</script>";
                        rating = null;

                        break;

                    case "6":
                        // Escenario pasa al agente automático.
                        respuestaFinal = respuesta + "<script>AutoQueueForAgentChat();</script>";
                        rating = null;

                        break;

                    case "7":

                        respuestaFinal = respuesta + "<script>FinalizaConversacion();</script>";

                        break;

                    default:

                        // Escenario pasa al agente automático.
                        //respuestaFinal = "Disculpa, mi entrenamiento se enfoca en temas de soporte de primer nivel de la mesa de servicios. ¿Te puedo ayudar en algo más o deseas que te comunique con uno de nuestros agentes? <br/><div class='form-inline'><button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 OtherAnswer'><i class='fa fa-check'></i> Si</button>&nbsp;<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 GenerateTicket'><i class='fa fa-close'></i> No</button></div><script>nextQuery();</script>";
                        respuestaFinal = "Tengo un problema con permisos o con la llave de acceso al API, el error es el siguiente: " + respuesta;

                        rating = null;

                        break;
                }

                //Craft a chat model object
                chat.Name = "Pólux";
                chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
                chat.Message = respuestaFinal;
                Clients.Group(Context.ConnectionId).addChatMessage(chat);
                if (rating != null)
                {
                    // Tiempo de espera por longitud.
                    //operacionMatematica = respuestaFinal.Length * 50;
                    operacionMatematica = 1000;
                    Thread.Sleep(operacionMatematica);
                    chat.Message = rating;
                    Clients.Group(Context.ConnectionId).addChatMessage(chat);
                }
            }
            else
            {
                var casoGenerado = (from casoGenerados in _db.CasosGenerados
                                    where casoGenerados.IdConexion == chat.ConnectionId
                                    orderby casoGenerados.Id descending
                                    select casoGenerados).FirstOrDefault();

                var messageAgent = new ChatReportAgent
                {
                    IdSesion = sessionId,
                    ConnectionId = chat.ConnectionId,
                    Texto = texto,
                    Fecha = DateTime.Now,
                    UserID = Context.User.Identity.GetUserId(),
                    Id_Caso = casoGenerado.Numero_Caso
                };

                //Save chat message to DB
                _db.ChatReportAgents.Add(messageAgent);
                _db.SaveChanges();
            }
        }


        /// <summary>
        /// Handle user's Switch to Agent request
        /// </summary>
        /// <param name="chat">Model data from user chat</param>
        public void CreateAgentRequest(ChatViewModel chat)
        {
            var resolved = false;

            //Get user Company ID
            var companyId = Context.User.Identity.GetCompanyId();

            var connectionCA = (from connCa in _db.ConexionCAs where connCa.IdCompania == companyId select connCa).First();

            //Create a new notification on DB
            var newNotification = new AgentRequestNotification
            {
                ConnectionId = chat.ConnectionId,
                RequesterName = chat.Name,
                RequesterGroup = chat.ConnectionId,
                CreationDate = DateTime.Now,
                CompanyId = Context.User.Identity.GetCompanyId(),
                AttendedByAgent = false,
                AgentName = "no ha sido atendido"
            };

            //Save to DB
            _db.AgentRequestNotifications.Add(newNotification);
            _db.SaveChanges();

            // Create Ticket CA
            string casoID = "";
            var ticket = CreateTicketCA(chat.Name, connectionCA.UsuarioCA, connectionCA.PasswordCA, connectionCA.Category, connectionCA.Group_ServiceDesk, connectionCA.Urgency);

            string[] separar;
            string descripcion = "";
            //separar = ticket.Split('-');
            //casoID = separar[1];
            casoID = ticket[0];
            //descripcion = separar[2];

            // Se actualiza el estado a Resuelto
            UpdateTicket(ticket[1], connectionCA.UsuarioCA, connectionCA.PasswordCA);
            validarRespuestaCorrecta(chat.Name, 1);

            if (chat.TypeBoolResolved == 1)
                resolved = true;

            UpdateSamiChatReport(chat.Name, true, true, casoID, "Abierto");
            ///UpdateSamiChatReport(chat.Name, resolved, false, "Sin Crear Casos", "Abierto");

            //Get unattended notifications by company ID ordered from newer to older
            var notifications = (from notification in _db.AgentRequestNotifications
                                 where notification.AttendedByAgent == false && notification.CompanyId == companyId
                                 orderby notification.CreationDate
                                 select notification);

            //Get a Notifications hub object
            var notifHub = GlobalHost.ConnectionManager.GetHubContext<NotificationsHub>();

            //Send / refresh notifications on connected agents
            notifHub.Clients.All.getNotifications(JsonConvert.SerializeObject(notifications.ToList()));

            var userId = Context.User.Identity.GetUserId();

            var caseManager = new CasosGenerado
            {
                Numero_Caso = casoID,
                Usuario = chat.Name,
                IdConexion = chat.ConnectionId,
                Descripcion = descripcion,
                Estado = "Abierto",
                Plataforma = "Chat"
            };

            _db.CasosGenerados.Add(caseManager);
            _db.SaveChanges();

            //Craft a chat model object
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
            chat.Message = $"Por favor espere a que un agente se conecte para darle soporte.";

            //Send message letting user know his request has been queued
            Clients.Group(Context.ConnectionId).addChatMessage(chat);

        }

        /// <summary>
        /// Handle user's Switch to Agent request
        /// </summary>
        /// <param name="chat">Model data from user chat</param>
        public void CreateAgentRequestAgain(ChatViewModel chat)
        {
            var db = new SamiEntities();

            //Get user Company ID
            var companyId = Context.User.Identity.GetCompanyId();

            //Get notification from DB by ID
            var request = (from notification in db.AgentRequestNotifications
                           where notification.RequesterGroup == chat.Group
                           select notification).First();

            //Mark the request as attended
            request.AttendedByAgent = false;
            request.CreationDate = DateTime.Now;
            request.AttendedByAgentDate = null;
            request.AgentName = "no ha sido atendido";

            //Save changes
            db.AgentRequestNotifications.AddOrUpdate(request);
            db.SaveChanges();


            //Get unattended notifications by company ID ordered from newer to older
            var notifications = (from notification in db.AgentRequestNotifications
                                 where notification.AttendedByAgent == false && notification.CompanyId == companyId
                                 orderby notification.CreationDate
                                 select notification);

            //Get a Notifications hub object
            var notifHub = GlobalHost.ConnectionManager.GetHubContext<NotificationsHub>();

            //Send / refresh notifications on connected agents
            notifHub.Clients.All.getNotifications(JsonConvert.SerializeObject(notifications.ToList()));

            //Craft a chat model object
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
            chat.Message = $"Por favor espere a que un agente se conecte para darle soporte";

            //Send message letting user know his request has been queued
            Clients.Group(Context.ConnectionId).addChatMessage(chat);

        }

        /// <summary>
        ///     Metodo para eliminar la notificación al agente.
        /// </summary>
        /// <param name="chat"></param>
        public void DeleteRequestAgent(ChatViewModel chat)
        {
            var db = new SamiEntities();

            //Get user Company ID
            var companyId = Context.User.Identity.GetCompanyId();

            //Get notification from DB by ID
            var request = (from notification in db.AgentRequestNotifications
                           where notification.RequesterGroup == chat.Group
                           select notification).First();

            //Mark the request as attended
            request.AttendedByAgent = true;
            request.AttendedByAgentDate = DateTime.Now;

            //Save changes
            db.AgentRequestNotifications.AddOrUpdate(request);
            db.SaveChanges();

            //Get unattended notifications by company ID ordered from newer to older
            var notifications = (from notification in db.AgentRequestNotifications
                                 where notification.AttendedByAgent == false && notification.CompanyId == companyId
                                 orderby notification.CreationDate
                                 select notification);

            //Get a Notifications hub object
            var notifHub = GlobalHost.ConnectionManager.GetHubContext<NotificationsHub>();

            //Send / refresh notifications on connected agents
            notifHub.Clients.All.getNotifications(JsonConvert.SerializeObject(notifications.ToList()));

            var casoGenerado = (from casoGenerados in db.CasosGenerados
                                where casoGenerados.Usuario == chat.Name
                                orderby casoGenerados.Id descending
                                select casoGenerados).FirstOrDefault();

            EnvioCorreo(chat.Name, casoGenerado.Numero_Caso);

            //Craft a chat model object
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
            chat.Message = $"Estimado usuario, lo invitamos a registrar su caso a través de la herramienta de autogestión ubicada en la intranet de la compañía. En caso de desconocer este proceso, por favor comuníquese con la mesa de servicio al teléfono en Bogotá: <br/> 6381893 o canal directo: 3888. </br> ¿Le puedo ayudar en algo más? <script>nextQuery();</script>";

            //Send message letting user know his request has been queued
            Clients.Group(Context.ConnectionId).addChatMessage(chat);

        }

        /// <summary>
        /// Handle user's Switch to Agent request
        /// </summary>
        /// <param name="nameUser"></param>
        /// <param name="typeFunction"></param>
        public void CloudAfterAnswerSami(string nameUser, string typeFunction, string idConexion)
        {
            SamiEntities db = new SamiEntities();

            var companyId = Context.User.Identity.GetCompanyId();

            // Get Data Connection CA

            var connectionCA = (from connCa in _db.ConexionCAs where connCa.IdCompania == companyId select connCa).First();

            // Variables
            var answers = "";
            //var ticket = "";
            string[] separar;
            string casoID = "";
            string descripcion = "";
            string estado = "";

            switch (typeFunction)
            {
                // Caso donde se envia correo al agente, si el usuario no desea pasar con el agente desde SAMI.
                case "NegativeAnswer":

                    var ticket = CreateTicketCA(nameUser, connectionCA.UsuarioCA, connectionCA.PasswordCA, connectionCA.Category, connectionCA.Group, connectionCA.Urgency);
                    //separar = ticket.Split('-');
                    casoID = ticket[0];
                    //casoID = separar[1];
                    estado = "Progreso";
                    //descripcion = separar[2];
                    // Se actualiza el estado a Resuelto
                    UpdateTicket(ticket[1], connectionCA.UsuarioCA, connectionCA.PasswordCA);
                    validarRespuestaCorrecta(nameUser, 1);
                    UpdateSamiChatReport(nameUser, true, true, casoID, estado);
                    answers = $"Fue un gusto ayudarle, el número del caso es: {casoID} y su estado es Pendiente por Atencion. </br> ¿Le puedo ayudar en algo más? <script>nextQuery();</script>";
                    //Registrar casos service manager

                    var caseManager = new CasosGenerado
                    {
                        Numero_Caso = casoID,
                        Usuario = nameUser,
                        IdConexion = idConexion,
                        Descripcion = "",//descripcion
                        Estado = estado,
                        Plataforma = "Chat"
                    };


                    db.CasosGenerados.Add(caseManager);
                    db.SaveChanges();

                    /*
                    answers = $"Estimado usuario, lo invitamos a registrar su caso a través de la herramienta de autogestión ubicada en la intranet de la compañía. En caso de desconocer este proceso, por favor comuníquese con la mesa de servicio al teléfono en Bogotá: <br/> 6381893 o canal directo: 3888. </br> ¿Le puedo ayudar en algo más? <script>nextQuery();</script>";

                    var caseManager = new CasosGenerado
                    {
                        Numero_Caso = casoID,
                        Usuario = nameUser,
                        IdConexion = idConexion,
                        Descripcion = descripcion,
                        Estado = estado,
                        Plataforma = "Chat"
                    };

                    db.CasosGenerados.Add(caseManager);
                    db.SaveChanges();

                    UpdateSamiChatReport(nameUser, true, false, casoID, estado);

                    // Correo

                    EnvioCorreo(nameUser, casoID);
                    */
                    break;

                // Caso donde valida si el usuario desea o no pasar al agente.
                case "CreateNewTicket":

                    validarRespuestaCorrecta(nameUser, 2);

                    answers = "<div><span>¿Desea pasar con un agente para que le solucione el caso?</span><div class='form-inline'>" +
                                "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 QueueForAgentChat'><i class='fa fa-check'></i> Si</button>" +
                                       "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 NegativeAnswer'><i class='fa fa-close'></i> No</button>" +
                                    "</div>" +
                                "</div>";

                    break;
                // Caso cuando se resuelve el caso.
                case "OtherQuery":
                    // Se crea el caso
                    
                    ticket = CreateTicketCA(nameUser, connectionCA.UsuarioCA, connectionCA.PasswordCA, connectionCA.Category, connectionCA.Group, connectionCA.Urgency);
                    //separar = ticket.Split('-');
                    casoID = ticket[0];
                    //casoID = separar[1];
                    estado = "Resuelto";
                    //descripcion = separar[2];
                    // Se actualiza el estado a Resuelto
                    UpdateTicket2(ticket[1], connectionCA.UsuarioCA, connectionCA.PasswordCA);
                    validarRespuestaCorrecta(nameUser, 1);
                    UpdateSamiChatReport(nameUser, true, true, casoID, estado);
                    answers = $"Fue un gusto ayudarle, el número del caso es: {casoID} y su estado es Resuelto. </br> ¿Le puedo ayudar en algo más? <script>nextQuery();</script>";
                    //Registrar casos service manager

                    caseManager = new CasosGenerado
                    {
                        Numero_Caso = casoID,
                        Usuario = nameUser,
                        IdConexion = idConexion,
                        Descripcion = descripcion,
                        Estado = estado,
                        Plataforma = "Chat"
                    };


                    db.CasosGenerados.Add(caseManager);
                    db.SaveChanges();
                    

                    //answers = $"Ha sido un gusto atenderle, </br> ¿Le puedo ayudar en algo más? <script>nextQuery();</script>";
                    break;
            }

            var chat = new ChatViewModel
            {
                Name = "Pólux",
                Message = answers,
                ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png"
            };

            Clients.Group(Context.ConnectionId).addChatMessage(chat);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="category"></param>
        /// <param name="customer"></param>
        /// <param name="requested_by"></param>
        /// <param name="group"></param>
        /// <param name="urgency"></param>
        /// <param name="summary"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public List<string> CreateTicketCA(string userName, string user, string pass, string category, string group, string urgency)
        {

            // Datos para eliminar

            //user = "CAIntegracionSAMI";
            //pass = "CAIntegracionSAMI2019.";

            var cr = "cr";
            var summary = "Hola soy Pólux, he generado el siguiente caso del usuario: " + userName + " en nuestra ultima conversación.";
            string[] datos = { "persistent_id" };
            var cntUser = "";
            var contextLuis = false;
            var listadoDescripcion = new List<string>();
            StringBuilder descriptions = new StringBuilder();
            listadoDescripcion.Clear();
            descriptions.Clear();

            // Get Id User

            var userList = (from users in _db.AspNetUsers where users.UserName == userName select users).First();

            // Get Chat with IdUser

            var idSesion = (from chat1 in _db.Chats where chat1.UserID == userList.Id orderby chat1.IdChat descending select chat1).FirstOrDefault();

            // Get Text Session User.
            var chatUser = (from chat2 in _db.Chats where chat2.IdSesion == idSesion.IdSesion orderby chat2.IdChat descending select chat2).Take(2);

            foreach (var item in chatUser)
            {
                if (item.Texto.Contains("<span>¿Te ha sido útil mi respuesta?</span>"))
                {

                }
                else
                {
                    // Eliminar el autoagent <scripts>
                    listadoDescripcion.Add(" • " + item.Texto + "\n\n");
                }


            }


            listadoDescripcion.Reverse();

            foreach (var item2 in listadoDescripcion)
            {
                descriptions.Append(item2);
            }

            // Se logea a CA
            login = ca.login(user, pass);

            // Consultamos el nombre de red del usuario

            var queryCnt = ca.doSelectAsync(login, "cnt", $"userid='{userList.IdentificationUser}'", 1, datos);

            // Tomamos el valor devuelto

            string e = queryCnt.Result.doSelectReturn;

            XElement caResult = XElement.Parse(e);
            // Se toma solo las etiquetas <AttrValue></AttrValue>
            var att = caResult.Descendants("AttrValue").ToList();

            foreach (var nw in att)
            {
                // Se imprime los resultados junto con la cadena de array, aquí se elimina el <AttrValue></AttrValue>
                cntUser = nw.Value;
            }

            // Parámetros predeterminados
            /*string[] attVal = { "category", "pcat:404755",
                "customer", "cnt:E59543D7E17B8F4DA614DA28F7EDB299",
                "requested_by", "cnt:E59543D7E17B8F4DA614DA28F7EDB299",
                "group", "cnt:8E6FA80B87E8054CA6ED87226B9E6522",
                "assignee", "cnt:758D44770369C743A00720CFECB63723",
                "priority", "pri:500",
                "summary", summary,
                "z_Medio_ingreso", "400006",
                "description", summary + "\n\n" + "INICIO DE LA CONVERSACIÓN" + "\n\n" + descriptions.ToString() + "\n\n" + "FIN DE LA CONVERSACIÓN"};*/

            string[] attVal = { "category", "pcat:401979",
                "customer", "cnt:D3903AE7DC22EF4099B826063CA9AAAC",
                "requested_by", "cnt:D3903AE7DC22EF4099B826063CA9AAAC",
                "group", "cnt:49DD665BE9D68C49A02A84AA8FA42AFE",
                "assignee", "cnt:2F1B4F435D92C34B8059B094679931A1",
                "priority", "pri:502",
                "summary", summary,
                "z_medio_ingreso", "400004",


                "description", summary + "\n\n" + "INICIO DE LA CONVERSACIÓN" + "\n\n" + descriptions.ToString() + "\n\n" + "FIN DE LA CONVERSACIÓN"};


            //cnt:E59543D7E17B8F4DA614DA28F7EDB299
            // Método para crear un caso.
            string create = ca.createRequest(login, "cnt:D3903AE7DC22EF4099B826063CA9AAAC", attVal, nullado1, "", nullado3, ref cr, ref nullado);

            XElement caCr = XElement.Parse(create);
            var rft = caCr.Descendants("AttrName").ToList();
            var refNum = caCr.Descendants("AttrValue").ToList();

            foreach (var item in rft)
            {
                var cns = item.Value;
                if (cns == "ref_num")
                {
                    var nt = refNum[114].Value;
                    ticketsNumbers.Add(nt);
                }
            }

            foreach (var nw in refNum)
            {
                var cns = nw.Value;
                /*if (cns.Contains("RQ"))
                {
                    num = cns;
                    ticketsNumbers.Add(num);
                }*/
                if (cns.Contains("cr:"))
                {
                    numHnadle = cns;
                    ticketsNumbers.Add(numHnadle);
                }


            }
           

            // Cierra Sesión
            ca.logout(login);

            return ticketsNumbers;
            //return create + "-" + descriptions.ToString();


            //return "prueba-prueba-" + description.ToString();
        }

        /// <summary>
        /// Update Ticket CA
        /// </summary>
        /// <param name="ticketCr"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        public void UpdateTicket(string ticketCr, string user, string pass)
        {


            string[] attrValsUpdateProgress = { "status", "WIP" };
            //string[] attrValsUpdateResolved = { "status", "RE", "resolution_code", "resocode:400009", "zcompany", "F8D9CBDF7FF2D840A4F0F59CF04C0596" };
            string[] attrValsUpdateResolved = { "status", "RE", "resolution_method", "resomethod:400001" };

            login = ca.login(user, pass);

            // Primera actualización En revisión
            var updateProgress = ca.updateObject(login, ticketCr, attrValsUpdateProgress, nullado1);

            // Segunda actualización Terminado //VERIFICAR LUEGOOO
            //var updateResolved = ca.updateObject(login, ticketCr, attrValsUpdateResolved, nullado1);

            ca.logout(login);

        }



        public void UpdateTicket2(string ticketCr, string user, string pass)
        {


            string[] attrValsUpdateProgress = { "status", "WIP" };
            //string[] attrValsUpdateResolved = { "status", "RE", "resolution_code", "resocode:400009", "zcompany", "F8D9CBDF7FF2D840A4F0F59CF04C0596" };
            string[] attrValsUpdateResolved = { "status", "RE", "resolution_method", "resomethod:400001" };

            login = ca.login(user, pass);

            // Primera actualización En revisión
            var updateProgress = ca.updateObject(login, ticketCr, attrValsUpdateProgress, nullado1);

            // Segunda actualización Terminado //VERIFICAR LUEGOOO
            var updateResolved = ca.updateObject(login, ticketCr, attrValsUpdateResolved, nullado1);

            ca.logout(login);

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
        /// Method to let user know that an agent has attended his request and connected to chat
        /// </summary>
        /// <param name="chat"></param>
        public void NotifyAgentConnection(ChatViewModel chat)
        {
            //Craft a chat model object
            chat.Message = $"El agente <strong>{chat.Name}</strong> se ha conectado al chat!";
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
            //Send message letting user know a agent has attended his request and connected to chat
            Clients.Client(chat.ConnectionId).addChatMessage(chat);
        }

        /// <summary>
        /// Method to let user know that an agent has attended his request and connected to chat
        /// </summary>
        /// <param name="chat"></param>
        public void HistoryUser(ChatViewModel chat)
        {

            /**
             * 
             * @desc: Este método trae el historial de usuario con S@MI que fue seleccionado por el agente.
             * @variables: 
             *      db (Variable conexión DB),
             *      separar (Array de Textos, utilizado para separar el texto por Split)
             *      requestId (Posición 0 de separar, me trae el Id de la sesión)
             *      nameUser (Posición 1 de separar, me trae el nombre del usuario)
             *      texto (Variable donde se almacena el texto del historial consultado)
             *      value (variable donde almacena todo el contenido del historial)
             * @retorno: Retorna Value.
             * 
            **/

          
            string[] separar;
            separar = chat.Name.Split('-');
            int requestId = Int32.Parse(separar[0]);
            string nameUser = separar[1];
            var texto = "";
            string value = "";
            var txt = "";

            // Get notification User

            var notificationAgent = (from notification in _db.AgentRequestNotifications
                                     where notification.RequesterName == nameUser
                                     select notification).First();

            // Get user ID

            var userList = (from users in _db.AspNetUsers
                            where users.UserName == notificationAgent.RequesterName
                            select users).First();

            // Get last Id Sesion with User

            var idSesion = (from chat1 in _db.Chats
                            where chat1.UserID == userList.Id
                            orderby chat1.IdChat descending
                            select chat1).FirstOrDefault();
            // Get all history session.

            var chatUser = (from chat2 in _db.Chats
                            where chat2.IdSesion == idSesion.IdSesion
                            orderby chat2.IdChat ascending
                            select chat2).ToList();
            var usuario = "";
            //var textos = respuestasfinales;
            foreach (var item in respuestasfinales)
            {
                txt = item;
            }

            value = $"<span class='post'>";

            foreach (var chatMessage in chatUser)
            {

                if (chatMessage.Texto.Contains("</div>"))
                {
                    usuario = "";
                    texto = "";
                    //txt = "";
                }
                else
                {
                    if (chatMessage.UserID == null)
                        usuario = "Pólux";
                    else
                        usuario = nameUser;
                    //texto = RespuestaFinal;
                    texto = chatMessage.Texto;
                    
                   

                    value += $"<strong>{usuario}: </strong><small>({chatMessage.Fecha})</small><br/>{texto}<br/>";
                }

            }

            value += $"</span><span class='time_date'>Historial generado por el usuario {nameUser} conversación con Pólux</span>";


            //Craft a chat model object
            chat.Message = $"He generado su historial para que el agente se entere de nuestra conversación: <br/>{value}";
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";

            //Send notifications to agent clients

            Clients.Group(chat.ConnectionId).addChatMessage(chat);

        }

        /// <summary>
        /// 
        /// 
        /// </summary>
        public void ChangeRequest(ChatViewModel chat)
        {
            var db = new SamiEntities();
            var claseHtml = 0;

            //Get notification from DB by ID
            var request = (from notification in db.AgentRequestNotifications
                           where notification.RequesterGroup == chat.Group
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

                if (chat.Group.Count() > 1)
                {
                    claseHtml++;
                }

                //Craft a chat model object
                chat.Message = $"<div id='validatortimeOutAgent' class='timeOutAgent{claseHtml}'><span>Nuestros Agentes se encuentran ocupados, ¿desea seguir esperando? o me autoriza para cancelar la petición y enviar el caso al correo de los agentes humanos</span><div class='form-inline'>" +
                                    "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 QueueForAgentChatAgain'><i class='fa fa-check'></i> Deseo Esperar</button>" +
                                           "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 CancelRequest'><i class='fa fa-close'></i> Deseo cancelar</button>" +
                                        "</div>" +
                                    "</div>";
                chat.Name = "Pólux";
                chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
                //Send message letting user know a agent has attended his request and connected to chat
                Clients.Client(chat.ConnectionId).addChatMessage(chat);
            }
        }


        /// <summary>
        /// Method to send a welcome message to a newly connected user
        /// </summary>
        public void Welcome()
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
                Name = "Pólux",
                Message = "Bienvenid@, soy Pólux ¿En qué le puedo ayudar?",
                ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png"
            };

            //Send welcome message to user
            Clients.Group(Context.ConnectionId).addChatMessage(chat);
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
                ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png"
            };

            //Send welcome message to user
            Clients.Group(Context.ConnectionId).addChatMessage(chat);
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
                chat2.Message = $"<div id='validatortimeOutAgent'><span>Nuestros Agentes se encuentran ocupados, ¿desea seguir esperando? o me autoriza para cancelar la petición y enviar el caso al correo de los agentes humanos</span><div class='form-inline'>" +
                                    "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 QueueForAgentChatAgain'><i class='fa fa-check'></i> Deseo Esperar</button>" +
                                           "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 CancelRequest'><i class='fa fa-close'></i> Deseo cancelar</button>" +
                                        "</div>" +
                                    "</div>";
                chat2.Name = "Pólux";
                chat2.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
                //Send message letting user know a agent has attended his request and connected to chat
                Clients.Client(chat2.ConnectionId).addChatMessage(chat2);
            }
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
                Name = "Pólux",
                Message = msg,
                ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png",
                ConnectionId = Context.ConnectionId
            };

            //Send message to user
            Clients.Group(Context.ConnectionId).addChatMessage(chat);

            return base.OnDisconnected(stopCalled);
        }

        ///<summary>
        ///     Metodo para cerrar la conversación de S@MI después de 25 minutos
        /// </summary>
        /// 
        public void CloseChatSami(ChatViewModel chat)
        {
            //Craft a chat model object
            chat.Message = $"<div id='validatortimeOutAgent'><span>Ha interactuado con Pólux por más de 25 minutos. ¿Desea seguir conversando o cerrar la sesión?</span><div class='form-inline'>" +
                                "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 Seguir_Conversando'><i class='fa fa-check'></i> Seguir Conversando</button>" +
                                       "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 CancelarConversacion'><i class='fa fa-close'></i> Deseo Cancelar</button>" +
                                    "</div>" +
                                "</div>";
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
            //Send message letting user know a agent has attended his request and connected to chat
            Clients.Client(chat.ConnectionId).addChatMessage(chat);
        }

        ///<summary>
        ///     Metodo para cancelar la conversación de S@MI después de 25 minutos
        /// </summary>
        /// 
        public void CancelarConversacion(ChatViewModel chat)
        {
            //Craft a chat model object
            chat.Message = $"<div id='validatortimeOutAgent'><span>Gracias por usar nuestros servicios. <br/> Estimado usuario, lo invitamos a registrar su caso a través de la herramienta de autogestión ubicada en la intranet de la compañía. En caso de desconocer este proceso, por favor comuníquese con la mesa de servicio al teléfono en Bogotá: <br/> 6381893 o canal directo: 3888.</span><div class='form-inline'>" +
                                "<button type='button' class='btn btn-sami-second mb-2 mr-sm-2 mb-sm-0 Seguir_Conversando'><i class='fa fa-reload'></i> Reiniciar Conversación</button>" +
                                    "</div>" +
                                "</div>";
            chat.Name = "Pólux";
            chat.ProfilePictureLocation = "/Images/UploadedProfilePictures/bot-avatar.png";
            //Send message letting user know a agent has attended his request and connected to chat
            Clients.Client(chat.ConnectionId).addChatMessage(chat);
        }

        ///<summary>
        ///Este método permite agregar si fue o no util la respuesta en la base de datos
        /// </summary>
        /// 

        public string validarRespuestaCorrecta(string userName, int caso)
        {
            var db = new SamiEntities();
            string texto = "";

            // Get Id User

            var userList = (from users in db.AspNetUsers
                            where users.UserName == userName
                            select users.Id).First();

            // Get Chat with IdUser

            var idSesion = (from chat1 in db.Chats
                            where chat1.UserID == userList
                            orderby chat1.IdChat descending
                            select chat1).FirstOrDefault();

            var idSesion2 = (from chat3 in db.Chats
                            where chat3.UserID == userList
                            select chat3.Texto).FirstOrDefault();

            switch (caso)
            {
                case 1:

                    texto = "La respuesta fue útil para el usuario.";

                    break;

                case 2:

                    Console.Write("hola");
                    return "bien";

                    break;
            }

            var newMessage = new Chat
            {
                IdSesion = idSesion.IdSesion,
                Texto = "fin",
                Fecha = DateTime.Now,
                UserID = "Respuesta automática"
            };

            //Save chat message to DB
            db.Chats.Add(newMessage);
            if (db.SaveChanges() == 1)
                return "bien";
            else
                return "error";
        }

        /// <summary>
        /// Actualizar la tabla de Chat de Sami
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="resuelto"></param>
        /// <param name="satisfecho"></param>
        /// <param name="noCaso"></param>
        /// <param name="estadoCaso"></param>
        public void UpdateSamiChatReport(string userName, bool resuelto, bool satisfecho, string noCaso, string estadoCaso)
        {
            var db = new SamiEntities();

            var updateReport = (from report in db.ChatReportSamis
                                where report.UserName == userName
                                orderby report.Id descending
                                select report).FirstOrDefault();

            updateReport.Fecha_Registro = DateTime.Now;
            updateReport.Resuelto = resuelto;
            updateReport.Satisfecho = satisfecho;
            updateReport.No_Caso = noCaso;
            updateReport.Estado_Caso = estadoCaso;

            db.ChatReportSamis.AddOrUpdate(updateReport);
            db.SaveChanges();

        }

        /// <summary>
        /// Este metodo permite enviar el correo.
        /// </summary>
        public void EnvioCorreo(string userName, string ticket)
        {
            var db = new SamiEntities();
            StringBuilder descriptions = new StringBuilder();

            // Get Id User

            var userList = (from users in db.AspNetUsers
                            where users.UserName == userName
                            select users).First();

            // Get Chat with IdUser

            var idSesion = (from chat1 in db.Chats
                            where chat1.UserID == userList.Id
                            orderby chat1.IdChat descending
                            select chat1).FirstOrDefault();

            // Get Text Session User.
            var chatUser = (from chat2 in db.Chats
                            where chat2.IdSesion == idSesion.IdSesion
                            && chat2.UserID == userList.Id
                            orderby chat2.IdChat descending
                            select chat2).Take(5);

            foreach (var item in chatUser)
            {
                descriptions.Append(item.Texto + " <br/>");
            }

            descriptions.Remove(descriptions.Length - 1, 1);

            SmtpClient client = new SmtpClient("smtp.office365.com", 587);
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential("superagente@axity.com", "Colombia1");
            MailAddress from = new MailAddress("superagente@axity.com", String.Empty, System.Text.Encoding.UTF8);
            MailAddress to = new MailAddress("superagente@axity.com");
            MailMessage message = new MailMessage(from, to);
            message.Body = $"Me han escrito la siguiente declaración y aún no tengo una respuesta para el usuario: {userName}<br/> Su correo es: {userList.Email} <br/> Su mensaje es: <b>{descriptions.ToString()}</b> <br/> He generado el caso con el ID: {ticket}";
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.Subject = "Hola soy Pólux, tengo un caso sin resolver ¿Me ayudas?";
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = true;

            client.Send(message);
        }
    }
}
