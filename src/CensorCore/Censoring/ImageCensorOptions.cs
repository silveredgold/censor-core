namespace CensorCore.Censoring
{
    public class ImageCensorOptions {
        public string CensorType {get;set;}

        public ImageCensorOptions(string censorType)
        {
            CensorType = censorType;
        }

        public ImageCensorOptions(string censorType, int level) : this(censorType)
        {
            Level = level;
        }

        public int? Level {get;set;}
    }
}
