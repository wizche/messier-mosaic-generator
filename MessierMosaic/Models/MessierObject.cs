using System.Text.Json.Serialization;

namespace MessierMosaic.Models
{
    public class MessierObject
    {
        [JsonPropertyName("messier")]
        public string Messier { get; set; } = string.Empty;
        [JsonPropertyName("objet")]
        public string Object { get; set; } = string.Empty;
        [JsonPropertyName("english_name_nom_en_anglais")]
        public string Name { get; set; } = string.Empty;
    }
}
