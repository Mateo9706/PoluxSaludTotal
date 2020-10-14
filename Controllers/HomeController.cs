using Samico.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
//using Samico.CaSamiConnect;

namespace Samico.Controllers
{
    public class HomeController : Controller
    {
        //USD_WebServiceSoapClient ca = new USD_WebServiceSoapClient();
        // Conexión a la base de datos
        private readonly SamiEntities _db = new SamiEntities();
        // Variables
        List<string> description = new List<string>();
        string nullado = "";
        string[] nullado1 = new string[0];
        string[] nullado3 = new string[0];
        int login = 0;

        public ActionResult Index()
        {
            if (Request["autoAgent"] != null)
            {
                //Check user roles
                if (User.IsInRole("Usuario"))
                    //Redirect to respective index view
                    return RedirectToAction("Index", "Chat", new
                    {
                        autoAgent = "true"
                    });
            }
            //Check user roles
            if (User.IsInRole("Usuario"))
                //Redirect to respective index view
                return RedirectToAction("Index", "Chat");

            //Check user roles
            if (User.IsInRole("Agente"))
                //Redirect to respective index view
                return RedirectToAction("Index", "Agent");

            if (User.IsInRole("Entrenador"))
                return RedirectToAction("Index", "Coach");

            if (User.IsInRole("Supervisor"))
                return RedirectToAction("Index", "Supervisor");

            if (Request["resultados"] != null && Request["email"] != null && Request["name"] != null)
            {

                // Variable de la pregunta del usuario
                string text2 = Request["resultados"];
                byte[] data2 = Convert.FromBase64String(text2);
                string questionUser = System.Text.Encoding.UTF8.GetString(data2);

                // Correo del usuario consultado en Webex Teams
                string text1 = Request["email"];
                byte[] data1 = Convert.FromBase64String(text1);
                string emailUser = System.Text.Encoding.UTF8.GetString(data1);

                string name = Request["name"];

                //

                SamiEntities db = new SamiEntities();


                // Get Data Connection CA

                // Query conexion usuarios

                var userName = (from users in _db.AspNetUsers where users.UserName == name select users).First();

                // Query conexión CA

                var connectionCA = (from connCa in _db.ConexionCAs where connCa.IdCompania == userName.CompanyId select connCa).First();

                // Query si ya fue calificado

                var qualifity = (from califica in _db.ChatReportSamis where califica.UserName == name orderby califica.Id descending select califica).First();

                // Variables
                var answers = "";
                var ticket = "";
                string[] separar;
                string casoID = "";
                string descripcion = "";
                string estado = "";

                var returnVal = "";


                if (qualifity.No_Caso != null)
                {
                    returnVal = "/Error_Page";
                }
                if (questionUser == "Si, quedé satisfecho con la calificación")
                {
                    // Se crea el caso
                    
                    //Registrar casos service manager

                    var caseManager = new CasosGenerado
                    {
                        Numero_Caso = casoID,
                        Usuario = name,
                        Descripcion = descripcion,
                        Estado = estado,
                        Plataforma = "Webex"
                    };

                    db.CasosGenerados.Add(caseManager);
                    db.SaveChanges();

                    returnVal = "/Success";

                }
                else if (questionUser == "No, deseo generar caso abierto")
                {
                    // Se crea el ticket

                    var caseManager = new CasosGenerado
                    {
                        Numero_Caso = casoID,
                        Usuario = name,
                        Descripcion = descripcion,
                        Estado = estado,
                        Plataforma = "Webex"
                    };

                    db.CasosGenerados.Add(caseManager);
                    db.SaveChanges();

                    returnVal = "/Error";
                }

                return RedirectToAction(returnVal, "BOT", new
                {
                    nombre = name,
                    idCase = casoID
                });
            }

            return View();
        }

    }
}