using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Samico.Models;

namespace Samico.Controllers
{
    public class CompanyAdminController : Controller
    {
        private readonly SamiEntities _db = new SamiEntities();
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };

        // GET: Company
        /// <summary>
        /// Handle companies list page GET request
        /// </summary>
        /// <returns>Companies list view</returns>
        [Authorize(Roles = "Administrador")]
        public ActionResult Manage()
        { 
           return View();
        }

        [Authorize(Roles = "Administrador")]
        public ActionResult ListCompanies()
        {
            var company = from compania in _db.Companias
                          select compania;

            return View(company.ToList());
        }

        // GET: Company/Details/5
        /// <summary>
        /// Handle company info page GET request
        /// </summary>
        /// <param name="id">Company ID</param>
        /// <returns>Company info view</returns>
        [Authorize(Roles = "Administrador")]
        public ActionResult Details(int? id)
        {
            ////If request has no ID, return 503
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            //Look up company by provided ID
            var compania = _db.Companias.Find(id);

            //If company is not found, return 404
            if (compania == null)
                return HttpNotFound();

            return View(compania);
        }

        // GET: Company/Create
        /// <summary>
        /// Handle new company form GET request
        /// </summary>
        [Authorize(Roles = "Administrador")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Company/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Handle new company form POST request
        /// </summary>
        /// <param name="compania">Model from posted view</param>
        /// <param name="file">Posted company picture (if any)</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Compania compania, HttpPostedFileBase file)
        {
            //If model is valid
            if (ModelState.IsValid)
            {
                //Block to try and upload company picture (if provided)
                try
                {
                    //Checks if a file was provided
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            //Grab extension to check if it's allowed (to avoid security issues / invalid file types)
                            var extension = Path.GetExtension(file.FileName)?.ToLower();

                            //If file is allowed
                            if (_allowedExtensions.Contains(extension))
                            {
                                //Generate path where file is going to be stored
                                var path = Path.Combine(Server.MapPath("~/Images/UploadedLogos"), fileName);
                                //Save file
                                file.SaveAs(path);
                                //Set company picture to provided file name
                                compania.LogoLocation = fileName;
                                ViewBag.Message = "Archivo cargado exitosamente";
                            }
                            //Else, let user know file is not allowed
                            else
                                ViewBag.Message = "Extension de archivo no permitida";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Falló la carga del archivo {ex.Message} - {ex.StackTrace}";
                }

                //Add new company to database
                _db.Companias.Add(compania);
                _db.SaveChanges();
            }

            return Redirect("Manage");
        }

        // GET: Company/Edit/5
        /// <summary>
        /// Handle company edit form GET request
        /// </summary>
        /// <param name="id">Company ID</param>
        /// <returns>Company edit form view with company's data</returns>
        [Authorize(Roles = "Administrador")]
        public ActionResult Edit(int? id)
        {
            //If request has no ID, return 503
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            //Look up company by provided ID
            var compania = _db.Companias.Find(id);

            //If company is not found, return 404
            if (compania == null)
                return HttpNotFound();

            return View(compania);
        }

        // POST: Company/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Handle company edit form POST request
        /// </summary>
        /// <param name="compania">Model from posted view</param>
        /// <param name="file">Posted company picture (if any)</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Compania compania, HttpPostedFileBase file)
        {
            //If model is valid
            if (ModelState.IsValid)
            {
                //Block to try and upload company picture (if provided)
                try
                {
                    //Checks if a file was provided
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            //Grab extension to check if it's allowed (to avoid security issues / invalid file types)
                            var extension = Path.GetExtension(file.FileName)?.ToLower();

                            //If file is allowed
                            if (_allowedExtensions.Contains(extension))
                            {
                                //Generate path where file is going to be stored
                                var path = Path.Combine(Server.MapPath("~/Images/UploadedLogos"), fileName);
                                //Save file
                                file.SaveAs(path);
                                //Set company picture to provided file name
                                compania.LogoLocation = fileName;
                                ViewBag.Message = "Archivo cargado exitosamente";
                            }
                            //Else, let user know file is not allowed
                            else
                                ViewBag.Message = "Extension de archivo no permitida";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Falló la carga del archivo {ex.Message} - {ex.StackTrace}";
                }

                //Update database
                _db.Entry(compania).State = EntityState.Modified;
                _db.SaveChanges();
            }

            return Redirect("Manage");
        }

        // GET: Company/Delete/5
        /// <summary>
        /// handle company delete form GET request
        /// </summary>
        /// <param name="id">Company ID</param>
        /// <returns>Company info view</returns>
        [Authorize(Roles = "Administrador")]
        public ActionResult Delete(int id)
        {

            try
            {
                using (SamiEntities db = new SamiEntities())
                {
                    Compania emp = db.Companias.Where(x => x.IdCompania == id).FirstOrDefault<Compania>();
                    db.Companias.Remove(emp);
                    var valor = db.SaveChanges();
                    if (valor == 1)
                    {
                        return Json(new { success = true, message = "Se ha eliminado la compañía correctamente" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error, se ha encontrado un problema, intentelo de nuevo" }, JsonRequestBehavior.AllowGet);
                    }
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}