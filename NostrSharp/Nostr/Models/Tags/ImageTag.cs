namespace NostrSharp.Nostr.Models.Tags
{
    public class ImageTag
    {
        public string ImageUrl { get; set; }
        /// <summary>
        /// Must be in the format of widthxheight, for example 1920x1080, or null
        /// </summary>
        public string? ImageWidthHeightInPx { get; set; } = null;


        public ImageTag() { }
        public ImageTag(string imageUrl, string? imageWidthHeightInPx)
        {
            ImageUrl = imageUrl;
            ImageWidthHeightInPx = imageWidthHeightInPx;
        }
    }
}
