using System;
using System.Collections.Generic;
using Common.Comparison;
using Design.Ids;
using Gameplay;
using UnityEngine;

namespace Design.Conditions
{
    public static class ConditionHelper
    {
        private static Dictionary<string, ConditionGroup> _conditionCache = new();

        public static bool ConditionsIsValidAndMet(this string conditions)
        {
            if (string.IsNullOrEmpty(conditions))
                return false;

            if (_conditionCache.TryGetValue(conditions, out var cache))
                return cache?.IsValidAndMet() ?? false;

            var parseSuccess = TryParseToConditions(conditions, out var conditionsGroup);
            _conditionCache[conditions] = conditionsGroup;

            return parseSuccess && (conditionsGroup?.IsValidAndMet() ?? false);
        }

        public static bool ConditionsIsValidAndMet(this List<Condition> conditions)
        {
            if (conditions is null or { Count: 0 })
                return false;

            foreach (var condition in conditions)
            {
                if (!condition.IsMet())
                    return false;
            }

            return true;
        }

        public static bool TryParseToConditions(string data, out ConditionGroup conditionGroup)
        {
            conditionGroup = null;

            if (string.IsNullOrEmpty(data))
                return false;

            if (!TryGetConditionMatchType(data, out var conditionMatchType, out var conditionData))
                return false;

            var conditionList = new List<Condition>();

            var cdtArray = conditionData.Split(';');
            foreach (var cdt in cdtArray)
            {
                var condition = ParseToCondition(cdt);
                if (condition == null)
                {
                    conditionList.Clear();
                    return false;
                }

                conditionList.Add(condition);
            }

            conditionGroup = new()
            {
                MatchType = conditionMatchType,
                Conditions = conditionList
            };

            return true;
        }

        public static ConditionGroup ParseToConditions(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            if (!TryGetConditionMatchType(data, out var conditionMatchType, out var conditionData))
                return null;

            var conditions = new List<Condition>();
            var cdtArray = conditionData.Split(';');
            foreach (var cdt in cdtArray)
            {
                var condition = ParseToCondition(cdt);
                if (condition != null)
                    conditions.Add(condition);
            }

            return new ()
            {
                MatchType = conditionMatchType,
                Conditions = conditions
            };
        }

        public static Condition ParseToCondition(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            var dataValues = data.Split(':');
            if (dataValues.Length != 2)
            {
                Debug.LogError($"Invalid condition string: {data}");
                return null;
            }

            var conditionId = dataValues[0];

            switch (conditionId)
            {
                case ConditionId.NormalModeLevelReached:
                {
                    if (int.TryParse(dataValues[1], out var level))
                        return new NormalModeLevelReachedCondition(level);

                    Debug.LogError($"Invalid condition value: {dataValues[1]}");
                    return null;
                }

                case ConditionId.BoosterUnlocked:
                {
                    if (ItemId.IsBooster(dataValues[1]))
                        return new BoosterUnlockedCondition(dataValues[1]);

                    Debug.LogError($"Invalid booster value: {dataValues[1]}");
                    return null;
                }

                case ConditionId.ItemAmount:
                {
                    var compareValues = GetCompareConditionValues<int>(dataValues[1]);
                    if (compareValues != null && !string.IsNullOrEmpty(compareValues.CompareTarget))
                        return new ItemAmountCondition(compareValues.CompareTarget, compareValues.CompareValue, compareValues.ComparisonType);

                    Debug.LogError($"Invalid item amount condition value: {dataValues[1]}");
                    return null;
                }

                case ConditionId.Gameplay_Normal_AttemptNum:
                {
                    var compareValues = GetCompareConditionValues<int>(dataValues[1]);
                    if (compareValues != null)
                        return new Gameplay.AttemptNumCondition(GameMode.Normal, compareValues.CompareValue, compareValues.ComparisonType);

                    Debug.LogError($"Invalid gameplay attempt num condition value: {dataValues[1]}");
                    return null;
                }

                case ConditionId.Gameplay_Normal_BoostersUsed:
                {
                    var compareValues = GetCompareConditionValues<int>(dataValues[1]);
                    if (compareValues != null)
                        return new Gameplay.BoostersUsedCondition(GameMode.Normal, compareValues.CompareValue, compareValues.ComparisonType);

                    Debug.LogError($"Invalid gameplay boosters used condition value: {dataValues[1]}");
                    return null;
                }

                case ConditionId.Gameplay_Normal_TotalPuzzleSolveTime:
                {
                    var compareValues = GetCompareConditionValues<float>(dataValues[1]);
                    if (compareValues != null)
                        return new Gameplay.TotalPuzzleSolveTimeCondition(GameMode.Normal, compareValues.CompareValue, compareValues.ComparisonType);

                    Debug.LogError($"Invalid gameplay total puzzle solve time condition value: {dataValues[1]}");
                    return null;
                }

                case ConditionId.Gameplay_Normal_LevelDifficulty:
                {
                    var compareValues = GetCompareConditionValues<float>(dataValues[1]);
                    if (compareValues != null)
                        return new Gameplay.LevelDifficultyCondition(GameMode.Normal, compareValues.CompareValue, compareValues.ComparisonType);

                    Debug.LogError($"Invalid gameplay level difficulty condition value: {dataValues[1]}");
                    return null;
                }

                default:
                    Debug.LogError($"Invalid condition id: {conditionId}");
                    return null;
            }
        }

        private static bool TryGetConditionMatchType(string data, out ConditionMatchType conditionMatchType, out string conditionData)
        {
            conditionMatchType = ConditionMatchType.All;
            conditionData = data;

            if (string.IsNullOrEmpty(data))
                return false;

            var separatorIndex = data.IndexOf('|');
            if (separatorIndex < 0)
                return true;

            if (separatorIndex == 0 || separatorIndex == data.Length - 1)
                return false;

            var matchTypeStr = data[..separatorIndex];
            conditionData = data[(separatorIndex + 1)..];

            return matchTypeStr.TryToConditionMatchType(out conditionMatchType);
        }

        private static CompareConditionValues<T> GetCompareConditionValues<T>(string data) where T : IComparable, IConvertible
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogError($"Invalid condition value: {data}");
                return null;
            }

            var targetStr = "";
            var valueStr = data;

            if (data.TryGetComparisonOperator(out var compareStr) && !string.IsNullOrEmpty(compareStr))
            {
                var parts = data.Split(compareStr);
                if (parts.Length != 2)
                {
                    Debug.LogError($"Invalid condition value: {data}");
                    return null;
                }

                targetStr = parts[0];
                valueStr = parts[1];
            }
            else
            {
                compareStr = "==";
            }

            if (!valueStr.TryParseToValue<T>(out var compareValue))
            {
                return null;
            }

            return new CompareConditionValues<T>()
            {
                CompareTarget = targetStr,
                CompareValue = compareValue,
                ComparisonType = compareStr.ToComparisonType(),
            };
        }

        private class CompareConditionValues<T> where T : IComparable, IConvertible
        {
            public ComparisonType ComparisonType;
            public string CompareTarget;
            public T CompareValue;
        }
    }
}