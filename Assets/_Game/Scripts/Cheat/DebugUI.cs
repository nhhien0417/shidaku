using System;
using System.Collections.Generic;
using _Game.Scripts.Utils;
using Gameplay;
using UserDataPack;
using MEC;
using TMPro;
using UnityEngine;

namespace _Game.Scripts.Cheat
{
    public class DebugUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtCurrentDateTime;
        [SerializeField] private TextMeshProUGUI _txtLevelDifficulty;

        public DragableUI PnlContent;

        private CoroutineHandle _dateTimeCoroutine;

        private IEnumerator<float> DateTimeCoroutine()
        {
            while (true)
            {
                // DateTime
                var now = DateTimeManager.Now;
                _txtCurrentDateTime.text = now.ToString("MM-dd-yyyy HH:mm:ss");

                // Level Difficulty
                var levelConfigs = Design.DesignDataHolder.Instance != null ? Design.DesignDataHolder.Instance.LevelConfigs : null;
                if (levelConfigs != null && GameplayManager.CurrentMode == GameMode.Normal && UserData.Instance != null && UserData.Instance.GameplayData != null)
                {
                    int levelNum = UserData.Instance.GameplayData.CurrentGameplayLevel;
                    float offset = UserData.Instance.GameplayData.UserDifficulty;
                    _txtLevelDifficulty.text = levelConfigs.GetDebugDiffText(levelNum, offset);
                }
                else
                {
                    _txtLevelDifficulty.text = "";
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void OnEnable()
        {
            _dateTimeCoroutine = Timing.RunCoroutine(DateTimeCoroutine());
        }

        private void OnDisable()
        {
            Timing.KillCoroutines(_dateTimeCoroutine);
        }
    }
}
