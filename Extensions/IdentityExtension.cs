using Samico.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Samico.Extensions
{
    /// <summary>
    /// ASP.NET Identity extension methods to retrieve custom user attributes from database
    /// </summary>
    public static class IdentityExtension
    {
        private static readonly SamiEntities Db = new SamiEntities();

        /// <summary>
        /// Method to get Company ID from user identity
        /// </summary>
        /// <returns></returns>
        public static int GetCompanyId(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("CompanyId");
            // Test for null to avoid issues during local testing
            return (claim != null) ? Convert.ToInt32(claim.Value) : 0;
        }

        /// <summary>
        /// Method to get Profile picture location from user identity
        /// </summary>
        /// <returns></returns>
        public static string GetProfilePictureLocation(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("ProfilePictureLocation");
            // Test for null to avoid issues during local testing
            return (claim != null) ? claim.Value : "";
        }

        /// <summary>
        /// Method to get Company name from user identity
        /// </summary>
        /// <returns></returns>
        public static string GetCompanyName(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();
            
            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.Compañia).FirstOrDefault();
        }

        /// <summary>
        /// Method to get specific KB ID from user identity
        /// </summary>
        public static string GetSpecificKnowledgebaseId(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.KnowledgebaseId).FirstOrDefault();
        }

        /// <summary>
        /// Method to get specific KB ID from user identity
        /// </summary>
        public static string GetUriBaseQnA(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.QnamakerUriBase).FirstOrDefault();
        }

        /// <summary>
        /// Method to get specific subscription key from user identity
        /// </summary>
        public static string GetSpecificSubscriptionKey(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.QnamakerSubscriptionKey).FirstOrDefault();
        }

        /// <summary>
        /// Method to get specific subscription key from user identity
        /// </summary>
        public static string GetSpecificSubscriptionKeyAzure(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.QnamakerSubscriptionKeyAzure).FirstOrDefault();
        }

        ///<summary>
        ///Method to get EndpointApi from Luis
        /// </summary>
        public static string GetEndpointApiFromLuis(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.EndPointApiLuis).FirstOrDefault();
        }

        ///<summary>
        ///Method to get Authoring Key Api from Luis
        /// </summary>
        public static string GetAuthoringKeyApiFromLuis(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.AuthoringKeyApiLuis).FirstOrDefault();
        }

        ///<summary>
        ///Method to get UriBase Api from Luis
        /// </summary>
        public static string GetUriBaseApiLuis(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.UriBaseApiLuis).FirstOrDefault();
        }

        ///<summary>
        ///Method to get key Spell Check from Luis
        /// </summary>
        public static string GetKeySpellCheckLuis(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.KeySpellCheck).FirstOrDefault();
        }

        /// <summary>
        /// Method to get header color from user company
        /// </summary>
        public static string GetHeaderColor(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.HeaderColor).FirstOrDefault();
        }

        /// <summary>
        /// Method to get footer color from user company
        /// </summary>
        public static string GetFooterColor(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.FooterColor).FirstOrDefault();
        }

        /// <summary>
        /// Method to get logo location from user company
        /// </summary>
        public static string GetLogoLocation(this IIdentity identity)
        {
            var companyId = identity.GetCompanyId();

            return (from compania in Db.Companias
                    where compania.IdCompania.ToString() == companyId.ToString()
                    select compania.LogoLocation).FirstOrDefault();
        }
    }
}