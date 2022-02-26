namespace CensorCore
{
    public class CensoredImage
    {
        public byte[] ImageContents {get;}

        public CensoredImage(byte[] imageContents, string mimeType, string? imageDataUrl)
        {
            ImageContents = imageContents;
            MimeType = mimeType;
            ImageDataUrl = imageDataUrl ?? $"data:{mimeType};base64,{Convert.ToBase64String(imageContents, Base64FormattingOptions.InsertLineBreaks)}";
        }

        public string MimeType {get;}
        public string ImageDataUrl {get;}
    }
}
