using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Samico.Models
{

    public class UsersViewModel
    {
        public List<UserViewModel> Users { get; set; }

        public UsersViewModel()
        {
            Users = new List<UserViewModel>();
        }
    }

    public class UserViewModel
    {
        public int Status { get; set; }
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Usuario")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Dirección inválida")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Compañía")]
        public int CompanyId { get; set; }

        [Required]
        [Display(Name = "Rol")]
        public string Role { get; set; }

        [Display(Name = "Foto de perfil")]
        public string ProfilePictureLocation { get; set; }

        //Required for mapping
        public IEnumerable<SelectListItem> CompaniesList { get; set; }
        public IEnumerable<SelectListItem> RolesList { get; set; }
        //Auxiliar attributes
        public List<string> Roles { get; set; }
        public string Company { get; set; }
        [Required]
        [Display(Name = "Usuario de Red DA")]
        public string NickNameRed {get;set;}
    }

    public class RegistrarNuevoUsuarioApi
    {
        public string NicknameRed { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int CompanyId { get; set; }
        public string Role { get; set; }
        public string ProfilePictureLocation { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Usuario de Red DA")]
        public string NicknameRed { get; set; }

        [Required]
        [Display(Name = "Usuario")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Dirección inválida")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Compañía")]
        public int CompanyId { get; set; }

        [Required]
        [Display(Name = "Rol")]
        public string Role { get; set; }

        [Display(Name = "Foto de perfil")]
        public string ProfilePictureLocation { get; set; }

        public IEnumerable<SelectListItem> CompaniesList { get; set; }
        public IEnumerable<SelectListItem> RolesList { get; set; }

        [NotMapped]
        public HttpPostedFileBase ImageUpload { get; set; }

        public RegisterViewModel()
        {
            ProfilePictureLocation = "~/Images/UploadedProfilePictures/avatar.jpeg";
        }
    }
}