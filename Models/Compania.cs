//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Samico.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Compania
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Compania()
        {
            this.AgentRequestNotifications = new HashSet<AgentRequestNotification>();
            this.AspNetUsers = new HashSet<AspNetUser>();
            this.ConexionCAs = new HashSet<ConexionCA>();
        }
    
        public int IdCompania { get; set; }
        public string Compañia { get; set; }
        public string KnowledgebaseId { get; set; }
        public string QnamakerSubscriptionKey { get; set; }
        public string QnamakerUriBase { get; set; }
        public string EndPointApiLuis { get; set; }
        public string AuthoringKeyApiLuis { get; set; }
        public string UriBaseApiLuis { get; set; }
        public string KeySpellCheck { get; set; }
        public string HeaderColor { get; set; }
        public string FooterColor { get; set; }
        public string LogoLocation { get; set; }
        public Nullable<bool> ValidateDA { get; set; }
        public string IpAuthenticationDA { get; set; }
        public string Dominio_Compania { get; set; }
        public string Dominio_SamiApi { get; set; }
        public string QnamakerSubscriptionKeyAzure { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AgentRequestNotification> AgentRequestNotifications { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AspNetUser> AspNetUsers { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConexionCA> ConexionCAs { get; set; }
    }
}
