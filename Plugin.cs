using BepInEx;
using BepInEx.Logging;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.IO;
using UnityEngine;
using MPD = PlayerProgression.MiscProgressionData;

namespace alphappy.Watchermelon
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "alphappy.watchermelon";
        public const string PLUGIN_NAME = "Watchermelon";
        public const string PLUGIN_VERSION = "0.2.1";
        public static string ROUTE_FOLDER_PATH = Application.persistentDataPath.Replace('/', '\\') + "\\ModConfigs\\Watchermelon";
        public static string ROUTE_FILE_PATH = ROUTE_FOLDER_PATH + "\\route.txt";

        internal static bool initialized;
        public static ManualLogSource logger;
        public static void Log(object o) => logger.LogDebug(o);
        public static void Log(Exception ex) => logger.LogError(ex);
        public Plugin() { logger = Logger; }

        public void OnEnable()
        {
            if (initialized) return;
            try
            {
                initialized = true;

                On.RainWorld.OnModsInit += Initialization;
                //IL.Player.WatcherUpdate += Player_WatcherUpdate;
                //IL.Watcher.Watcher.OnInit += Watcher_OnInit;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets += LogTargetList;

                new Hook(typeof(MPD).GetProperty(nameof(MPD.watcherCampaignSeed)).GetGetMethod(), GetSeedHook);

                if (!Directory.Exists(ROUTE_FOLDER_PATH)) Directory.CreateDirectory(ROUTE_FOLDER_PATH);
                if (!File.Exists(ROUTE_FILE_PATH)) File.WriteAllText(ROUTE_FILE_PATH, "VISIT: wskb warf wskd* ward warf* wtda ware* wskc wtda* wbla* wskd ward* warb ward\r\nNORMAL: ward warb_j07\r\nVISIT: warb* wara\r\nNORMAL: wara wpta_f01");

                Log("Enabled");
            }
            catch (Exception e) { Log(e); }
        }

        private List<string> LogTargetList(On.Watcher.WarpPoint.orig_GetAvailableDynamicWarpTargets orig, AbstractRoom room, bool spreadingRot)
        {
            List<string> ret = orig(room, spreadingRot);
            Log($"Warping from {room.name} with Ripple {room.world.game.GetStorySession.saveState.deathPersistentSaveData.rippleLevel}, possible targets are [{string.Join(", ", ret)}]");
            return ret;
        }

        private void Initialization(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(PLUGIN_GUID, Settings.instance);
            Search.InitializeComponent();
            Search.TestRoute();
        }

        internal int GetSeedHook(Func<MPD, int> orig, MPD self) => Settings.Seed ?? orig(self);

        private void Watcher_OnInit(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(x => x.MatchNewobj(typeof(ConfigAcceptableRange<int>)));

            c.GotoPrev(MoveType.After, x => x.MatchLdcI4(0));
            int ChangeMin(int _) => int.MinValue;
            c.EmitDelegate(ChangeMin);

            c.GotoPrev(MoveType.After, x => x.MatchLdcI4(99999));
            int ChangeMax(int _) => int.MaxValue;
            c.EmitDelegate(ChangeMax);
        }

        private void Player_WatcherUpdate(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.Before, x => x.MatchCallOrCallvirt(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.watcherCampaignSeed)).GetSetMethod()));

            static int Delegate(int prev) => Watcher.Watcher.cfgForcedCampaignSeed.Value is int seed and not 0 ? seed : prev;
            c.EmitDelegate(Delegate);
        }
    }
}
