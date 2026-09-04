using System;

namespace Common.Comparison
{
    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        LessThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        NotEqual
    }

    public static class ComparisonHelper
    {
        public static readonly string[] ComparisonOperator =
        {
            "==",
            "<=",
            ">=",
            "!=",
            ">",
            "<"
        };

        public static bool TryGetComparisonOperator(this string str, out string comparisonOperator)
        {
            comparisonOperator = null;

            if (string.IsNullOrEmpty(str))
                return false;

            foreach (var op in ComparisonOperator)
            {
                if (str.IndexOf(op, StringComparison.Ordinal) < 0)
                    continue;

                comparisonOperator = op;
                return true;
            }

            return false;
        }

        public static bool GetResult<T>(this ComparisonType comparisonType, T compareValue, T compareWithValue) where T : IComparable
        {
            return comparisonType switch
            {
                ComparisonType.Equal => compareValue.Equals(compareWithValue),
                ComparisonType.GreaterThan => compareValue.CompareTo(compareWithValue) > 0,
                ComparisonType.LessThan => compareValue.CompareTo(compareWithValue) < 0,
                ComparisonType.LessThanOrEqual => compareValue.CompareTo(compareWithValue) <= 0,
                ComparisonType.GreaterThanOrEqual => compareValue.CompareTo(compareWithValue) >= 0,
                ComparisonType.NotEqual => !compareValue.Equals(compareWithValue),
                _ => false
            };
        }

        public static ComparisonType ToComparisonType(this string str)
        {
            return str switch
            {
                "==" or ":" => ComparisonType.Equal,
                ">" => ComparisonType.GreaterThan,
                "<" => ComparisonType.LessThan,
                "<=" => ComparisonType.LessThanOrEqual,
                ">=" => ComparisonType.GreaterThanOrEqual,
                "!=" => ComparisonType.NotEqual,
                _ => ComparisonType.Equal
            };
        }
    }
}