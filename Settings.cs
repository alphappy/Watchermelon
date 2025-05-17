using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System.Diagnostics;

namespace alphappy.Watchermelon
{
    public class Settings : OptionInterface
    {
        internal static Settings instance = new();
        internal static Configurable<int> seed = instance.config.Bind("seed", 1, new ConfigAcceptableRange<int>(int.MinValue + 2, int.MaxValue - 2));
        internal static Configurable<bool> doOverride = instance.config.Bind("override", true);
        internal static Configurable<bool> running = instance.config.Bind("running", false);
        internal static Configurable<int> seedsPerSecond = instance.config.Bind("seedsPerSecond", 100, new ConfigAcceptableRange<int>(0, 3000));
        internal static int? Seed => doOverride.Value ? seed.Value : null;
        internal static string SeedText => Seed?.ToString() ?? "off";

        internal OpLabelLong outputLabel;
        internal OpLabelLong routeLabel;
        internal OpSlider speedSplider;
        internal OpCheckBox runningBox;
        internal int nextUpdate = updateInterval;
        internal const int updateInterval = 10;
        internal DateTime blockOpenUntil;
        public override void Initialize()
        {
            base.Initialize();
            // first row
            OpCheckBox overrideBox = new(doOverride, 20f, 550f) { description="If checked, set the Watcher campaign seed.  This one actually does something." };
            OpUpdown seedEntry = new(seed, new(50f, 548f), 100) { description="The Watcher campaign seed to use.  This one actually does something." };

            // second row
            runningBox = new(running, new(20f, 500f)) { description="If checked, search for seeds that meet the currently loaded route." };
            speedSplider = new OpSlider(seedsPerSecond, new(59f, 495f), 300) { description="The rate at which new seeds are checked." };

            //third row
            OpSimpleButton button = new(new(50f, 450f), new(80f, 30f), "Reload") { description="Reload the route file." };
            static void Button_OnClick(UIfocusable _) => Search.ReadRouteFile();
            button.OnClick += Button_OnClick;

            OpSimpleButton button2 = new(new(150f, 450f), new(80f, 30f), "Open") { description="Open the folder containing the route file." };
            void OpenFolder(UIfocusable _)
            {
                DateTime now = DateTime.Now;
                if (now < blockOpenUntil) return;
                blockOpenUntil = now + new TimeSpan(0, 0, 3);

                Process.Start("explorer.exe", Plugin.ROUTE_FOLDER_PATH + "\\");
            }
            button2.OnClick += OpenFolder;

            // the rest
            outputLabel = new OpLabelLong(new(400f, 550f), new(150f, 0f), "");
            routeLabel = new OpLabelLong(new(20f, 400f), new(350f, 0f), "");

            Tabs = new OpTab[] { new(this, "Settings"), new(this, "Instructions") };
            Tabs[0].AddItems(
                overrideBox, seedEntry,
                runningBox, speedSplider,
                button, button2,
                outputLabel, routeLabel
                );

            OpLabelLong instructions = new(new(10f, 580f), new(580f, 0f), instructionText);

            Tabs[1].AddItems(instructions);
        }

        public override void Update()
        {
            base.Update();
            Search.attempts = runningBox.GetValueBool() ? int.Parse(speedSplider.value) : 0;
            if (nextUpdate-- <= 0)
            {
                nextUpdate = updateInterval;
                outputLabel.text = $"Current position: {Search.seed}\n\nFound {Search.hits.Count()} seeds:\n{string.Join("\n", Search.hits)}";
            }
            if (Search.stepsDirty)
            {
                routeLabel.text = Search.Error ?? "Current route:\n" + string.Join("", Search.Steps.Select(x => $"\n > {x}"));
                Search.stepsDirty = false;
            }
        }

        public override string ValidationString()
        {
            return $"{ValidationString_ID()} {SeedText}";
        }

        public static string instructionText =
            "The route is defined by a sequence of steps with one step on each line.  There are two types of steps:\n\n" +

            "   > A visit step looks like `VISIT: REGIONS` specifies that regions are being visited and Spinning Tops (STs) are being met.  `REGIONS` is a space-separated sequence of region codes.  If a ST is being met, an asterisk `*` is placed after that region.  Regions may be specified multiple times (for instance, to communicate the actual route) and this has no effect.\n" +
            "   > A normal dynamic warp step looks like `NORMAL: SOURCE TARGET`.  `SOURCE` is the code of the region (or room) that the warp is occurring from.  `TARGET` is the specific room that the warp should go to.  Watchermelon does not currently verify that `TARGET` is valid for a given Ripple level.\n\n" +

            "There are numerous factors which affect where a dynamic warp goes, and each of the route step types is intended to address these factors (though not every factor is accounted for yet):\n\n" +

            "   > The Watcher campaign seed.  In 1.10.4, the Remix setting the Watcher Remix menu does not work.  The one in Watchermelon does.\n" +
            "   > The number of dynamic warps already created.\n" +
            "   > The current Ripple level.  Each potential target has a minimum Ripple requirement, but the weight of a target also decreases as Ripple goes beyond that requirement.\n" +
            "   > The region that the warp is being created from.  A dynamic warp cannot target the region it originates from.\n" +
            "   > Which regions have already been visited.  It's important that `VISIT` steps indicate every region that gets loaded, even if a shelter does not get used in that region.\n" +
            "   > How many Prince encounters have occurred.\n" +
            "   > Whether a dynamic warp could spread rot to an infectable region.\n\n" +

            "Once a route has been created and saved with an external text editor, click Reload to import the route.  " +
            "The second checkbox and slider may be used to adjust the search speed " +
            "(set it too high and your game will probably become slow and unresponsive).  " +
            "Once a seed has been found, use the first checkbox and numeric entry to use that seed.\n\n" +

            "While Watchermelon will let you use (almost) any seed value for testing, note that only seeds 1 through 99,999 are legal in the Watcher Remix interface (whenever it actually gets fixed).  0 is a valid value for the Watcher Remix interface, but this just causes the game to pick a random seed when the campaign starts.";
    }
}
