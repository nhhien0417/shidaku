using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ShowIfAttribute : PropertyAttribute
{
    public string conditionFieldName;
    public object expectedValue;

    /// <summary>
    /// Shows the field if the condition field's value matches the expected value.
    /// </summary>
    /// <param name="conditionFieldName">The name of the condition field to check.</param>
    /// <param name="expectedValue">The value required to show this field.</param>
    public ShowIfAttribute(string conditionFieldName, object expectedValue)
    {
        this.conditionFieldName = conditionFieldName;
        this.expectedValue = expectedValue;
    }
}