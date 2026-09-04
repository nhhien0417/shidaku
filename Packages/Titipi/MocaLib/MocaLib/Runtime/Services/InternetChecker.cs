using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

namespace Titipi.MocaLib.Runtime.Services
{
    public class InternetChecker : MonoBehaviour
    {
        public Action<bool> OnInternetStatusChanged;

        public bool IsInternetAvailable { get; private set; } = false;

        private const string TEST_URL = "https://clients3.google.com/generate_204";

        private float _checkInterval = 10f;
        private int _timeout = 5;

        private bool _lastStatus;
        private bool _enabled;
        private Coroutine _checkRoutine;

        public void Enable(float checkInterval = 10f, int timeout = 5)
        {
            if (_checkRoutine != null) return;

            _checkInterval = checkInterval;
            _timeout = timeout;
            _lastStatus = true;
            _enabled = true;

            _checkRoutine = StartCoroutine(CheckInternetLoop());
        }

        public void Disable()
        {
            _enabled = false;

            if (_checkRoutine == null) return;

            OnInternetStatusChanged = null;

            StopCoroutine(_checkRoutine);
            _checkRoutine = null;
        }

        private IEnumerator CheckInternetLoop()
        {
            while (true)
            {
                yield return CheckInternetConnection(isConnected =>
                {
                    IsInternetAvailable = isConnected;

                    if (isConnected != _lastStatus)
                    {
                        _lastStatus = isConnected;
                        OnInternetStatusChanged?.Invoke(isConnected);
                    }
                });

                yield return new WaitForSecondsRealtime(_checkInterval);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _checkRoutine != null)
            {
                StopCoroutine(_checkRoutine);
                _checkRoutine = null;
            }
            else if (!paused && _enabled && _checkRoutine == null)
            {
                _checkRoutine = StartCoroutine(CheckInternetLoop());
            }
        }

        private IEnumerator CheckInternetConnection(Action<bool> callback)
        {
            using var request = UnityWebRequest.Get(TEST_URL);
            request.timeout = _timeout;
            yield return request.SendWebRequest();

            var success =
                request.result == UnityWebRequest.Result.Success &&
                request.responseCode == 204;

            callback?.Invoke(success);
        }
    }
}
