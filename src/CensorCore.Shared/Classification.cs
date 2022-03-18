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
        
        /// <summary>
        /// Offset angle for any reference points this classification is based on. 
        /// This will only be set when the classification has been transformed from the original match. 
        /// This will (usually) be set to the angle between any existing reference points from the original match.
        /// </summary>
        /// <value>The angle in degrees of the underlying reference points (if present).</value>
        public float? SourceAngle {get;set;} = null;
    }
}
