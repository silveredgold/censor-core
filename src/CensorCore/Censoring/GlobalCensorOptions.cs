namespace CensorCore.Censoring
{
    public class GlobalCensorOptions
    {
        public bool? AllowTransformers { get;set; } = true;
        public float? RelativeCensorScale { get; set; } = 1F;
        public float? PaddingScale { get; set; } = 1F;
        public bool? ForcePixelBackground { get; set; } = false;

        public Dictionary<string, float> ClassStrength {get;set;} = new Dictionary<string, float>();
    }
}