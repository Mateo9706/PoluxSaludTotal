using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR.Client;
using RazorEngine;
using Samico.Models;
using RazorEngine.Templating;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading;
using Samico.Utilities;
using Samico.BotConnectJson;
//using Samico.CaSamiConnect;
using System.Text;
using Samico.Extensions;
using System.Data.Entity.Migrations;

namespace Samico.Controllers
{
    public class BotController : Controller
    {
        private string rating = null;
        // Connect DataBase Entity
        private readonly SamiEntities db = new SamiEntities();

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

        /// <summary>
        /// Handle Generate Answer form GET request
        /// </summary>
        /// <returns></returns>
        public ActionResult GenerateAnswer()
        {
            return View();
        }

        public ActionResult Result(string process)
        {
            return View();
        }

        public ActionResult Success()
        {
            return View();

        }

        public ActionResult Error()
        {
            return View();
        }

        public ActionResult Error_Page()
        {
            return View();
        }

        /// <summary>
        /// Handle Generate Answer POST request
        /// This method handles the interaction between user and bot. When the user asks a quesstion, a POST 
        /// request is sent from the chat hub to this view and this controller sends the question text to the
        /// QnA engine through <see cref="ConnectToBot.GetAnswersAsync"/> method from <see cref="SamiBotInterface"/>
        /// library
        /// </summary>
        /// <param name="model">Chat model posted to view</param>
        [HttpPost]
        public async Task<ActionResult> GenerateAnswer(ChatViewModel model)
        {
            // Iteracción entre el usuario y s@mi Bot

            // Variables tipo texto
            string textUser = model.Message, queryLuis = "", respuestaFinal = "", intencion = "", respuestaSami = "", entidad = "", casoJson = "", respuestaLuis = ""
                , puntajeLuis = "", puntajeQnA = "", respuesta = "";

            // Variables tipo texto array
            string[] returnLuisFormat = null;

            // Variables tipo enteros
            int operacionMatematica = 0;

            // Instancia con las APIS
            var bot = new ConnectToBot();

            var uri = new Uri(Request.Url.AbsoluteUri);
            List<string> exchangeRate = new List<string>();

            //Generate a URL from the URI
            var url = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
            //Generate a hub client to specified url
            var hub = new HubConnection(url);

            //Generate a hub proxy to specified hub
            IHubProxy proxy = hub.CreateHubProxy("SamiChatHub");

            var datosSamiApi = (from samiapi in db.ApiConversacions select samiapi).First();

            var resultadosApi = await bot.GetAnswersAsync(datosSamiApi.SamiApiKey
                , datosSamiApi.SamiApiLink
                , textUser
                , model.CompanyId
                , model.IdUserAspNetUser
                , model.OS
                , model.Name);

            model.RepliedByBot = true;

            casoJson = resultadosApi.TipoCaso;
            respuesta = resultadosApi.Respuesta;
            respuestaLuis = resultadosApi.FormatoLuis;
            intencion = resultadosApi.Intencion;
            entidad = resultadosApi.Entidades;
            puntajeLuis = resultadosApi.PuntajeLuis;
            puntajeQnA = resultadosApi.PuntajeQnA;

            /*
             * Aquí pasa la función para guardar a la tabla de reporte.
             */

            if (casoJson != "2" && casoJson != "5" && casoJson != null)
            {

                string[] convertido = respuestaSami.Split(new[] { "<script>" }, StringSplitOptions.None);

                var chatReportSami = SaveReportChatSami(model.IdSesionSaved, model.Name, model.IdUserAspNetUser, textUser, respuesta, respuestaLuis, model.OS, intencion, entidad, puntajeLuis, puntajeQnA, "Web");

            }


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

                    string numericPhone = new string(textUser.ToCharArray().Where(c => Char.IsDigit(c)).ToArray());

                    // Pasa por el método para conectarse con CA y consulta el ticket solicitado.

                    respuestaFinal = await consultaCa.GetTicketCa(numericPhone, model.CompanyId) + "<script>nextQuery();</script>";

                    break;

                case "3":

                    //Escenario respuesta de QnA sin FeedBack

                    respuestaFinal = respuesta + "<script>nextQuery();</script>";

                    // Se desactiva el panel de calificación.
                    rating = null;

                    break;

                case "4":

                    //Escenario de QnA con FeedBack

                    respuestaFinal = respuesta;
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

                default:

                    // Escenario pasa al agente automático.
                    respuestaFinal = "No estoy entrenado para ese tipo de preguntas. ¿En qué te puedo ayudar? <script>nextQuery();</script>";
                    rating = null;

                    break;
            }
            //Start hub connection
            await hub.Start();
            //Set chat model values
            model.Name = "Pólux";
            model.Message = respuestaFinal;
            model.ProfilePictureLocation = "bot-avatar.png";
            //Send answer message
            await proxy.Invoke<ChatViewModel>("sendChatMessage", model);
            //Send acceptable answer or switch to agent prompt
            if (rating != null)
            {
                // Tiempo de espera por longitud.
                operacionMatematica = respuestaFinal.Length * 50;
                Thread.Sleep(operacionMatematica);
                model.Message = rating;
                await proxy.Invoke<ChatViewModel>("sendChatMessage", model);
            }
            //Stop hub connection
            hub.Stop();

            return View(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="sesion"></param>
        /// <param name="idUser"></param>
        /// <param name="pregunta"></param>
        /// <param name="respuesta"></param>
        /// <param name="formatoLuis"></param>
        /// <param name="so"></param>
        /// <param name="intencion"></param>
        /// <param name="entidad"></param>
        /// <param name="puntajeLuis"></param>
        /// <param name="puntajeQnA"></param>
        /// <returns></returns>
        public string SaveReportChatSami(int sesion, string userName, string idUser, string pregunta, string respuesta, string formatoLuis, string so, string intencion, string entidad, string puntajeLuis, string puntajeQnA, string plataforma)
        {
            try
            {

                var saveReport = new ChatReportSami
                {
                    UserName = userName,
                    IdUsuario = idUser,
                    IdSesion = sesion,
                    Pregunta = pregunta,
                    Respuesta = respuesta,
                    Formato_Luis = formatoLuis,
                    S_O = so,
                    Intencion = intencion,
                    Entidad = entidad,
                    Fecha_Registro = DateTime.Now,
                    Puntaje_LUIS = puntajeLuis,
                    Puntaje_QnA = puntajeQnA,
                    Plataforma = plataforma
                };

                db.ChatReportSamis.Add(saveReport);
                db.SaveChanges();
            }
            catch (Exception ex)
            {

            }
            return "ok";
        }
    }
}