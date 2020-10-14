using System;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Samico.Models;
using Samico.Utilities;

namespace Samico.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private readonly SamiEntities _db = new SamiEntities();
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };
        public string NameUser = "";



        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

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

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            //If user is already logged in
            if (Request.IsAuthenticated)
                //Redirect to Index view
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // GET: /Account/LoginWebex
        [AllowAnonymous]
        public ActionResult LoginWebex(string returnUrl)
        {
            //If user is already logged in
            if (Request.IsAuthenticated)
                //Redirect to Index view
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public int RandomNumber()
        {
            int min = 100, max = 5000;
            Random random = new Random();
            return random.Next(min, max);
        }
        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {

            var param = false;

            if (model.typeLoginValidate == 1)
            {
                model.Password = "Test12345*";
                var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                        where aspNetUser.UserName == model.Username
                                        select aspNetUser).FirstOrDefault();

                if (consultarUsuario != null)
                {
                    //if (consultarUsuario.Status == 1)
                    //{
                    //    ModelState.AddModelError("", "La cuenta ya se encuentra conectada.");
                    //    return View(model);
                   // }
                    //else
                    //{
                        if (consultarUsuario.IdentificationUser == null)
                        {
                            consultarUsuario.IdentificationUser = model.UserRed;

                            //Save changes
                            _db.AspNetUsers.AddOrUpdate(consultarUsuario);
                            _db.SaveChanges();
                        }
                        var loggedinUserDA = await UserManager.FindAsync(model.Username, "Test12345*");

                        if (loggedinUserDA != null)
                        {

                            await UserManager.UpdateSecurityStampAsync(loggedinUserDA.Id);
                        }
                    //}
                }
                else
                {
                    // Si el usuario ingresa por primera vez, se guardan los datos a la base de datos.

                    //Create a role manager object

                    var roleStore = new RoleStore<IdentityRole>();
                    var roleMgr = new RoleManager<IdentityRole>(roleStore);
                    var profile = "avatar.jpeg";
                    var email = model.Correo.Split('@');
                    var emailFinal = email[0] + "@axity.com";
                    var companyId = 1;

                    //Load user data into memory
                    // Se guardan los datos
                    var user = new ApplicationUser { UserName = model.Username, Email = emailFinal, CompanyId = companyId, ProfilePictureLocation = profile, IdentificationUser = model.UserRed };

                    // Se añade la clave y se guarda a la base de datos.
                    var result1 = await UserManager.CreateAsync(user, "Test12345*");

                    //IF created succesfully
                    if (result1.Succeeded)
                    {
                        //Se crea el objeto del id del usuario registrado
                        var userId = await UserManager.FindByNameAsync(model.Username);
                        //Se le añade un rol predeterminado (USUARIO)
                        var role = await roleMgr.FindByIdAsync("e4501cfd-212b-4e67-a507-e4469657caf7");
                        //Se añade el rol y el id del usuario
                        await UserManager.AddToRoleAsync(userId.Id, role.Name);

                    }
                    AddErrors(result1);

                    // Inicia sesión automáticamente

                    var loggedinUserDA = await UserManager.FindAsync(model.Username, "Test12345*");

                    if (loggedinUserDA != null)
                    {

                        await UserManager.UpdateSecurityStampAsync(loggedinUserDA.Id);
                    }
                }
            }
            else if(model.typeLoginValidate == 2)
            {
                DataEncrypt valores = new DataEncrypt();
                //var ttr = ServiceSAC.Service1SoapClient;
                ServiceSAC.Service1SoapClient ps = new ServiceSAC.Service1SoapClient();
                

                var user = model.Username;
                var pwd = model.Password;

                var userEncryp = valores.Encrypt(user);
                var pwdEncryp = valores.Encrypt(pwd);

                try
                {
                    var previewResult = ps.LoginAsync(userEncryp, pwdEncryp);
                    var resultes = previewResult.Result;
                    NameUser = resultes.Body.LoginResult;//errorThe user name or password is incorrect.

                    if (NameUser.Contains("incorrect"))
                    {
                        var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                                where aspNetUser.UserName == model.Username
                                                select aspNetUser).FirstOrDefault();
                        if (consultarUsuario.Status == 1)
                        {
                            ModelState.AddModelError("", "La cuenta ya se encuentra conectada.");
                            return View(model);
                        }
                        else
                        {

                            var loggedinUserDA = await UserManager.FindAsync(model.Username, model.Password);

                            if (loggedinUserDA != null)
                            {
                                await UserManager.UpdateSecurityStampAsync(loggedinUserDA.Id);
                                var result2 = await SignInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, shouldLockout: false);
                                switch (result2)
                                {
                                    case SignInStatus.Success:
                                        if (param)
                                            return RedirectToAction("Index", "Home", new
                                            {
                                                autoAgent = "true"
                                            });
                                        else
                                            return RedirectToAction("Index", "Home");//hola
                                    case SignInStatus.LockedOut:
                                        return View("Lockout");
                                        //case SignInStatus.RequiresVerification:
                                        //return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, model.RememberMe });
                                }
                            }
                        }
                    }
                    else
                    {
                        var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                                where aspNetUser.UserName == NameUser
                                                select aspNetUser).FirstOrDefault();


                        if (consultarUsuario != null)
                        {
                            if (consultarUsuario.IdentificationUser == null)
                            {
                                consultarUsuario.IdentificationUser = user;

                                //Save changes
                                _db.AspNetUsers.AddOrUpdate(consultarUsuario);
                                _db.SaveChanges();
                            }
                            var loggedinUserDA = await UserManager.FindByNameAsync(NameUser);
                            await SignInManager.SignInAsync(loggedinUserDA, true, false);
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            Console.Write("pr");
                            var roleStore = new RoleStore<IdentityRole>();
                            var roleMgr = new RoleManager<IdentityRole>(roleStore);
                            var profile = "avatar.jpeg";

                            var companyId = 3;
                            var random = RandomNumber();
                            var aleaEmail = "Salud" + random.ToString() + ".Total@axity.com";
                            var email = user + ".ST@axity.com"; 
                            var userName = new ApplicationUser { UserName = NameUser, Email = email, CompanyId = companyId, ProfilePictureLocation = profile, IdentificationUser = null };

                            // Se añade la clave y se guarda a la base de datos.
                            var result1 = await UserManager.CreateAsync(userName);

                            //IF created succesfully
                            if (result1.Succeeded)
                            {
                                //Se crea el objeto del id del usuario registrado
                                var userId = await UserManager.FindByNameAsync(NameUser);
                                //Se le añade un rol predeterminado (USUARIO)
                                var role = await roleMgr.FindByIdAsync("e4501cfd-212b-4e67-a507-e4469657caf7");
                                //Se añade el rol y el id del usuario
                                await UserManager.AddToRoleAsync(userId.Id, role.Name);
                                await SignInManager.SignInAsync(userName, true, false);
                                return RedirectToAction("Index", "Home");

                            }
                            AddErrors(result1);

                        }
                    }
                }
                catch
                {
                    var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                            where aspNetUser.UserName == model.Username
                                            select aspNetUser).FirstOrDefault();
                    if (consultarUsuario.Status == 1)
                    {
                        ModelState.AddModelError("", "La cuenta ya se encuentra conectada.");
                        return View(model);
                    }
                    else
                    {
                        
                        var loggedinUserDA = await UserManager.FindAsync(model.Username, model.Password);

                        if (loggedinUserDA != null)
                        {
                            await UserManager.UpdateSecurityStampAsync(loggedinUserDA.Id);
                            var result2 = await SignInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, shouldLockout: false);
                            switch (result2)
                            {
                                case SignInStatus.Success:
                                    if (param)
                                        return RedirectToAction("Index", "Home", new
                                        {
                                            autoAgent = "true"
                                        });
                                    else
                                        return RedirectToAction("Index", "Home");
                                case SignInStatus.LockedOut:
                                    return View("Lockout");
                                    //case SignInStatus.RequiresVerification:
                                    //return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, model.RememberMe });
                            }
                        }
                    }
                }
                



            }

            return View(model);
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Requerir que el usuario haya iniciado sesión con nombre de usuario y contraseña o inicio de sesión externo
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // El código siguiente protege de los ataques por fuerza bruta a los códigos de dos factores. 
            // Si un usuario introduce códigos incorrectos durante un intervalo especificado de tiempo, la cuenta del usuario 
            // se bloqueará durante un período de tiempo especificado. 
            // Puede configurar el bloqueo de la cuenta en IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Código no válido.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    // Para obtener más información sobre cómo habilitar la confirmación de cuentas y el restablecimiento de contraseña, visite https://go.microsoft.com/fwlink/?LinkID=320771
                    // Enviar correo electrónico con este vínculo
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirmar cuenta", "Para confirmar la cuenta, haga clic <a href=\"" + callbackUrl + "\">aquí</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // Si llegamos a este punto, es que se ha producido un error y volvemos a mostrar el formulario
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // No revelar que el usuario no existe o que no está confirmado
                    return View("ForgotPasswordConfirmation");
                }

                // Para obtener más información sobre cómo habilitar la confirmación de cuentas y el restablecimiento de contraseña, visite https://go.microsoft.com/fwlink/?LinkID=320771
                // Enviar correo electrónico con este vínculo
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Restablecer contraseña", "Para restablecer la contraseña, haga clic <a href=\"" + callbackUrl + "\">aquí</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // Si llegamos a este punto, es que se ha producido un error y volvemos a mostrar el formulario
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // No revelar que el usuario no existe
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Solicitar redireccionamiento al proveedor de inicio de sesión externo
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generar el token y enviarlo
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Si el usuario ya tiene un inicio de sesión, iniciar sesión del usuario con este proveedor de inicio de sesión externo
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // Si el usuario no tiene ninguna cuenta, solicitar que cree una
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Obtener datos del usuario del proveedor de inicio de sesión externo
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            var request = (from connectAgent in _db.ConexionAgentes
                           where connectAgent.IdUser == User.Identity.Name
                           orderby connectAgent.id descending
                           select connectAgent);

            var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                    where aspNetUser.UserName == User.Identity.Name
                                    select aspNetUser).FirstOrDefault();

            // Lo ponemos como desconectado

            consultarUsuario.Status = 0;
            _db.AspNetUsers.AddOrUpdate(consultarUsuario);
            _db.SaveChanges();

            if (request.Any())
            {
                var desconexion = request.First();
                //Mark the request as attended
                desconexion.FechaDesconexion = DateTime.Now;
                _db.SaveChanges();
            }

            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Aplicaciones auxiliares
        // Se usa para la protección XSRF al agregar inicios de sesión externos
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}
