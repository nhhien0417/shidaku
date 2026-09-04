using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEngine;
using UnityEngine.Localization.Tables;

public sealed class AutoSmartStringPullWindow : EditorWindow
{
    private readonly List<CollectionState> m_Collections = new List<CollectionState>();
    private Vector2 m_ScrollPosition;
    private bool m_DisableSmartWhenNoPlaceholder = true;

    [MenuItem("Tools/Localization/Google Sheets Smart Puller")]
    public static void Open()
    {
        var window = GetWindow<AutoSmartStringPullWindow>("Smart Puller");
        window.minSize = new Vector2(620f, 420f);
        window.RefreshCollections();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshCollections();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawActions();
        DrawCollections();
        DrawFooter();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Google Sheets Smart Puller", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this window instead of Unity's built-in Pull when you want pulled strings to auto-apply IsSmart from {...} placeholders without adding a Google Sheet column.",
            MessageType.Info);

        m_DisableSmartWhenNoPlaceholder = EditorGUILayout.ToggleLeft(
            new GUIContent(
                "Disable Smart when no placeholder is found",
                "On: IsSmart exactly follows the pulled string. Off: only auto-enables Smart, never auto-disables existing Smart entries."),
            m_DisableSmartWhenNoPlaceholder);
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            RefreshCollections();

        if (GUILayout.Button("Select All", GUILayout.Width(90f)))
            SetAllSelected(true);

        if (GUILayout.Button("Select None", GUILayout.Width(90f)))
            SetAllSelected(false);

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(m_Collections.Count == 0 || !m_Collections.Any(item => item.Selected)))
        {
            if (GUILayout.Button("Pull Selected + Apply Smart", GUILayout.Width(210f)))
                PullSelected();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCollections()
    {
        EditorGUILayout.Space(6f);
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        if (m_Collections.Count == 0)
        {
            EditorGUILayout.HelpBox("No String Table Collections with Google Sheets extensions were found.", MessageType.Warning);
        }

        foreach (var item in m_Collections)
            DrawCollection(item);

        EditorGUILayout.EndScrollView();
    }

    private void DrawCollection(CollectionState item)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        item.Selected = EditorGUILayout.Toggle(item.Selected, GUILayout.Width(20f));
        EditorGUILayout.ObjectField(item.Collection, typeof(StringTableCollection), false);

        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.IndentLevelScope())
        {
            foreach (var extension in item.Extensions)
                DrawExtension(item.Collection, extension);
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawExtension(StringTableCollection collection, GoogleSheetsExtension extension)
    {
        string validationError = AutoSmartStringGoogleSheetsPuller.GetValidationError(collection, extension);
        bool valid = string.IsNullOrEmpty(validationError);

        EditorGUILayout.LabelField(
            valid ? "Ready" : "Invalid",
            valid ? "Google Sheets extension is configured." : validationError);

        string spreadsheetId = string.IsNullOrEmpty(extension.SpreadsheetId)
            ? "<missing>"
            : extension.SpreadsheetId;

        EditorGUILayout.LabelField("Spreadsheet", spreadsheetId);
        EditorGUILayout.LabelField("Sheet Id", extension.SheetId.ToString());
        EditorGUILayout.LabelField("Columns", string.Join(", ", extension.Columns.Where(column => column != null).Select(column => column.Column)));
    }

    private void DrawFooter()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"Collections: {m_Collections.Count} | Selected: {m_Collections.Count(item => item.Selected)}", EditorStyles.miniLabel);
    }

    private void PullSelected()
    {
        var selectedCollections = m_Collections
            .Where(item => item.Selected)
            .Select(item => item.Collection)
            .ToArray();

        if (selectedCollections.Length == 0)
            return;

        var result = AutoSmartStringGoogleSheetsPuller.PullCollections(
            selectedCollections,
            m_DisableSmartWhenNoPlaceholder);

        AutoSmartStringGoogleSheetsPuller.LogResult(result);
        ShowNotification(new GUIContent($"Pulled {result.PulledExtensionCount}, updated {result.UpdatedEntryCount} Smart entries"));
    }

    private void RefreshCollections()
    {
        var previousSelection = m_Collections.ToDictionary(
            item => item.Collection != null ? item.Collection.TableCollectionName : string.Empty,
            item => item.Selected);

        m_Collections.Clear();

        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            var extensions = collection.Extensions.OfType<GoogleSheetsExtension>().ToList();
            if (extensions.Count == 0)
                continue;

            bool selected;
            if (!previousSelection.TryGetValue(collection.TableCollectionName, out selected))
                selected = true;

            m_Collections.Add(new CollectionState(collection, extensions, selected));
        }

        Repaint();
    }

    private void SetAllSelected(bool selected)
    {
        foreach (var item in m_Collections)
            item.Selected = selected;
    }

    private sealed class CollectionState
    {
        public readonly StringTableCollection Collection;
        public readonly List<GoogleSheetsExtension> Extensions;
        public bool Selected;

        public CollectionState(StringTableCollection collection, List<GoogleSheetsExtension> extensions, bool selected)
        {
            Collection = collection;
            Extensions = extensions;
            Selected = selected;
        }
    }
}