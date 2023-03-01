namespace MessierMosaic.Db
{
    public class MessierObjectEntry
    {
        public long? Id { get; set; } = null;
        public byte[]? Preview { get; set; } = null;
        public byte[]? Image { get; set; } = null;
        public string ContentType { get; set; } = string.Empty;
        public string ImagePictureDescription { get; set; } = string.Empty;
    }
}
