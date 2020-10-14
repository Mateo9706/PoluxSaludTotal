using Newtonsoft.Json;
using Samico.Models;
using Samico.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;

namespace Samico.BotConnectJson
{
    public class Metadata
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class QnaDocument
    {
        public int id { get; set; }
        public string answer { get; set; }
        public string source { get; set; }
        public List<string> questions { get; set; }
        public List<Metadata> metadata { get; set; }
        public List<object> alternateQuestionClusters { get; set; }
        public string changeStatus { get; set; }
        public string kbId { get; set; }
    }

    public class RootObject
    {
        public List<QnaDocument> qnaDocuments { get; set; }
    }

    public class QnAConnect
    {
        private readonly SamiEntities _db = new SamiEntities();

        static string host = "https://westus.api.cognitive.microsoft.com";

        static string service = "/qnamaker/v4.0";
        static string method = "/knowledgebases/";
        // Metod List
        static string methodList = "/knowledgebases/{0}/{1}/qna/";

        // NOTE: Replace this with "test" or "prod".
        static string env = "test";

        public struct Response
        {
            public HttpResponseHeaders headers;
            public string response;

            public Response(HttpResponseHeaders headers, string response)
            {
                this.headers = headers;
                this.response = response;
            }
        }

        static string PrettyPrint(string s)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(s), Formatting.Indented);
        }

        async static Task<Response> Patch(string uri, string body, string key)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                request.Method = new HttpMethod("PATCH");
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                return new Response(response.Headers, responseBody);
            }
        }

        async static Task<Response> Get(string uri, string key)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                return new Response(response.Headers, responseBody);
            }
        }

        async static Task<Response> PostUpdateKB(string kb, string new_kb, string key)
        {
            string uri = host + service + method + kb;
            Console.WriteLine("Calling " + uri + ".");
            return await Patch(uri, new_kb, key);
        }

        async static Task<Response> GetStatus(string operation, string key)
        {
            string uri = host + service + operation;
            Console.WriteLine("Calling " + uri + ".");
            return await Get(uri, key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kb"></param>
        /// <param name="new_kb"></param>
        /// <returns></returns>
        public async Task UpdateKB(string kb, string new_kb, string key)
        {
            var response = await PostUpdateKB(kb, new_kb, key);
            var operation = response.headers.GetValues("Location").First();
            Console.WriteLine(PrettyPrint(response.response));

            var done = false;
            while (true != done)
            {
                response = await GetStatus(operation, key);
                Console.WriteLine(PrettyPrint(response.response));

                var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.response);

                String state = fields["operationState"];
                if (state.CompareTo("Running") == 0 || state.CompareTo("NotStarted") == 0)
                {
                    var wait = response.headers.GetValues("Retry-After").First();
                    Console.WriteLine("Waiting " + wait + " seconds...");
                    Thread.Sleep(Int32.Parse(wait) * 1000);
                }
                else
                {
                    Console.WriteLine("Press any key to continue.");
                    done = true;
                }
            }
        }

        async static Task<string> GetList(string uri, string key)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetQnA(string kb, string key)
        {
            var method_with_id = String.Format(methodList, kb, env);
            var uri = host + service + method_with_id;
            var response = await GetList(uri, key);

            response.Replace(@"\", string.Empty);

            RootObject datalist = JsonConvert.DeserializeObject<RootObject>(response);

            return response;
        }

        async Task<string> Post(string uri, string body, string endpointKey)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {

                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", "EndpointKey " + endpointKey);
                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();


            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="knowledgebaseId"></param>
        /// <param name="subscriptionKey"></param>
        /// <param name="uriBase"></param>
        /// <returns></returns>
        public async Task<Answer[]> GetAnswersAsync(string query, string knowledgebaseId, string subscriptionKey, string uriBase)
        {

            var response = "";

            //Build a Uri from the provided uriBase, KB ID and subscription key
            var qnaMakerUriBase = new Uri(uriBase);
            string uri = uriBase + "/knowledgebases/" + knowledgebaseId + "/generateAnswer";
            string questionJSON = @"{'question': '" + query + "'}";

            response = await Post(uri, questionJSON, subscriptionKey);
            //Prepare and send request, then get answer from service
            var answers = QnAMakerResult.FromJson(response).Answers;

            return answers;

        }

        async static Task<string> Post(string uri, string key)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return "{'result' : 'Success.'}";
                }
                else
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }


        async static void PublishKB(string kb, string key)
        {
            var uri = host + service + method + kb;
            Console.WriteLine("Calling " + uri + ".");
            var response = await Post(uri, key);
            Console.WriteLine(PrettyPrint(response));
            Console.WriteLine("Press any key to continue.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="cases"></param>
        /// <returns></returns>
        public async Task<string> QnaAnswer(string answer, int cases, string kb, string key, string uri)
        {
            var returnAnswer = "";
            switch (cases)
            {
                case 1:

                    await UpdateKB(kb, answer, key);

                    returnAnswer = "Exito";
                    break;

                case 2:

                    returnAnswer = await GetQnA(kb, key);

                    break;

                case 3:

                    PublishKB(kb, key);

                    break;

            }

            return returnAnswer;
        }

        public async Task<Answer[]> GetData(string answer, string kb, string key, string uri)

        {

            var method2 = "/qnamaker/knowledgebases/" + kb + "/generateAnswer";
            string uriC = uri + method2;

            var response = await Post(uriC, answer, key);
            //Prepare and send request, then get answer from service
            var answers = QnAMakerResult.FromJson(response).Answers;

            return answers;

        }
    }
}