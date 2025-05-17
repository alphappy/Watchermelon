namespace alphappy.Watchermelon
{
    public static class Extensions
    {
        /// <summary>Temporarily set <see cref="RNG.state"/>, then run <paramref name="func"/>.</summary>
        /// <returns>The return of <paramref name="func"/>.</returns>
        public static T WithSetSeed<T>(this int seed, Func<T> func)
        {
            RNG.State state = RNG.state;
            try
            {
                RNG.InitState(seed);
                return func();
            }
            finally { RNG.state = state; }
        }
        
        /// <summary>
        /// Pick a random element.
        /// </summary>
        /// <typeparam name="T">The type contained by the enumerable.</typeparam>
        /// <param name="list">The enumerable to pick from.</param>
        /// <returns>A random element of <paramref name="list"/>, or the default value of <typeparamref name="T"/> if <paramref name="list"/> is empty.</returns>
        public static T Pick<T>(this IEnumerable<T> list) => list.ElementAtOrDefault(RNG.Range(0, list.Count()));

        public static string Region(this string self) => self.Contains("_") && self.Split('_') is string[] list ? list[0] : self;

        public static string RegionNameFromCode(this string self)
        {
            return self.Region().ToUpperInvariant() switch
            {
                "WARA" => "Shattered Terrace",
                "WARB" => "Salination",
                "WARC" => "Fetid Glen",
                "WARD" => "Cold Storage",
                "WARE" => "Heat Ducts",
                "WARF" => "Aether Ridge",
                "WARG" => "The Surface",
                "WAUA" => "Ancient Urban",
                "WBLA" => "Badlands",
                "WDSR" => "Decaying Tunnels",
                "WGWR" => "Infested Wastes",
                "WHIR" => "Corrupted Factories",
                "WORA" => "Outer Rim",
                "WPTA" => "Signal Spires",
                "WRFA" => "Coral Caves",
                "WRFB" => "Turbulent Pump",
                "WRRA" => "Rusted Wrecks",
                "WRSA" => "Daemon",
                "WSKA" => "Torrential Railways",
                "WSKB" => "Sunlit Port",
                "WSKC" => "Stormy Coast",
                "WSKD" => "Shrouded Coast",
                "WSSR" => "Unfortunate Evolution",
                "WSUR" => "Crumbling Fringes",
                "WTDA" => "Torrid Desert",
                "WTDB" => "Desolate Tract",
                "WVWA" => "Verdant Waterways",
                _ => "UNKNOWN REGION"
            };
        }
    }
}
