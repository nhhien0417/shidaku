using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public class ServerTimeManager : MonoBehaviour
    {
        private const string TAG = "ServerTimeManager";

        private const string PLAYER_PREFS_SERVER_TIME_AT_FETCH = "moca_server_time_at_fetch";
        private const string PLAYER_PREFS_REAL_TIME_AT_FETCH = "moca_real_time_at_fetch";
        private const float RESYNC_INTERVAL = 600f; // in seconds

        private bool _initialized;
        private DateTime _serverUtcAtFetch;
        private float _realtimeAtFetch;
        private Coroutine _syncRoutine;

        /// <summary>
        /// Fired once when server time becomes available (either from cache or online fetch).
        /// </summary>
        public event Action OnServerTimeReady;

        public bool IsInitialized => _initialized;

        /// <summary>
        /// Returns the current estimated UTC time based on the last server sync.
        /// Immune to device clock manipulation.
        /// </summary>
        public DateTime UtcNow
        {
            get
            {
                if (!_initialized)
                {
                    Utils.MocaLibLogWarning(TAG, "Server time not initialized yet; returning local time.");
                    return DateTime.UtcNow; // fallback
                }

                var elapsed = Time.realtimeSinceStartup - _realtimeAtFetch;
                return _serverUtcAtFetch.AddSeconds(elapsed);
            }
        }

        public void Initialize()
        {
            LoadCachedTime();
            _syncRoutine = StartCoroutine(SyncRoutine());
        }

        private void OnDestroy()
        {
            if (_syncRoutine != null)
            {
                StopCoroutine(_syncRoutine);
                _syncRoutine = null;
            }
        }

        private IEnumerator SyncRoutine()
        {
            while (true)
            {
                yield return FetchServerTime();
                yield return new WaitForSeconds(RESYNC_INTERVAL);
            }
        }

        private void LoadCachedTime()
        {
            if (PlayerPrefs.HasKey(PLAYER_PREFS_SERVER_TIME_AT_FETCH) &&
                PlayerPrefs.HasKey(PLAYER_PREFS_REAL_TIME_AT_FETCH))
            {
                var cachedServer = PlayerPrefs.GetString(PLAYER_PREFS_SERVER_TIME_AT_FETCH);
                var cachedRealtime = PlayerPrefs.GetFloat(PLAYER_PREFS_REAL_TIME_AT_FETCH);

                if (DateTime.TryParse(cachedServer, out var parsedTime))
                {
                    _serverUtcAtFetch = parsedTime;
                    _realtimeAtFetch = cachedRealtime;
                    _initialized = true;

                    Utils.MocaLibLog(TAG, $"Loaded cached server time: {_serverUtcAtFetch:u}");
                    OnServerTimeReady?.Invoke();
                }
            }
        }

        private IEnumerator FetchServerTime()
        {
            const string url = "https://api.titipigames.com/v1/time/utc_datetime";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("x-api-key", "rUoP-V6jtBz8WNdltfzR95b82LIImYYT1OSriWHG4NU");
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var json = JsonUtility.FromJson<WorldTimeApiResponse>(req.downloadHandler.text);
                    _serverUtcAtFetch = DateTime.Parse(json.utc_datetime);
                    _realtimeAtFetch = Time.realtimeSinceStartup;
                    _initialized = true;

                    // Save for offline recovery
                    PlayerPrefs.SetString(PLAYER_PREFS_SERVER_TIME_AT_FETCH, _serverUtcAtFetch.ToString("o"));
                    PlayerPrefs.SetFloat(PLAYER_PREFS_REAL_TIME_AT_FETCH, _realtimeAtFetch);
                    PlayerPrefs.Save();

                    Utils.MocaLibLog(TAG, $"Synced server time: {_serverUtcAtFetch:u}");

                    OnServerTimeReady?.Invoke();
                }
                catch (Exception ex)
                {
                    Utils.MocaLibLogWarning(TAG, $"Failed to parse server time: {ex.Message}");
                    Firebase.Crashlytics.Crashlytics.LogException(ex);
                }
            }
            else
            {
                Utils.MocaLibLogWarning(TAG, $"Request failed: {req.error}");
                Firebase.Crashlytics.Crashlytics.Log($"{TAG} - Request failed: {req.error}");
            }
        }

        [Serializable]
        private class WorldTimeApiResponse
        {
            public string utc_datetime;
        }
    }
}
