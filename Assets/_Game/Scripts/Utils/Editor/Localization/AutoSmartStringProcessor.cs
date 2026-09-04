using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class AutoSmartStringUtility
{
    public static int ApplyToAllCollections(bool disableSmartWhenNoPlaceholder = true)
    {
        int updatedCount = 0;
        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            updatedCount += ApplyToCollection(collection, disableSmartWhenNoPlaceholder);
            EditorUtility.SetDirty(collection.SharedData);
        }

        AssetDatabase.SaveAssets();
        return updatedCount;
    }

    public static int ApplyToCollection(StringTableCollection collection, bool disableSmartWhenNoPlaceholder = true)
    {
        if (collection == null)
            return 0;

        int updatedCount = 0;
        foreach (var table in collection.StringTables)
        {
            bool tableChanged = false;
            foreach (var entry in table.Values)
            {
                bool shouldBeSmart = IsSmartString(entry.LocalizedValue);
                if (!disableSmartWhenNoPlaceholder && !shouldBeSmart)
                    continue;

                if (entry.IsSmart == shouldBeSmart)
                    continue;

                entry.IsSmart = shouldBeSmart;
                updatedCount++;
                tableChanged = true;
            }

            if (tableChanged)
                EditorUtility.SetDirty(table);
        }

        return updatedCount;
    }

    public static bool IsSmartString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != '{')
                continue;

            if (i + 1 < value.Length && value[i + 1] == '{')
            {
                i++;
                continue;
            }

            for (int j = i + 1; j < value.Length; j++)
            {
                if (value[j] != '}')
                    continue;

                if (j + 1 < value.Length && value[j + 1] == '}')
                {
                    j++;
                    continue;
                }

                return j > i + 1;
            }
        }

        return false;
    }
}

public sealed class AutoSmartStringPullResult
{
    public int PulledExtensionCount { get; set; }
    public int UpdatedEntryCount { get; set; }
    public int FailedExtensionCount { get; set; }
    public List<string> Messages { get; } = new List<string>();
}

public static class AutoSmartStringGoogleSheetsPuller
{
    private const bool DefaultDisableSmartWhenNoPlaceholder = true;

    [MenuItem("Tools/Localization/Google Sheets/Pull Selected + Auto Tag Smart Strings", true)]
    public static bool CanPullSelected()
    {
        return Selection.GetFiltered<StringTableCollection>(SelectionMode.Assets).Length > 0;
    }

    [MenuItem("Tools/Localization/Google Sheets/Pull Selected + Auto Tag Smart Strings")]
    public static void PullSelected()
    {
        var collections = Selection.GetFiltered<StringTableCollection>(SelectionMode.Assets);
        var result = PullCollections(collections, DefaultDisableSmartWhenNoPlaceholder);
        LogResult(result);
    }

    [MenuItem("Tools/Localization/Google Sheets/Pull All + Auto Tag Smart Strings")]
    public static void PullAll()
    {
        var result = PullCollections(
            LocalizationEditorSettings.GetStringTableCollections().ToArray(),
            DefaultDisableSmartWhenNoPlaceholder);
        LogResult(result);
    }

    public static AutoSmartStringPullResult PullCollections(
        IEnumerable<StringTableCollection> collections,
        bool disableSmartWhenNoPlaceholder)
    {
        var result = new AutoSmartStringPullResult();

        foreach (var collection in collections.Where(collection => collection != null))
        {
            foreach (var googleExtension in collection.Extensions.OfType<GoogleSheetsExtension>())
            {
                string validationError = GetValidationError(collection, googleExtension);
                if (!string.IsNullOrEmpty(validationError))
                {
                    result.Messages.Add(validationError);
                    continue;
                }

                try
                {
                    PullExtension(collection, googleExtension);
                    result.PulledExtensionCount++;
                    result.UpdatedEntryCount += AutoSmartStringUtility.ApplyToCollection(
                        collection,
                        disableSmartWhenNoPlaceholder);
                }
                catch (System.Exception exception)
                {
                    EditorUtility.ClearProgressBar();
                    result.FailedExtensionCount++;
                    result.Messages.Add($"{collection.TableCollectionName}: {exception.Message}");
                    Debug.LogException(exception);
                }
            }
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    public static string GetValidationError(StringTableCollection collection, GoogleSheetsExtension googleExtension)
    {
        if (googleExtension == null)
            return $"{collection.TableCollectionName}: missing Google Sheets extension.";

        if (googleExtension.SheetsServiceProvider == null)
            return $"{collection.TableCollectionName}: missing Google Sheets Service Provider.";

        if (string.IsNullOrEmpty(googleExtension.SpreadsheetId))
            return $"{collection.TableCollectionName}: missing Spreadsheet Id.";

        var columns = googleExtension.Columns.Where(column => column != null).ToList();
        if (columns.Count == 0 || columns.Count(column => column is IPullKeyColumn) != 1)
            return $"{collection.TableCollectionName}: Google Sheets columns must include exactly one Key column.";

        return null;
    }

    private static void PullExtension(StringTableCollection collection, GoogleSheetsExtension googleExtension)
    {
        var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider)
        {
            SpreadSheetId = googleExtension.SpreadsheetId
        };

        googleSheets.PullIntoStringTableCollection(
            googleExtension.SheetId,
            collection,
            googleExtension.Columns,
            googleExtension.RemoveMissingPulledKeys,
            null,
            true);
    }

    public static void LogResult(AutoSmartStringPullResult result)
    {
        foreach (string message in result.Messages)
            Debug.LogWarning($"[Localization] {message}");

        Debug.Log($"[Localization] Pulled {result.PulledExtensionCount} Google Sheets extensions, failed {result.FailedExtensionCount}, and updated {result.UpdatedEntryCount} Smart String entries.");
    }
}
