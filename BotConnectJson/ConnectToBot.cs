using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Samico.BotConnectJson
{

    public class ConnectToBot
    {
        public class RootObject
        {
            public string Respuesta { get; set; }
            public string Intencion { get; set; }
            public string Entidades { get; set; }
            public string PuntajeLuis { get; set; }
            public string PuntajeQnA { get; set; }
            public string TipoCaso { get; set; }
            public string FormatoLuis { get; set; }
        }

        private static async Task<RootObject> PostAsync(string key, string uri, string questionJSON)
        {
            RootObject Result = new RootObject();

            using (var client = new HttpClient())

            using (var request = new HttpRequestMessage())
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(uri);
                    request.Content = new StringContent(questionJSON, Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", "Bearer " + key);
                    var response = await client.SendAsync(request);

                    var JsonDataResponse = await response.Content.ReadAsStringAsync();
                    Result = JsonConvert.DeserializeObject<RootObject>(JsonDataResponse);

                }
                catch (Exception ex) { }


            }

            return Result;
        }

        /// <summary>
        /// Method to handle question's sending to QnA Maker service
        /// </summary>
        /// <param name="query">Question typed by the user</param>
        /// <param name="knowledgebaseId">QnA Maker Knowledge Base ID</param>
        /// <param name="subscriptionKey">QnA Maker Subscription Key</param>
        /// <param name="uriBase">QnA Maker connection URL</param>
        /// <returns>Answer collection from QnA based on user question</returns>
        public async Task<RootObject> GetAnswersAsync(string subscriptionKey, string uri, string question, int idSesion, string idUsuario, string SO, string plataforma)
        {
            string questionJSON = @"{'Question': '" + question + "', " +
                "'idSesion':'" + idSesion + "'," +
                "'idUsuario' : '" + idUsuario + "', " +
                "'SistemaOperativo': '" + SO + "', " +
                "'plataforma':'" + plataforma + "'}";

            var response = await PostAsync(subscriptionKey, uri, questionJSON);
            //Prepare and send request, then get answer from service
            return response;

        }
    }
}