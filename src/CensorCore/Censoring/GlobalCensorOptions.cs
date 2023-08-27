namespace CensorCore.Censoring
{
    public class GlobalCensorOptions
    {
        public bool? AllowTransformers { get;set; } = true;
        public float? RelativeCensorScale { get; set; } = 1F;
        public float? PaddingScale { get; set; } = 1F;
        public bool? ForcePixelBackground { get; set; } = false;

        public Dictionary<string, float> ClassStrength {get;set;} = new Dictionary<string, float>();
        /// <summary>
        /// This is a kludge solution to a real problem. As such, while it is present (and in use) in
        /// current versions of CensorCore, you should not rely on it remaining part of the long-term 
        /// and/or stable API in future. Censoring providers should use this to increase the layers 
        /// of effects applied to certain match classes;
        /// </summary>
        /// <typeparam name="string">The match class to apply to.</typeparam>
        /// <typeparam name="int">A positive or negative value that will get added to the provider's default layer value.</typeparam>
        /// <returns></returns>
        public Dictionary<string, int> LayerModifier {get;set;} = new Dictionary<string, int>();
    }
}