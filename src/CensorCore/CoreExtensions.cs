namespace CensorCore {
    internal static class CoreExtensions {
        internal static Random Rand = new Random();
        internal static float GetScoreForClass(this MatchOptions opts, string className)
        {
            return opts.ClassScores.GetValueOrDefault(className, opts.MinimumScore);
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

        public static T Random<T>(this IEnumerable<T> input, int? count = null)
        {
            return input.ElementAt(Rand.Next(count ?? input.Count()));
        }

        internal static BoundingBox ScaleBy(this BoundingBox box, float amountX, float amountY)
        {
            box.X = box.X - amountX;
            box.Y = box.Y - amountY;
            box.Height = Convert.ToInt32(box.Height + (2 * amountY));
            box.Width = Convert.ToInt32(box.Width + (2 * amountX));
            return box;
        }

        internal static bool ContainsAny(this string src, params string[] options) {
            foreach (var opt in options)
            {
                if (src.Contains(opt)) {
                    return true;
                }
            }
            return false;
        }

        internal static bool ContainsAny(this string src, StringComparison comparison, params string[] options) {
            foreach (var opt in options)
            {
                if (src.Contains(opt, comparison)) {
                    return true;
                }
            }
            return false;
        }

        internal static float GetScaleFactor(this int? value, float defaultValue) {
            var factor = ((value ?? defaultValue) / defaultValue);
            return factor;
        }

        internal static float GetScaleFactor(this int value, float defaultValue) {
            var factor = (value / defaultValue);
            return factor;
        }

        internal static float GetScaleFactor(this float value, float defaultValue) {
            var factor = (value / defaultValue);
            return factor;
        }

        internal static (List<string>? Categories, Flurl.QueryParamCollection? Parameters) GetOptions(this string method, string methodName) {
            List<string>? categories = null;
            if (method.Contains('?') && method.Replace(methodName, string.Empty, StringComparison.CurrentCultureIgnoreCase)[0] == '?') {
                //query string parse
                var url = new Flurl.Url(method);
                if (url.QueryParams.GetAll("category") is var cats && cats.Any()) {
                    categories ??= new List<string>();
                    categories.AddRange(cats.Cast<string>());
                }
                if (url.QueryParams.TryGetFirst("categories", out var catsObj)) {
                    var splitCats = ((string)catsObj).Split(',', ';').ToList();
                    if (splitCats.Any()) {
                        categories ??= new List<string>();
                        categories.AddRange(splitCats);
                    }
                }
                return (categories, url.QueryParams);

            } else {
                var catString = method.Split(":").LastOrDefault();
                if (!string.IsNullOrWhiteSpace(catString)) {
                    var cats = catString.Split(',', ';').ToList();
                    if (!cats.Any()) {
                        throw new InvalidOperationException();
                    }
                    else {
                        categories = cats;
                    }
                }
                return (categories, null);
            }
        }

        internal static (List<string>? Categories, bool PreferBox) GetCaptionOptions(this string method, string methodName) {
            List<string>? categories = null;
            var preferBox = false;
            if (method.Contains('?') && method.Replace(methodName, string.Empty, StringComparison.CurrentCultureIgnoreCase)[0] == '?') {
                //query string parse
                var url = new Flurl.Url(method);
                if (url.QueryParams.GetAll("category") is var cats && cats.Any()) {
                    categories ??= new List<string>();
                    categories.AddRange(cats.Cast<string>());
                }
                if (url.QueryParams.TryGetFirst("categories", out var catsObj)) {
                    var splitCats = ((string)catsObj).Split(',', ';').ToList();
                    if (splitCats.Any()) {
                        categories ??= new List<string>();
                        categories.AddRange(splitCats);
                    }
                }
                if (url.QueryParams.TryGetFirst("preferBox", out var preferBoxParam)) {
                    if (bool.TryParse((string)preferBoxParam, out var boxPreferred)) {
                        preferBox = boxPreferred;
                    }
                }
                return (categories, preferBox);

            } else {
                var catString = method.Split(":").LastOrDefault();
                if (!string.IsNullOrWhiteSpace(catString)) {
                    var cats = catString.Split(',', ';').ToList();
                    if (!cats.Any()) {
                        throw new InvalidOperationException();
                    }
                    else {
                        categories = cats;
                    }
                }
                return (categories, preferBox);
            }
        }

        internal static float ClosestTo(this IEnumerable<float> values, float constant) {
            return values.Aggregate((x,y) => Math.Abs(x-constant) < Math.Abs(y-constant) ? x : y);
        }

    }
}