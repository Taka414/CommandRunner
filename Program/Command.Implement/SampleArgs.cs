using System.Text.Json.Serialization;

namespace Takap.Ulitity
{
    public class SampleArgs : ICommandArgs
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }
    }
}
