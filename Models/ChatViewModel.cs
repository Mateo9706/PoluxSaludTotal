using System.ComponentModel.DataAnnotations;

namespace Samico.Models
{
    public class ChatViewModel
    {
        [Display(Name = "IdUserAspNetUser")]
        public string IdUserAspNetUser { get; set; }
        [Display(Name = "Chat ID")]
        public string ConnectionId { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "Group")]
        public string Group { get; set; }

        [Display(Name = "Company ID")]
        public int CompanyId { get; set; }

        [Display(Name = "OS")]
        public string OS { get; set; }

        [Display(Name = "SpecificKnowledgebaseId")]
        public string SpecificKnowledgebaseId { get; set; }

        [Display(Name = "SpecificSubscriptionKey")]
        public string SpecificSubscriptionKey { get; set; }

        [Display(Name = "EndPointApiLuis")]
        public string EndPointApiLuis { get; set; }

        [Display(Name = "AuthoringKeyApiLuis")]
        public string AuthoringKeyApiLuis { get; set; }

        [Display(Name = "UriBaseLuisApi")]
        public string UriBaseLuisApi { get; set; }

        [Display(Name = "KeySpellCheckApiLuis")]
        public string KeySpellCheckApiLuis { get; set; }

        [Display(Name = "Replied by bot?")]
        public bool RepliedByBot { get; set; }

        [Display(Name = "Attended by agent?")]
        public bool AttendedByAgent { get; set; }

        [Display(Name = "Reply Query")]
        public bool OtherAnswer { get; set; }

        [Display(Name = "Profile picture location")]
        public string ProfilePictureLocation { get; set; }

        [Display(Name = "TypeBoolResolved")]
        public int TypeBoolResolved { get; set; }

        [Display(Name = "IdSesionSaved")]
        public int IdSesionSaved { get; set; }

        [Display(Name = "UriBaseQnA")]
        public string UriBaseQnA { get; set; }
    }
}