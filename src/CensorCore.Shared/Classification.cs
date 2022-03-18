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

        /// <summary>
        /// Indicates if this classification's bounding box is a "virtual" match.
        /// If this is true, the box should only be considered a valid match if it
        /// has also had relevant transforms applied.
        /// </summary>
        /// <remarks>
        /// The most notable effect of this flag being true is that providers that
        /// don't support rotation will likely ignore the match entirely.
        /// </remarks>
        /// <value>True if the bounding box is virtual, otherwise false.</value>
        public bool VirtualBox {get;set;} = false;
    }
}
