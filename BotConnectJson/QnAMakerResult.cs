using Newtonsoft.Json;

namespace Samico.BotConnectJson
{
    public partial class QnAMakerResult
    {
        [JsonProperty("answers")]
        public Answer[] Answers { get; set; }

    }

    public class Answer
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("answer")]
        public string AnswerText { get; set; }

        [JsonProperty("questions")]
        public object[] Questions { get; set; }

        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("metadata")]
        public object[] Metadata { get; set; }
    }

    public partial class QnAMakerResult
    {
        public static QnAMakerResult FromJson(string json) => JsonConvert.DeserializeObject<QnAMakerResult>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this QnAMakerResult self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }

}