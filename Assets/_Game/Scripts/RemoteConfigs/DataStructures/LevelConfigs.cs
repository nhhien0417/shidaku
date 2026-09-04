using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UserDataPack;
using LevelDesign;
using Design;

namespace RemoteConfigs
{
    [Serializable]
    public class AutoHintConfig
    {
        public int ActiveToLevel;
        public float DefaultDelay;
        public List<AutoHintInterval> Intervals = new();

        public void ApplyRemoteConfig(JSONNode root)
        {
            if (root == null || !root.IsObject) return;

            var activeToLevel = root["active_to_level"] != null
                ? root["active_to_level"].AsInt
                : ActiveToLevel;
            var intervalsNode = root["interval"];
            if (activeToLevel < 0) return;
            if (intervalsNode == null)
            {
                ActiveToLevel = activeToLevel;
                return;
            }
            if (!intervalsNode.IsArray) return;

            var intervals = new List<AutoHintInterval>();
            var configuredLevels = new HashSet<int>();
            var defaultDelay = DefaultDelay;

            foreach (JSONNode node in intervalsNode.AsArray)
            {
                if (!node.IsObject) continue;

                if (node["default"] != null)
                {
                    if (!node["default"].Value.TryParseFloat(out var parsedDefaultDelay)
                        || parsedDefaultDelay <= 0f) continue;
                    defaultDelay = parsedDefaultDelay;
                    continue;
                }

                if (node["delay"] == null
                    || !node["delay"].Value.TryParseFloat(out var delay)
                    || delay <= 0f)
                    continue;

                var levels = new List<int>();
                if (node["range"] != null && node["range"].IsArray && node["range"].Count == 2)
                {
                    var min = node["range"][0].AsInt;
                    var max = node["range"][1].AsInt;
                    if (min <= 0 || min > max) continue;
                    for (var level = min; level <= max; level++) levels.Add(level);
                }
                else if (node["arr"] != null && node["arr"].IsArray)
                {
                    foreach (JSONNode levelNode in node["arr"].AsArray)
                    {
                        if (!levelNode.IsNumber || levelNode.AsInt <= 0)
                        {
                            levels.Clear();
                            break;
                        }
                        levels.Add(levelNode.AsInt);
                    }
                }
                else continue;

                if (levels.Count == 0) continue;

                var hasDuplicate = false;
                foreach (var level in levels)
                {
                    if (configuredLevels.Contains(level))
                    {
                        hasDuplicate = true;
                        break;
                    }
                }
                if (hasDuplicate) continue;
                foreach (var level in levels) configuredLevels.Add(level);

                intervals.Add(new AutoHintInterval
                {
                    Levels = levels,
                    Delay = delay
                });
            }

            ActiveToLevel = activeToLevel;
            DefaultDelay = defaultDelay;
            Intervals = intervals;
        }

        public bool IsActive(int level)
        {
            return level > 0 && level <= ActiveToLevel;
        }

        public float GetDelay(int level)
        {
            foreach (var interval in Intervals)
                if (interval.Levels.Contains(level))
                    return interval.Delay;
            return DefaultDelay;
        }
    }

    [Serializable]
    public class AutoHintInterval
    {
        public List<int> Levels = new();
        public float Delay;
    }

    [Serializable]
    public class LevelDifficultAdjustRule
    {
        public int Window;
        public int LevelStartAdjustment;
        public float MinAdjustment;
        public float MaxAdjustment;
        public float AdjustmentValue;
        public List<string> Increase = new();
        public List<string> Decrease = new();

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var root = JSON.Parse(json);
                if (root != null)
                {
                    Window = root["window"] != null ? root["window"].AsInt : Window;
                    LevelStartAdjustment = root["level_start_adjustment"] != null ? root["level_start_adjustment"].AsInt : LevelStartAdjustment;
                    if (root["min_adjustment"]?.Value.TryParseFloat(out var minAdjustment) == true)
                        MinAdjustment = minAdjustment;
                    if (root["max_adjustment"]?.Value.TryParseFloat(out var maxAdjustment) == true)
                        MaxAdjustment = maxAdjustment;
                    if (root["adjustment_value"]?.Value.TryParseFloat(out var adjustmentValue) == true)
                        AdjustmentValue = adjustmentValue;

                    if (root["increase"] != null && root["increase"].IsArray)
                    {
                        Increase.Clear();
                        foreach (JSONNode node in root["increase"].AsArray)
                            Increase.Add(node.Value);
                    }

                    if (root["decrease"] != null && root["decrease"].IsArray)
                    {
                        Decrease.Clear();
                        foreach (JSONNode node in root["decrease"].AsArray)
                            Decrease.Add(node.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelDifficultAdjustRule] Parse failed: {e.Message}");
            }
        }
    }

    [Serializable]
    public class LevelDifficultConfig
    {
        public List<FixedConfig> FixedLevels = new();
        public List<RangeRepeatingConfig> RangeRepeatingCycle = new();
        public List<float> DefaultRepeatingCycle = new();
        public List<IntOptions> DefaultBoardSizeCycle = new();
        public List<IntOptions> DefaultFirstTechniqueCycle = new();
        public List<IntOptions> DefaultHighestTechniqueCycle = new();
        public FixedConfig AfterArtPuzzle = new();

        private Dictionary<int, FixedConfig> _fixedLevelDict = new();

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var root = JSON.Parse(json);
                if (root != null && root.IsArray)
                {
                    FixedLevels.Clear();
                    RangeRepeatingCycle.Clear();
                    DefaultRepeatingCycle.Clear();
                    DefaultBoardSizeCycle.Clear();
                    DefaultFirstTechniqueCycle.Clear();
                    DefaultHighestTechniqueCycle.Clear();

                    foreach (JSONNode node in root.AsArray)
                    {
                        var afterArtNode = node["after_art_puzzle"];
                        if (afterArtNode != null)
                        {
                            if (afterArtNode["diff"]?.Value.TryParseFloat(out var afterArtDifficulty) == true
                                && IsValidOptions(afterArtNode["first_technique"], SolveTechniqueUtility.Min, SolveTechniqueUtility.Max)
                                && IsValidOptions(afterArtNode["highest_technique"], SolveTechniqueUtility.Min, SolveTechniqueUtility.Max)
                                && IsValidOptions(afterArtNode["board_size"], 1, 10))
                                AfterArtPuzzle = ReadFixedConfig(afterArtNode, 0, afterArtDifficulty);
                            continue;
                        }

                        if (!IsValidConfigEntry(node)) continue;

                        var remoteFixedNode = node["fixed"];
                        if (remoteFixedNode != null && remoteFixedNode.IsArray)
                            DesignDataHolder.Instance?.FixedLevelSequenceData?.ApplyRemoteConfig(remoteFixedNode);

                        var diffNode = node["diff"];
                        if (diffNode != null)
                        {
                            if (!diffNode.Value.TryParseFloat(out var diff)) continue;

                            if (node["range"] != null && node["range"].IsArray)
                            {
                                var rangeNode = node["range"].AsArray;
                                if (rangeNode.Count == 2)
                                {
                                    var start = rangeNode[0].AsInt;
                                    var end = rangeNode[1].AsInt;
                                    for (var lvl = start; lvl <= end; lvl++)
                                    {
                                        FixedLevels.Add(ReadFixedConfig(node, lvl, diff));
                                    }
                                }
                            }

                            if (node["arr"] != null && node["arr"].IsArray)
                            {
                                foreach (JSONNode val in node["arr"].AsArray)
                                {
                                    FixedLevels.Add(ReadFixedConfig(node, val.AsInt, diff));
                                }
                            }
                        }

                        var rangeRepeatingCycleNode = node["range_repeating_cycle"];
                        if (rangeRepeatingCycleNode != null && rangeRepeatingCycleNode.IsArray)
                        {
                            var rangeArray = rangeRepeatingCycleNode.AsArray;
                            if (rangeArray.Count == 2)
                            {
                                var min = rangeArray[0].AsInt;
                                var max = rangeArray[1].AsInt;

                                if (node["diff_cycle"] != null && node["diff_cycle"].IsArray)
                                {
                                    if (!TryReadFloatList(node["diff_cycle"], out var difficulties)) continue;
                                    var config = new RangeRepeatingConfig
                                    {
                                        Min = min,
                                        Max = max,
                                        DifficultCycle = difficulties,
                                        FirstTechniqueCycle = ReadOptionsCycle(node["first_technique_cycle"]),
                                        HighestTechniqueCycle = ReadOptionsCycle(node["highest_technique_cycle"]),
                                        BoardSizeCycle = ReadOptionsCycle(node["board_size_cycle"])
                                    };
                                    RangeRepeatingCycle.Add(config);
                                }
                            }
                        }

                        if (node["default_repeating_cycle"] != null && node["default_repeating_cycle"].IsArray)
                        {
                            if (!TryReadFloatList(node["default_repeating_cycle"], out var difficulties)) continue;
                            DefaultRepeatingCycle = difficulties;
                            DefaultBoardSizeCycle = ReadOptionsCycle(node["board_size_cycle"]);
                            DefaultFirstTechniqueCycle = ReadOptionsCycle(node["first_technique_cycle"]);
                            DefaultHighestTechniqueCycle = ReadOptionsCycle(node["highest_technique_cycle"]);
                        }
                    }

                    CreateCaches();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelDifficultConfig] Parse failed: {e.Message}");
            }
        }

        public float GetDifficultForLevel(int level)
        {
            if (level <= 0)
                return 0;

            if (_fixedLevelDict is { Count: 0 })
                CreateCaches();

            if (_fixedLevelDict.TryGetValue(level, out var fixedConfig))
                return fixedConfig.Difficulty;

            var rangeConfig = RangeRepeatingCycle.Find(config => level >= config.Min && level <= config.Max);
            if (rangeConfig != null && rangeConfig.DifficultCycle is { Count: > 0 })
            {
                var cycleIndex = GetCycleIndex(level, rangeConfig.Min, rangeConfig.DifficultCycle.Count);
                return rangeConfig.DifficultCycle[cycleIndex];
            }

            if (DefaultRepeatingCycle != null && DefaultRepeatingCycle.Count > 0)
            {
                var cycleIndex = GetCycleIndex(level, 1, DefaultRepeatingCycle.Count);
                return DefaultRepeatingCycle[cycleIndex];
            }

            return 0;
        }

        public FixedConfig GetSelectionForLevel(int level)
        {
            if (level <= 0) return default;

            if (_fixedLevelDict is { Count: 0 })
                CreateCaches();

            if (_fixedLevelDict.TryGetValue(level, out var fixedConfig))
                return fixedConfig;

            var result = new FixedConfig
            {
                Level = level,
                Difficulty = GetDifficultForLevel(level)
            };

            var rangeConfig = RangeRepeatingCycle.Find(config => level >= config.Min && level <= config.Max);
            if (rangeConfig != null && rangeConfig.DifficultCycle is { Count: > 0 })
            {
                var cycleIndex = GetCycleIndex(level, rangeConfig.Min, rangeConfig.DifficultCycle.Count);
                result.FirstTechnique = GetCycleValue(rangeConfig.FirstTechniqueCycle, cycleIndex);
                result.HighestTechnique = GetCycleValue(rangeConfig.HighestTechniqueCycle, cycleIndex);
                result.BoardSize = GetCycleValue(rangeConfig.BoardSizeCycle, cycleIndex);
            }
            else if (DefaultRepeatingCycle is { Count: > 0 })
            {
                var cycleIndex = GetCycleIndex(level, 1, DefaultRepeatingCycle.Count);
                result.BoardSize = GetCycleValue(DefaultBoardSizeCycle, cycleIndex);
                result.FirstTechnique = GetCycleValue(DefaultFirstTechniqueCycle, cycleIndex);
                result.HighestTechnique = GetCycleValue(DefaultHighestTechniqueCycle, cycleIndex);
            }

            return result;
        }

        private void CreateCaches()
        {
            _fixedLevelDict = new();
            foreach (var config in FixedLevels)
            {
                _fixedLevelDict[config.Level] = config;
            }
        }

        private static FixedConfig ReadFixedConfig(JSONNode node, int level, float difficulty)
        {
            return new FixedConfig
            {
                Level = level,
                Difficulty = difficulty,
                FirstTechnique = ReadOptions(node["first_technique"]),
                HighestTechnique = ReadOptions(node["highest_technique"]),
                BoardSize = ReadOptions(node["board_size"])
            };
        }

        private static int GetCycleIndex(int level, int rangeMin, int cycleCount)
        {
            return (level - rangeMin) % cycleCount;
        }

        private static bool IsValidConfigEntry(JSONNode node)
        {
            if (!node.IsObject
                || (node["diff"] == null
                    && node["range_repeating_cycle"] == null
                    && node["default_repeating_cycle"] == null
                    && node["fixed"] == null))
                return false;

            if (!IsValidOptions(node["first_technique"], SolveTechniqueUtility.Min, SolveTechniqueUtility.Max)
                || !IsValidOptions(node["highest_technique"], SolveTechniqueUtility.Min, SolveTechniqueUtility.Max)
                || !IsValidOptions(node["board_size"], 1, 10))
                return false;

            var diffCycle = node["diff_cycle"];
            var defaultCycle = node["default_repeating_cycle"];
            var cycleCount = diffCycle != null && diffCycle.IsArray
                ? diffCycle.Count
                : defaultCycle != null && defaultCycle.IsArray
                    ? defaultCycle.Count
                    : 0;

            return IsValidOptionsCycle(node["first_technique_cycle"], SolveTechniqueUtility.Min, SolveTechniqueUtility.Max, cycleCount)
                   && IsValidOptionsCycle(node["highest_technique_cycle"], SolveTechniqueUtility.Min, SolveTechniqueUtility.Max, cycleCount)
                   && IsValidOptionsCycle(node["board_size_cycle"], 4, 10, cycleCount);
        }

        private static bool TryReadFloatList(JSONNode node, out List<float> values)
        {
            values = new List<float>();
            if (node == null || !node.IsArray || node.Count == 0) return false;
            foreach (JSONNode value in node.AsArray)
            {
                if (!value.Value.TryParseFloat(out var number)) return false;
                values.Add(number);
            }
            return true;
        }

        private static bool IsValidOptions(JSONNode node, int allowedMin, int allowedMax)
        {
            if (node == null) return true;
            if (!node.IsArray) return false;

            foreach (JSONNode value in node.AsArray)
            {
                if (!value.IsNumber
                    || value.AsInt < allowedMin
                    || value.AsInt > allowedMax)
                    return false;
            }

            return true;
        }

        private static bool IsValidOptionsCycle(JSONNode node, int allowedMin, int allowedMax, int expectedCount)
        {
            if (node == null) return true;
            if (!node.IsArray) return false;
            if (node.Count == 0) return true;
            if (expectedCount == 0 || node.Count != expectedCount) return false;
            foreach (JSONNode options in node.AsArray)
                if (!IsValidOptions(options, allowedMin, allowedMax)) return false;
            return true;
        }

        private static List<int> ReadOptions(JSONNode node)
        {
            var result = new List<int>();
            if (node == null || !node.IsArray) return result;
            foreach (JSONNode value in node.AsArray)
                if (!result.Contains(value.AsInt))
                    result.Add(value.AsInt);
            return result;
        }

        private static List<IntOptions> ReadOptionsCycle(JSONNode node)
        {
            var result = new List<IntOptions>();
            if (node == null) return result;
            foreach (JSONNode options in node.AsArray)
                result.Add(new IntOptions { Options = ReadOptions(options) });
            return result;
        }

        private static List<int> GetCycleValue(List<IntOptions> cycle, int index)
        {
            return cycle != null && index >= 0 && index < cycle.Count ? cycle[index]?.Options : null;
        }

        [Serializable]
        public class IntOptions
        {
            public List<int> Options = new();
        }

        [Serializable]
        public struct FixedConfig
        {
            public int Level;
            public float Difficulty;
            public List<int> FirstTechnique;
            public List<int> HighestTechnique;
            public List<int> BoardSize;
        }

        [Serializable]
        public class RangeRepeatingConfig
        {
            public int Min;
            public int Max;
            public List<float> DifficultCycle = new();
            public List<IntOptions> FirstTechniqueCycle = new();
            public List<IntOptions> HighestTechniqueCycle = new();
            public List<IntOptions> BoardSizeCycle = new();
        }
    }

    [Serializable]
    public struct PuzzlePoolEntry
    {
        public string Tag;
        public List<float> Range;
    }

    [Serializable]
    public class PuzzlePoolConfig
    {
        public List<PuzzlePoolEntry> PoolEntries = new();

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var root = JSON.Parse(json);
                if (root != null)
                {
                    var entries = new List<PuzzlePoolEntry>();
                    foreach (var key in root.Keys)
                    {
                        var node = root[key];
                        if (node == null || !node.IsArray || node.Count != 2) continue;

                        var range = new List<float>();
                        var isValid = true;
                        foreach (JSONNode val in node.AsArray)
                        {
                            if (!val.Value.TryParseFloat(out var score))
                            {
                                isValid = false;
                                break;
                            }
                            range.Add(score);
                        }
                        if (!isValid) continue;
                        entries.Add(new PuzzlePoolEntry { Tag = key, Range = range });
                    }
                    PoolEntries = entries;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PuzzlePoolConfig] Parse failed: {e.Message}");
            }
        }
    }

    [CreateAssetMenu(fileName = "LevelConfigs", menuName = "Design/LevelConfigs")]
    public class LevelConfigs : ScriptableObject
    {
        public bool LevelDdaEnabled;
        public PuzzlePoolConfig PuzzlePool = new();
        public LevelDifficultAdjustRule AdjustRule = new();
        public LevelDifficultConfig DifficultConfigs = new();
        public AutoHintConfig AutoHint = new();

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var root = JSON.Parse(json);
                if (root == null || !root.IsObject) return;

                var dda = root["level_dda_enabled"];
                if (dda != null && !dda.IsNull)
                    LevelDdaEnabled = dda.AsBool;

                var adjustRule = root["level_difficult_adjust_rule"];
                if (adjustRule != null && !adjustRule.IsNull)
                    AdjustRule.ApplyRemoteConfig(adjustRule.ToString());

                var difficultConfig = root["level_difficult_config"];
                if (difficultConfig != null && !difficultConfig.IsNull)
                    DifficultConfigs.ApplyRemoteConfig(difficultConfig.ToString());

                var puzzlePool = root["puzzle_pool"];
                if (puzzlePool != null && !puzzlePool.IsNull)
                    PuzzlePool.ApplyRemoteConfig(puzzlePool.ToString());

                var autoHint = root["auto_hint"];
                if (autoHint != null && !autoHint.IsNull)
                    AutoHint.ApplyRemoteConfig(autoHint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelConfigs] Parse failed: {e.Message}");
                return;
            }

            Debug.Log($"[LevelConfigs] Configs applied. DDA Enabled: {LevelDdaEnabled}, " +
                      $"Adjust Rule Window: {AdjustRule.Window}, " +
                      $"Fixed Levels Count: {DifficultConfigs.FixedLevels.Count}, " +
                      $"Range Repeating Cycle Count: {DifficultConfigs.RangeRepeatingCycle.Count}, " +
                      $"Default Repeating Cycle Count: {DifficultConfigs.DefaultRepeatingCycle.Count}, " +
                      $"Puzzle Pools: {PuzzlePool.PoolEntries.Count}");
        }

        public float GetBaseLevelDifficulty(int level)
        {
            if (DifficultConfigs == null) return 0;

            return GetBaseLevelSelection(level).Difficulty;
        }

        public LevelDifficultConfig.FixedConfig GetBaseLevelSelection(int level)
        {
            if (DifficultConfigs == null) return default;

            var prevLevelIsArt = UserData.Instance?.GameplayData?.PrevLevelIsArt == true;
            if (prevLevelIsArt)
                return DifficultConfigs.AfterArtPuzzle;

            return DifficultConfigs.GetSelectionForLevel(level);
        }

        public string GetDifficultyTag(float finalDifficulty)
        {
            if (PuzzlePool == null || PuzzlePool.PoolEntries == null || PuzzlePool.PoolEntries.Count == 0)
            {
                return "normal";
            }

            foreach (var entry in PuzzlePool.PoolEntries)
            {
                var range = entry.Range;
                if (range != null && range.Count == 2)
                {
                    if (finalDifficulty >= range[0] && finalDifficulty < range[1])
                    {
                        return entry.Tag;
                    }
                }
            }

            return "normal";
        }

        public string GetAddressableTag(string poolKey)
        {
            if (string.IsNullOrEmpty(poolKey)) return "level_normal";
            if (poolKey.Contains("tutorial")) return "level_tutorial";
            if (poolKey.Contains("easy")) return "level_easy";
            if (poolKey.Contains("normal")) return "level_normal";
            if (poolKey.Contains("challenge")) return "level_challenge";
            if (poolKey.Contains("master")) return "level_master";
            return "level_normal";
        }

        public LevelType GetLevelTypeFromPoolKey(string poolKey)
        {
            if (string.IsNullOrEmpty(poolKey)) return LevelType.Normal;
            if (poolKey.Contains("tutorial")) return LevelType.Tutorial;
            if (poolKey.Contains("easy")) return LevelType.Easy;
            if (poolKey.Contains("normal")) return LevelType.Normal;
            if (poolKey.Contains("challenge")) return LevelType.Challenge;
            if (poolKey.Contains("master")) return LevelType.Master;
            return LevelType.Normal;
        }

        public string GetFinalPoolKey(int level, float offset)
        {
            var baseDiff = GetBaseLevelDifficulty(level);
            var finalDiff = baseDiff + offset;
            var poolKey = GetDifficultyTag(finalDiff);

            return poolKey;
        }

        public (string targetTag, float minScore, float maxScore) GetLevelTagAndRange(int level, float offset)
        {
            var poolKey = GetFinalPoolKey(level, offset);
            var addrTag = GetAddressableTag(poolKey);

            var minScore = 0f;
            var maxScore = 9999999f;

            var entry = PuzzlePool.PoolEntries.Find(e => e.Tag == poolKey);
            if (entry.Range != null && entry.Range.Count == 2)
            {
                minScore = entry.Range[0];
                maxScore = entry.Range[1];
            }

            return (addrTag, minScore, maxScore);
        }

        public string GetDebugDiffText(int levelNum, float offset)
        {
            var baseScore = GetBaseLevelDifficulty(levelNum);
            var effectiveOffset = LevelDdaEnabled ? offset : 0;
            var finalScore = baseScore > 0 ? (baseScore + effectiveOffset) : baseScore;

            int baseIndex = 1;
            int finalIndex = 1;
            string basePoolTag = "unknown";
            string finalPoolTag = "unknown";

            if (PuzzlePool != null && PuzzlePool.PoolEntries != null)
            {
                var entries = PuzzlePool.PoolEntries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var r = entries[i].Range;
                    if (r != null && r.Count == 2)
                    {
                        if (baseScore >= r[0] && baseScore < r[1])
                        {
                            baseIndex = i + 1;
                            basePoolTag = entries[i].Tag;
                        }
                        if (finalScore >= r[0] && finalScore < r[1])
                        {
                            finalIndex = i + 1;
                            finalPoolTag = entries[i].Tag;
                        }
                    }
                }
            }

            // If base score is 0, it means it's a fixed level with no offset applied, set offset display to 0.
            float displayOffset = baseScore > 0 ? offset : 0f;

            return $"Level Diff: {finalPoolTag} ({finalScore})\n" +
                   $"Base Diff: {basePoolTag} ({baseScore})\n" +
                   $"User Diff: {finalIndex - baseIndex} ({displayOffset})";
        }
    }
}
