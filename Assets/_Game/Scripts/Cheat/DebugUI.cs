using System.Collections.Generic;
using _Game.Scripts.Utils;
using MEC;
using TMPro;
using UnityEngine;

namespace _Game.Scripts.Cheat
{
    public class DebugUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtCurrentDateTime;

        public DragableUI PnlContent;

        private CoroutineHandle _dateTimeCoroutine;

        private IEnumerator<float> DateTimeCoroutine()
        {
            while (true)
            {
                var now = DateTimeManager.Now;
                _txtCurrentDateTime.text = now.ToString("MM-dd-yyyy HH:mm:ss");

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
