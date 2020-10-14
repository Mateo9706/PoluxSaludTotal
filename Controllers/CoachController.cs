using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using RazorEngine;
using RazorEngine.Templating;
using Samico.BotConnectJson;
using Samico.Extensions;
using Samico.Models;
using Samico.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Samico.Controllers
{
    public class CoachController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };
        private readonly string[] _allowedExtensionsDocument = { ".doc", ".docx", ".pdf", ".xls", ".xlsx" };


        [Authorize(Roles = "Entrenador")]
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

        [HttpGet]
        public async Task<ActionResult> GetData(string Preguntas)
        {
            var qnaModels = new QnAListEdit();
            var onlyQuery = "";

            var an = "";
            dynamic json = JsonConvert.DeserializeObject(Preguntas);

            foreach (var item in json)
            {
                an = item;
            }

            int valueMetadata = 0;
            int company = User.Identity.GetCompanyId();
            string kb = User.Identity.GetSpecificKnowledgebaseId();
            string key = User.Identity.GetSpecificSubscriptionKey();
            string uri = User.Identity.GetUriBaseQnA();

            string questionJSON = @"{'question': '" + an + "'}";

            var response = await qnaConn.GetData(questionJSON,kb, key, uri);

            var acceptableAnswers = from answerFinal in response.ToList()
                                    where answerFinal.Score == 100
                                    orderby answerFinal.Score descending
                                    select answerFinal;


            foreach (var list in acceptableAnswers.ToList())
            {
                var qnAModel = new QnaQuestionModel
                {
                    IdQna = list.Id,
                    Preguntas = new List<string>()
                };

                foreach (var metadata in list.Metadata)
                {
                    string stringMeta = metadata.ToString().Replace("\r\n", string.Empty);

                    if (stringMeta == "{  \"name\": \"feedback\",  \"value\": \"true\"}")
                        valueMetadata = 1;
                    else if (stringMeta == "{  \"name\": \"context\",  \"value\": \"ticketca\"}")
                        valueMetadata = 2;
                    else if (stringMeta == "{  \"name\": \"autoagent\",  \"value\": \"true\"}")
                        valueMetadata = 3;
                    else
                        valueMetadata = 0;

                    qnAModel.MetadataSelect = valueMetadata;
                }

                foreach (var question in list.Questions)
                {
                    qnAModel.Preguntas.Add($"<input type='text' id='PreguntasE' name='PreguntasE[]' class='form-control' placeholder='Intención-Entidad' value='{question}'/>");
                }

                qnAModel.Respuesta = list.AnswerText;

                qnaModels.QnaModelListEdit.Add(qnAModel);

            }

            return View(qnaModels);
        }

        // POST: /UserAdmin/Register
        /// <summary>
        /// Manages user registration form POST request
        /// </summary>
        /// <param name="file">Posted profile picture (if any)</param>
        /// <returns>User registration view with process' result message</returns>
        [HttpPost]
        [Authorize(Roles = "Supervisor")]
        public JsonResult UploadFile()
        {
            string urlData = "";
            //Get current connection URI
            var uri = new Uri(Request.Url.AbsoluteUri);
            //Generate URL to post data
            var url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/";

            int company = User.Identity.GetCompanyId();

            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i]; //Uploaded file
                                                            //Use the following properties to get file's name, size and MIMEType
                int fileSize = file.ContentLength;
                string fileName = file.FileName;
                string mimeType = file.ContentType;
                System.IO.Stream fileContent = file.InputStream;
                //Grab extension to check if it's allowed (to avoid security issues / invalid file types)
                var extension = Path.GetExtension(file.FileName)?.ToLower();

                //If file is allowed
                if (_allowedExtensions.Contains(extension))
                {
                    //Generate path where file is going to be stored
                    var path = Path.Combine(Server.MapPath("~/Content/Archivos/Imagenes"),
                        fileName);
                    //Save file
                    file.SaveAs(path);
                    urlData = url + "Content/Archivos/Imagenes/" + file.FileName;
                }
                //Else, let user know file is not allowed
                else if (_allowedExtensionsDocument.Contains(extension))
                {
                    //Generate path where file is going to be stored
                    var path = Path.Combine(Server.MapPath("~/Content/Archivos/Documentos"),
                        fileName);
                    //Save file
                    file.SaveAs(path);
                    urlData = url + "Content/Archivos/Documentos/" + file.FileName;
                }
                else
                {

                }

                var saveFileDataBaseLink = new ControlArchivo
                {
                    IdCompany = company,
                    IdUser = User.Identity.GetUserId(),
                    FileUpload = urlData,
                    FechaPublicacion = DateTime.Now,
                    FileName = file.FileName
                };

                _db.ControlArchivos.Add(saveFileDataBaseLink);
                _db.SaveChanges();
            }
            return Json("Uploaded " + Request.Files.Count + " files");
        }

        private string rating = null;
        private readonly SamiEntities _db = new SamiEntities();
        QnAConnect qnaConn = new QnAConnect();

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

        public async Task<JsonResult> EditQuestionAndAnswer(QnaQuestionModel question)
        {
            string kb = User.Identity.GetSpecificKnowledgebaseId();
            string key = User.Identity.GetSpecificSubscriptionKeyAzure();
            string uri = User.Identity.GetUriBaseQnA();
            List<string> listQuery = new List<string>();
            int company = User.Identity.GetCompanyId();
            string MetadataSelect = "";

            foreach (var datos in question.PreguntasE)
            {
                listQuery.Add("'" + datos.ToString() + "'");
            }

            if (question.MetadataSelectE == 1)
                MetadataSelect = "{'name': 'feedback', 'value': 'true'}";
            else if (question.MetadataSelectE == 2)
                MetadataSelect = "{'name': 'context', 'value': 'ticketca'}";
            else if (question.MetadataSelectE == 3)
                MetadataSelect = "{'name': 'autoagent', 'value': 'true'}";
            else
                MetadataSelect = "";

            string addListQuery = string.Join(",", listQuery.ToArray());

            string finalAnswer = @"{'add': {'qnaList': [{'id': 0,'answer': '" + question.RespuestaE + "','source': ' QnADataBaseCompany#" + company + "','questions': [" + addListQuery + "],'metadata': [" + MetadataSelect + "]}]},'delete': {'ids': [" + question.IdQna + "]}}";

            var resultado = await qnaConn.QnaAnswer(finalAnswer, 1, kb, key, uri);

            return Json(new { success = true, message = "Se ha editado correctamente" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize(Roles = "Supervisor")]
        public async Task<JsonResult> DeleteQnA(int id)
        {
            string kb = User.Identity.GetSpecificKnowledgebaseId();
            string key = User.Identity.GetSpecificSubscriptionKeyAzure();
            string new_kb = @"{'delete': {'ids': [" + id + "]}}";

            await qnaConn.QnaAnswer(new_kb, 1, kb, key, "");

            return Json(new { success = true, message = "Se ha eliminado correctamente" }, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public string GetListFile()
        {
            SamiEntities db = new SamiEntities();

            int company = User.Identity.GetCompanyId();

            var filesAdd = new GetListFiles();

            var jsonConvert = "";

            var getFiles = (from files in db.ControlArchivos where files.IdCompany == company select files);

            if (getFiles.Any())
            {
                foreach (var files in getFiles.ToList())
                {
                    var extension = files.FileName.Split('.');

                    var getList = new getFileList
                    {
                        name = files.FileName,
                        url = files.FileUpload,
                        extension = extension[1]
                    };

                    filesAdd.GetListFilesAdd.Add(getList);
                }

                jsonConvert = JsonConvert.SerializeObject(filesAdd.GetListFilesAdd.ToList());
            }
            else
            {
                jsonConvert = "error";
            }

            

            return jsonConvert;
        }

        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<string> GetApiQnAList()
        {
            string kb = User.Identity.GetSpecificKnowledgebaseId();
            string key = User.Identity.GetSpecificSubscriptionKeyAzure();
            string uri = User.Identity.GetUriBaseQnA();

            var qnaModels = new QnAListCreate();

            var onlyQuery = "";
            var metadataString = "";

            var response = await qnaConn.QnaAnswer("", 2, kb, key, uri);

            RootObject datalist = JsonConvert.DeserializeObject<RootObject>(response);

            qnaModels.QnaModelLists.Clear();
            

            foreach (var list in datalist.qnaDocuments)
            {
               
                var qnAModel = new QnaQuestionModel
                {
                    IdQna = list.id,
                    Preguntas = new List<string>()
                };



                foreach (var question in list.questions)
                {
                    onlyQuery = question;
                    qnAModel.Preguntas.Add(question);
                }

                foreach (var metadata in list.metadata)
                {
                    metadataString = metadata.name + ":" + metadata.value;
                }

                qnAModel.Pregunta = onlyQuery;

                qnAModel.Respuesta = list.answer;

                qnAModel.Metadata = metadataString;

                qnaModels.QnaModelLists.Add(qnAModel);

            }

            var jsonConvert = JsonConvert.SerializeObject(qnaModels.QnaModelLists.ToList());

            return jsonConvert;

        }


        [HttpPost]
        [Authorize(Roles = "Supervisor")]
        public async Task<JsonResult> SendQuestionAndAnswer(QnaQuestionModel question)
        {
            string kb = User.Identity.GetSpecificKnowledgebaseId();
            string key = User.Identity.GetSpecificSubscriptionKeyAzure();
            string uri = User.Identity.GetUriBaseQnA();
            List<string> listQuery = new List<string>();
            int company = User.Identity.GetCompanyId();
            string MetadataSelect = "";

            foreach (var datos in question.Preguntas)
            {
                listQuery.Add("'" + datos.ToString() + "'");
            }

            if (question.MetadataSelect == 1)
                MetadataSelect = "{'name': 'feedback', 'value': 'true'}";
            else if (question.MetadataSelect == 2)
                MetadataSelect = "{'name': 'context', 'value': 'ticketca'}";
            else if (question.MetadataSelect == 3)
                MetadataSelect = "{'name': 'autoagent', 'value': 'true'}";
            else
                MetadataSelect = "";

            string addListQuery = string.Join(",", listQuery.ToArray());

            string finalAnswer = @"{'add': {'qnaList': [{'id': 0,'answer': '" + question.Respuesta + "','source': ' QnADataBaseCompany#" + company + "','questions': [" + addListQuery + "],'metadata': [" + MetadataSelect + "]}]}}";

            await qnaConn.QnaAnswer(finalAnswer, 1, kb, key, uri);

            return Json(new { success = true, message = "Se ha agregado la pregunta correctamente" }, JsonRequestBehavior.AllowGet);

        }


        /// <summary>
        /// Publicar procesos
        /// </summary>
        /// <returns></returns>
        ///
        [HttpPost]
        [Authorize(Roles = "Supervisor")]
        public async Task<JsonResult> PublishQnA()
        {
            string kb = User.Identity.GetSpecificKnowledgebaseId();
            string key = User.Identity.GetSpecificSubscriptionKeyAzure();

            await qnaConn.QnaAnswer("", 3, kb, key, "");

            await PublishQnADatabase();

            return Json(new { success = true, message = "Se ha publicado correctamente" }, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Publicar procesos
        /// </summary>
        /// <returns></returns>
        ///
        public async Task PublishQnADatabase()
        {
            try
            {
                if (Request.IsAuthenticated)
                {
                    if (User.IsInRole("Supervisor"))
                    {
                        string kb = User.Identity.GetSpecificKnowledgebaseId();
                        string key = User.Identity.GetSpecificSubscriptionKeyAzure();
                        string uri2 = User.Identity.GetUriBaseQnA();

                        var response = await qnaConn.QnaAnswer("", 2, kb, key, uri2);

                        RootObject datalist = JsonConvert.DeserializeObject<RootObject>(response);

                        var saveChange = new EntrenamientoQnA
                        {
                            Count = datalist.qnaDocuments.Count(),
                            Observaciones = "Observaciones",
                            Fecha = DateTime.Now,
                            Company = User.Identity.GetCompanyId(),
                            UserId = User.Identity.GetUserId()
                        };

                        _db.EntrenamientoQnAs.Add(saveChange);
                        _db.SaveChanges();
                    }
                }
               

            }catch(Exception ex)
            {

            }
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

            var datosSamiApi = (from samiapi in _db.ApiConversacions select samiapi).First();

            var resultadosApi = await bot.GetAnswersAsync(datosSamiApi.SamiApiKey
                , datosSamiApi.SamiApiLink
                , textUser
                , model.CompanyId
                , model.IdUserAspNetUser
                , model.OS
                , model.Name);

            model.RepliedByBot = false;

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

            if (casoJson != "2" && casoJson != "5")
            {

                string[] convertido = respuestaSami.Split(new[] { "<script>" }, StringSplitOptions.None);

                var chatReportSami = SaveReportChatSami(model.IdSesionSaved, model.Name, model.IdUserAspNetUser, textUser, respuesta, respuestaLuis, model.OS, intencion, entidad, puntajeLuis, puntajeQnA, "Web");

            }


            switch (casoJson)
            {
                case "1":
                    // Escenario sin paso de QnA: ¿Que necesitas de....?, ¿Qué necesitas ...?
                    respuestaFinal = queryLuis + "<script>nextQuery();</script>";
                    rating = null;

                    break;

                case "2":
                    // Escenario respuesta del caso consultado.
                    respuestaFinal = "Tengo resultado de tu ticket, estos son los datos: <br/></script><script>nextQuery();</script>";
                    // Convertimos el archivo xml en texto
                    XElement caResult = XElement.Parse(queryLuis);
                    // Se toma solo las etiquetas <AttrValue></AttrValue>
                    var att = caResult.Descendants("AttrValue").ToList();
                    // Se crea un array de cadena de texto.
                    string[] lista = { "<br/>Estado: ", "<br/>Caso Asignado a: ", "<br/>Descripción: " };
                    // Se combina el resultado de CA con la cadena de Array
                    var answerCA = att.Zip(lista, (n, w) => new { Number = n, Word = w });
                    foreach (var nw in answerCA)
                    {
                        // Se imprime los resultados junto con la cadena de array, aquí se elimina el <AttrValue></AttrValue>
                        respuestaFinal += String.Join(" ", nw.Word.ToString() + nw.Number.Value);
                    }
                    // Se desactiva panel de calificación.
                    rating = null;

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
            }
            //Start hub connection
            await hub.Start();
            //Set chat model values
            model.Name = "Pólux";
            model.Message = respuestaFinal + $"<br/><small><b>LUIS: {respuestaLuis} <br/> PUNTAJE LUIS: {puntajeLuis} <br/> PUNTAJE QNA: {puntajeQnA}</b></small>";
            model.ProfilePictureLocation = "bot-avatar.png";
            //Send answer message
            await proxy.Invoke<ChatViewModel>("sendChatMessage2", model);
            //Send acceptable answer or switch to agent prompt
            if (rating != null)
            {
                // Tiempo de espera por longitud.
                operacionMatematica = respuestaFinal.Length * 50;
                Thread.Sleep(operacionMatematica);
                model.Message = rating;
                await proxy.Invoke<ChatViewModel>("sendChatMessage2", model);
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

                _db.ChatReportSamis.Add(saveReport);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {

            }
            return "ok";
        }
    }
}