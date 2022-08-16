using System.Text.Json.Serialization;

namespace SilentThief
{
    public class AbiturientOffer
    {
        [JsonPropertyName("prid")]        
        public int Id { get; set; }

        [JsonPropertyName("n")]
        public int Position { get; set; }

        [JsonPropertyName("fio")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("prsid")]
        public int StatusId { get; set; }

        [JsonPropertyName("p")]
        public int Priority { get; set; }

        [JsonPropertyName("kv")]
        public double Score { get; set; }

        [JsonPropertyName("rss")]
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
    }
}