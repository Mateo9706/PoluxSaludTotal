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
    
    public partial class ConexionCA
    {
        public int Id { get; set; }
        public Nullable<int> IdCompania { get; set; }
        public string UsuarioCA { get; set; }
        public string PasswordCA { get; set; }
        public string Category { get; set; }
        public string Customer { get; set; }
        public string Requested_by { get; set; }
        public string Group { get; set; }
        public string Urgency { get; set; }
        public string CR { get; set; }
        public Nullable<System.DateTime> Fecha { get; set; }
        public string Group_ServiceDesk { get; set; }
    
        public virtual Compania Compania { get; set; }
    }
}
