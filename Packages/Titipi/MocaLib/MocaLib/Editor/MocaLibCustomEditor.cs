using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Titipi.MocaLib.Editor
{
    [CustomEditor(typeof(Runtime.Services.MocaLib))]
    public class MocaLibCustomEditor : UnityEditor.Editor
    {
        private const string MOCALIB_ENABLE_INFO_LOG = "MOCALIB_ENABLE_INFO_LOG";

        private const string MOCALIB_AD_PROVIDER_APPLOVIN = "MOCALIB_AD_PROVIDER_APPLOVIN";
        private const string MOCALIB_AD_PROVIDER_LEVELPLAY = "MOCALIB_AD_PROVIDER_LEVELPLAY";
        private const string MOCALIB_ADMANAGER_USE_GOOGLE_UMP = "MOCALIB_ADMANAGER_USE_GOOGLE_UMP";

        private const string MOCALIB_MMP_PROVIDER_ADJUST = "MOCALIB_MMP_PROVIDER_ADJUST";
        private const string MOCALIB_MMP_PROVIDER_APPSFLYER = "MOCALIB_MMP_PROVIDER_APPSFLYER";
        private const string MOCALIB_USE_APPSFLYER_PURCHASE_CONNECTOR = "MOCALIB_USE_APPSFLYER_PURCHASE_CONNECTOR";

        private const string MOCALIB_USE_BYTEBREW = "MOCALIB_USE_BYTEBREW";
        private const string MOCALIB_USE_GAMEANALYTICS = "MOCALIB_USE_GAMEANALYTICS";
        private const string MOCALIB_USE_COST_CENTER = "MOCALIB_USE_COST_CENTER";

        private const string MOCALIB_USE_FIREBASE_LEADERBOARD = "MOCALIB_USE_FIREBASE_LEADERBOARD";
        private const string MOCALIB_USE_FIREBASE_APP_CHECK = "MOCALIB_USE_FIREBASE_APP_CHECK";

        private const string MOCALIB_USE_SERVER_TIME = "MOCALIB_USE_SERVER_TIME";

        private SerializedProperty _enableInfoLog;
        private SerializedProperty _adProvider;
        private SerializedProperty _mmpProvider;
        private SerializedProperty _useAFPurchaseConnector;
        private SerializedProperty _useByteBrew;
        private SerializedProperty _useGameAnalytics;
        private SerializedProperty _useCostCenter;
        private SerializedProperty _useFirebaseLeaderboard;
        private SerializedProperty _useFirebaseAppCheck;
        private SerializedProperty _useServerTime;

        private void OnEnable()
        {
            _enableInfoLog = serializedObject.FindProperty("_enableInfoLog");
            _adProvider = serializedObject.FindProperty("_adProvider");
            _mmpProvider = serializedObject.FindProperty("_mmpProvider");
            _useByteBrew = serializedObject.FindProperty("_useByteBrew");
            _useGameAnalytics = serializedObject.FindProperty("_useGameAnalytics");
            _useCostCenter = serializedObject.FindProperty("_useCostCenter");
            _useFirebaseLeaderboard = serializedObject.FindProperty("_useFirebaseLeaderboard");
            _useFirebaseAppCheck = serializedObject.FindProperty("_useFirebaseAppCheck");
            _useServerTime = serializedObject.FindProperty("_useServerTime");
            _useAFPurchaseConnector = serializedObject.FindProperty("_useAFPurchaseConnector");
        }

        public override void OnInspectorGUI()
        {
            var enableInfoLogPreviousValue = _enableInfoLog.boolValue;
            var adProviderPreviousIndex = _adProvider.enumValueIndex;
            var mmpProviderPreviousIndex = _mmpProvider.enumValueIndex;
            var useAfPurchaseConnectorPreviousValue = _useAFPurchaseConnector.boolValue;
            var useByteBrewPreviousValue = _useByteBrew.boolValue;
            var useGameAnalyticsPreviousValue = _useGameAnalytics.boolValue;
            var useCostCenterPreviousValue = _useCostCenter.boolValue;
            var useFirebaseLeaderboardPreviousValue = _useFirebaseLeaderboard.boolValue;
            var useFirebaseAppCheckPreviousValue = _useFirebaseAppCheck.boolValue;
            var useServerTimePreviousValue = _useServerTime.boolValue;

            base.OnInspectorGUI();
            serializedObject.Update();

            var enableInfoLogCurrentValue = _enableInfoLog.boolValue;
            var adProviderCurrentIndex = _adProvider.enumValueIndex;
            var mmpProviderCurrentIndex = _mmpProvider.enumValueIndex;
            var useAfPurchaseConnectorCurrentValue = _useAFPurchaseConnector.boolValue;
            var useByteBrewCurrentValue = _useByteBrew.boolValue;
            var useCostCenterCurrentValue = _useCostCenter.boolValue;
            var useGameAnalyticsCurrentValue = _useGameAnalytics.boolValue;
            var useFirebaseLeaderboardCurrentValue = _useFirebaseLeaderboard.boolValue;
            var useFirebaseAppCheckCurrentValue = _useFirebaseAppCheck.boolValue;
            var useServerTimeCurrentValue = _useServerTime.boolValue;

            if (enableInfoLogPreviousValue != enableInfoLogCurrentValue)
            {
                UpdateEnableInfoLogDefineSymbol(enableInfoLogCurrentValue);
            }

            if (adProviderPreviousIndex != adProviderCurrentIndex)
            {
                UpdateAdProviderDefineSymbol(adProviderCurrentIndex);
            }

            if (mmpProviderPreviousIndex != mmpProviderCurrentIndex || useAfPurchaseConnectorPreviousValue != useAfPurchaseConnectorCurrentValue)
            {
                UpdateMMPProviderDefineSymbol(mmpProviderCurrentIndex, useAfPurchaseConnectorCurrentValue);
            }

            if (useByteBrewPreviousValue != useByteBrewCurrentValue)
            {
                UpdateUseByteBrewDefineSymbol(useByteBrewCurrentValue);
            }

            if (useGameAnalyticsPreviousValue != useGameAnalyticsCurrentValue)
            {
                UpdateUseGameAnalyticsDefineSymbol(useGameAnalyticsCurrentValue);
            }

            if (useCostCenterPreviousValue != useCostCenterCurrentValue)
            {
                UpdateUseCostCenterDefineSymbol(useCostCenterCurrentValue);
            }

            if (useFirebaseLeaderboardPreviousValue != useFirebaseLeaderboardCurrentValue)
            {
                UpdateUseFirebaseLeaderBoardDefineSymbol(useFirebaseLeaderboardCurrentValue);
            }

            if (useFirebaseAppCheckPreviousValue != useFirebaseAppCheckCurrentValue)
            {
                UpdateUseFirebaseAppCheckDefineSymbol(useFirebaseAppCheckCurrentValue);
            }

            if (useServerTimePreviousValue != useServerTimeCurrentValue)
            {
                UpdateUseServerTimeDefineSymbol(useServerTimeCurrentValue);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateEnableInfoLogDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_ENABLE_INFO_LOG, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateAdProviderDefineSymbol(int index)
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.iOS })
            {
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget).Split(';').ToList();

                defines.Remove(MOCALIB_AD_PROVIDER_APPLOVIN);
                defines.Remove(MOCALIB_AD_PROVIDER_LEVELPLAY);
                defines.Remove(MOCALIB_ADMANAGER_USE_GOOGLE_UMP);

                switch (index)
                {
                    case 1:
                        defines.Add(MOCALIB_AD_PROVIDER_APPLOVIN);
                        break;
                    case 2:
                        defines.Add(MOCALIB_AD_PROVIDER_LEVELPLAY);
                        defines.Add(MOCALIB_ADMANAGER_USE_GOOGLE_UMP);
                        break;
                }

                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", defines));
            }
        }

        private void UpdateMMPProviderDefineSymbol(int index, bool useAfpc)
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.iOS })
            {
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget).Split(';').ToList();

                defines.Remove(MOCALIB_MMP_PROVIDER_ADJUST);
                defines.Remove(MOCALIB_MMP_PROVIDER_APPSFLYER);
                defines.Remove(MOCALIB_USE_APPSFLYER_PURCHASE_CONNECTOR);

                switch (index)
                {
                    case 1:
                        defines.Add(MOCALIB_MMP_PROVIDER_ADJUST);
                        break;
                    case 2:
                        defines.Add(MOCALIB_MMP_PROVIDER_APPSFLYER);
                        if (useAfpc)
                            defines.Add(MOCALIB_USE_APPSFLYER_PURCHASE_CONNECTOR);
                        break;
                }

                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", defines));
            }
        }

        private void UpdateUseByteBrewDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_USE_BYTEBREW, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateUseGameAnalyticsDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_USE_GAMEANALYTICS, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateUseCostCenterDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_USE_COST_CENTER, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateUseFirebaseLeaderBoardDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_USE_FIREBASE_LEADERBOARD, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateUseFirebaseAppCheckDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_USE_FIREBASE_APP_CHECK, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateUseServerTimeDefineSymbol(bool value)
        {
            UpdateDefineForPlatforms(MOCALIB_USE_SERVER_TIME, value, BuildTargetGroup.Android, BuildTargetGroup.iOS);
        }

        private void UpdateDefineForPlatforms(string symbol, bool shouldAdd, params BuildTargetGroup[] targetGroups)
        {
            foreach (var group in targetGroups)
            {
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget)
                                            .Split(';')
                                            .Where(d => !string.IsNullOrWhiteSpace(d))
                                            .ToList();

                defines.RemoveAll(d => d == symbol);

                if (shouldAdd)
                    defines.Add(symbol);

                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", defines));
            }
        }
    }
}
