using RWCustom;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace alphappy.Watchermelon
{
    public class Search : MonoBehaviour
    {
        public struct Step
        {
            public enum Type { NormalDynamicWarp, Visit, Perish }
            public Type type;
            public string source;
            public string target;
            private IEnumerable<string> arr;
            private int num;

            public readonly float IncreaseRipple => type switch { Type.Visit => num / 2f, Type.Perish => -num / 2f, _ => 0 };
            public readonly IEnumerable<string> TargetRegions => arr;

            public override string ToString()
            {
                return type switch
                {
                    Type.Visit => $"Visit {string.Join(", ", TargetRegions.Select(Extensions.RegionNameFromCode))} and {num} Spinning Tops",
                    Type.NormalDynamicWarp => $"Normal dynamic warp from {source.RegionNameFromCode()} to {target.RegionNameFromCode()} ({target.ToUpperInvariant()})",
                    Type.Perish => $"Perish to reduce Ripple by {-IncreaseRipple}",
                    _ => "unknown"
                };
            }

            public static Step BulkVisit(string operand)
            {
                MatchCollection matches = Regex.Matches(operand, "\\*");
                return new Step()
                {
                    type = Type.Visit,
                    num = matches.Count,
                    arr = operand.Replace("*", "").ToLowerInvariant().Split(' ').Select(Extensions.Region).Distinct()
                };
            }

            public static Step NormalDynamicWarp(string operand)
            {
                string[] split = operand.Split(' ').Select(x => x.Trim().ToLowerInvariant()).ToArray();
                return new Step() { type = Type.NormalDynamicWarp, source = split[0], target = split[1], arr = new string[] { split[1].Region() } };
            }

            public static Step Perish(string operand)
            {
                return new Step() { type = Type.Perish, num = int.Parse(operand) };
            }
        }

        public struct Result
        {
            public enum Type { Success, DynamicWarpIsntRight }
            public Type type;
            public string message;
            public Result(Type type = Type.Success, string message = "")
            {
                this.type = type;
                this.message = message;
            }

            public static bool operator true(Result self) => self.type == Type.Success;
            public static bool operator false(Result self) => self.type != Type.Success;
            public override string ToString() => message;
        }

        private static List<Step> steps = new();
        internal static bool stepsDirty = false;
        internal static List<Step> Steps
        {
            get => steps;
            set { stepsDirty = true; steps = value; }
        }

        internal static int seed = 1;
        internal static int step = 1;
        internal static int attempts = 0;
        internal static List<int> hits = new();
        internal static string error = null;
        internal static string Error
        {
            get => error;
            set { stepsDirty = true; error = value; }
        }

        internal static void InitializeComponent() => new GameObject("alphappy.Watchermelon").AddComponent<Search>();

        public void Update()
        {
            if (attempts > 0)
            {
                for (int i = 0; i < attempts; i++)
                {
                    Result result = DoesSeedWork(seed, steps);
                    if (result) hits.Add(seed);
                    seed += step;
                }
                attempts = 0;
            }
        }

        internal static void ReadRouteFile()
        {
            Plugin.Log("Reading");
            error = null;

            List<Step> s = new();
            int lineNum = 1;
            foreach (string line in File.ReadAllLines(Plugin.ROUTE_FILE_PATH))
            {
                if (ReadRouteLine(line, out string kind, out string operand))
                {
                    try
                    {
                        switch (kind)
                        {
                            case "VISIT": s.Add(Step.BulkVisit(operand)); break;
                            case "NORMAL": s.Add(Step.NormalDynamicWarp(operand)); break;
                            default: Error = $"Line {lineNum} is invalid: '{kind}' is not a valid step"; break;
                        }
                    }
                    catch (Exception e) { Error = $"Line {lineNum} is invalid: {e.GetType()}: {e.Message}"; }
                }
                else
                {
                    Error = $"Line {lineNum} is invalid: Not of the form 'KIND: OPERAND'";
                }
                if (error is not null) return;
                lineNum++;
            }
            SetNewRoute(s);
        }

        internal static bool ReadRouteLine(string line, out string kind, out string operand)
        {
            try
            {
                string[] parts = line.Split(':');
                kind = parts[0].Trim();
                operand = parts[1].Trim();
                return true;
            }
            catch { kind = null; operand = null; return false; }
        }

        internal static void SetNewRoute(List<Step> steps)
        {
            Steps = steps;
            hits = new();
            seed = 1;
        }

        internal static void TestRoute()
        {
            //TryRoute(new List<Step> {
            //    new(Step.Type.FixedWarp, "wskb", "wskb"),
            //    new(Step.Type.FixedWarp, "wskb", "warf"),
            //    new(Step.Type.FixedWarp, "warf_d06", "wskd_b33"),
            //    new(Step.Type.MeetSpinningTop, "wskd_b40", "ward_r15"),  // shrouded
            //    new(Step.Type.MeetSpinningTop, "warf_b33", "wtda_b12"),  // aether
            //    new(Step.Type.FixedWarp, "wtda_z01", "ware_h16"),
            //    new(Step.Type.MeetSpinningTop, "ware_h05", "wskc_a03"),  // heat
            //    new(Step.Type.MeetSpinningTop, "wtda_z14", "wbla_c01"),  // torrid
            //    new(Step.Type.MeetSpinningTop, "wbla_d03", "wskd_b01"),  // badlands
            //    new(Step.Type.FixedWarp, "wskd_b40", "ward_r15"),
            //    new(Step.Type.MeetSpinningTop, "ward_r02", "warb_f01"),  // cold
            //    new(Step.Type.DynamicWarp, "ward_r15", "warb_j07"),
            //    new(Step.Type.MeetSpinningTop, "warb_j01", "wara_p05"),  // sal
            //    new(Step.Type.DynamicWarp, "wara_p05", "wpta_f01"),
            //});
        }

        public static int? DoesRouteWork(IEnumerable<Step> steps, int initialSeed = 1, int attempts = 99999, int step = 1)
        {
            int seed = initialSeed;
            for (int i = 0; i < attempts; i++)
            {
                Result result = DoesSeedWork(seed, steps);
                if (result) Plugin.Log($"Seed {seed} works!"); else Plugin.Log($"Seed {seed} failed: {result}");
                seed += step;
            }
            return null;
        }

        public static Result DoesSeedWork(int seed, IEnumerable<Step> steps)
        {
            List<string> visitedRegions = new();
            float currentRipple = 1f;
            int normalWarpCount = 0;

            foreach (Step step in steps)
            {
                switch (step.type)
                {
                    case Step.Type.Visit:
                        currentRipple += step.IncreaseRipple;
                        break;

                    case Step.Type.NormalDynamicWarp:
                        if (ComputeTarget(seed + normalWarpCount, currentRipple, visitedRegions, step.source) is string actualTarget && actualTarget != step.target)
                            return new(Result.Type.DynamicWarpIsntRight, $"The warp that should've gone to {step.target} went to {actualTarget} instead");
                        normalWarpCount++;
                        break;
                }

                visitedRegions.AddRange(step.TargetRegions);
            }
            return new();
        }

        public static string ComputeTarget(int seed, float currentRipple, IEnumerable<string> visitedRegions, string currentRoom)
        {
            //if (seed == 1441)
            //{
            //    var targets = GetValidTargets(currentRipple, visitedRegions, currentRoom);
            //    var chosen = seed.WithSetSeed(targets.Pick).ToLowerInvariant();
            //    Plugin.Log($"chose {chosen} from {string.Join(",", targets)}");
            //    return chosen;
            //}
            return seed.WithSetSeed(GetValidTargets(currentRipple, visitedRegions, currentRoom).Pick).ToLowerInvariant();
        }

        internal static Dictionary<float, Dictionary<string, int>> PrecomputeWeights()
        {
            Dictionary<float, Dictionary<string, int>> d = new();
            for (float i = 1f; i <= 5f; i += 0.5f) d[i] = new();

            foreach (var tier in Custom.rainWorld.levelDynamicWarpTargets)
            {
                for (float ripple = tier.Key; ripple <= 5f; ripple += 0.5f)
                {
                    foreach (string room in tier.Value)
                    {
                        d[ripple][room.ToLowerInvariant()] = Mathf.Max(1, 4 - (int)(2 * (ripple - tier.Key)));
                    }
                }
            }

            return d;
        }

        private static Dictionary<float, Dictionary<string, int>> precomputedWeights;
        public static Dictionary<float, Dictionary<string, int>> PrecomputedWeights => precomputedWeights ??= PrecomputeWeights();

        public static List<string> GetValidTargets(
            float currentRipple, IEnumerable<string> visitedRegions, string currentRoomName, 
            IEnumerable<string> infectedRegions = null,
            bool princeConvo7 = false, bool spreadingRot = false, bool beatenSentientRot = false)
        {
            // based on `Watcher.WarpPoint.GetAvailableDynamicWarpTargets()`
            List<string> list = new();
            List<string> list2 = new();
            string item = "";
            //bool flag = room.world.game.GetStorySession.saveState.miscWorldSaveData.highestPrinceConversationSeen >= PrinceBehavior.PrinceConversation.PrinceConversationToId(WatcherEnums.ConversationID.Prince_7);
            foreach (KeyValuePair<float, List<string>> levelDynamicWarpTarget in Custom.rainWorld.levelDynamicWarpTargets)
            {
                if (!(currentRipple >= levelDynamicWarpTarget.Key) && !(levelDynamicWarpTarget.Key <= 1f))
                {
                    continue;
                }
                for (int j = 0; j < levelDynamicWarpTarget.Value.Count; j++)
                {
                    if (levelDynamicWarpTarget.Value[j].ToLowerInvariant() == currentRoomName.ToLowerInvariant() || Custom.rainWorld.levelBadWarpTargets.Contains(levelDynamicWarpTarget.Value[j]))
                    {
                        continue;
                    }
                    if (currentRoomName.Region() == levelDynamicWarpTarget.Value[j].Region()) continue;
                    int num = (currentRipple - levelDynamicWarpTarget.Key) switch
                    {
                        < 0.5f => 4,
                        < 1f => 3,
                        < 1.5f => 2,
                        _ => 1
                    };
                    string text3 = "";
                    if (levelDynamicWarpTarget.Value[j].Contains("_"))
                    {
                        text3 = levelDynamicWarpTarget.Value[j].Split('_')[0].ToLowerInvariant();
                    }
                    if (text3 != "")
                    {
                        if (spreadingRot)
                        {
                            if (infectedRegions?.Contains(text3) == false && !Region.HasSentientRotResistance(text3))
                            {
                                num *= 8;
                                list2.Add(levelDynamicWarpTarget.Value[j]);
                            }
                        }
                        else if (!visitedRegions.Contains(text3))
                        {
                            num *= 8;
                        }
                    }
                    for (int k = 0; k < num; k++)
                    {
                        list.Add(levelDynamicWarpTarget.Value[j]);
                    }
                }
            }
            if (spreadingRot && princeConvo7 && list2.Count > 0 && !beatenSentientRot)
            {
                list = list2;
            }
            if (list.Count == 0)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
