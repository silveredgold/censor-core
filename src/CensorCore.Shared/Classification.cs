namespace CensorCore
{
    public class Classification {
        public BoundingBox Box {get;set;}

        public Classification(BoundingBox box, float confidence, string label)
        {
            Box = box;
            Confidence = confidence;
            Label = label;
        }

        public float Confidence {get;set;}
        public string Label {get;set;}
    }
}
