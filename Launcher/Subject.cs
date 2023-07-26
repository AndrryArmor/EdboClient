using System.Text.Json.Serialization;

namespace EdboClient.Launcher
{
    public class Subject
    {
        [JsonPropertyName("sn")]
        public string Name { get; set; } = string.Empty;
    }
}