using System.Text.Json.Serialization;

namespace Takap.Ulitity
{
    public class SampleResult : CommandResult
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}
