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

            Tabs = new OpTab[] { new(this, "Settings") };
            Tabs[0].AddItems(
                overrideBox, seedEntry,
                runningBox, speedSplider,
                button, button2,
                outputLabel, routeLabel
                );
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
    }
}
