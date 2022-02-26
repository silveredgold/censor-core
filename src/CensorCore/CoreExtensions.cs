namespace CensorCore {
    internal static class CoreExtensions {
        internal static Random Rand = new Random();
        internal static float GetScoreForClass(this MatchOptions opts, string className)
        {
            return opts.ClassScores.GetValueOrDefault(className, opts.MinimumScore);
        }

        internal static BoundingBox ToBox(this float[] coords)
        {
            return new BoundingBox(coords[0], coords[1], coords[2], coords[3]);
        }

        internal static BoundingBox ToBox(this float[] coords, float scaleFactor)
        {
            return new BoundingBox(coords[0] * scaleFactor, coords[1] * scaleFactor, coords[2] * scaleFactor, coords[3] * scaleFactor);
        }

        internal static float GetNearest(this IEnumerable<float> values, float target)
        {
            var nearest = values.Aggregate((current, next) => Math.Abs((float)current - target) < Math.Abs((float)next - target) ? current : next);
            return nearest;
        }

        public static T Random<T>(this IEnumerable<T> input)
        {
            return input.ElementAt(Rand.Next(input.Count()));
        }

    }
}