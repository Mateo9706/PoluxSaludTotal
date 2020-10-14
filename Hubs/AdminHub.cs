using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using Samico.Models;

namespace Samico.Hubs
{
    /// <summary>
    /// Class to handle Notification hub methods
    /// </summary>
    [HubName("SamiAdminCrudHub")]
    public class AdminHub : Hub
    {
        // Conexión a la base de datos
        private readonly SamiEntities _db = new SamiEntities();


        public void GetAllUsers()
        {
            var vm = new UsersViewModel();

            var roleManager =
                new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            var role = roleManager.FindById("4a42005a-01f8-4e0b-9ddd-3e5f2847b5a1").Users.First();
            var usersInRole =
             _db.AspNetUsers.Where(u => u.AspNetRoles.Select(r => r.Id).Contains(role.RoleId)).ToList();

            foreach (var user in usersInRole)
            {
                //Load users' data into a lightweight user object used by the view
                var userVm = new UserViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Status = user.Status.Value,
                    //Load company name from CompanyId
                    Company = (from compania in _db.Companias
                               where compania.IdCompania == user.CompanyId
                               select compania.Compañia).FirstOrDefault(),
                    NickNameRed = user.IdentificationUser
                };

                vm.Users.Add(userVm);
            }

            var jsonConvert = JsonConvert.SerializeObject(vm.Users.ToList());
            Clients.All.getAllUsers(jsonConvert);
        }

        public void Welcome()
        {
            var consultarUsuario = (from aspNetUser in _db.AspNetUsers
                                    where aspNetUser.UserName == Context.User.Identity.Name
                                    select aspNetUser).FirstOrDefault();

            // Lo ponemos como desconectado

            consultarUsuario.Status = 1;
            _db.AspNetUsers.AddOrUpdate(consultarUsuario);
            _db.SaveChanges();
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