using System.Text.Json.Serialization;

namespace SilentThief
{
    public class Subject
    {
        [JsonPropertyName("sn")]
        public string Name { get; set; } = string.Empty;
    }
}