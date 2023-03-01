using System.Text.Json.Serialization;

namespace MessierMosaic.ViewModels
{
    public class MessierObject
    {
        [JsonPropertyName("key")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("common_name")]
        public string CommonName { get; set; } = string.Empty;
        [JsonPropertyName("constellation")]
        public string Constellation { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("picture_url")]
        public string PictureUrl { get; set; } = string.Empty;

        public string GetTitle()
        {
            var title = this.Name;
            if(CommonName != string.Empty)
            {
                title += " - ";
                title += CommonName;
            }
            return title;
        }

        public string GetPictureDescription()
        {
            var description = GetTitle();
            if (description.Length <= 3)
            {
                description += " - ";
                description += Type;
            }
            return description;
        }
    }
}
