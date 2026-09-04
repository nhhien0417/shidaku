using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.Storage;
using MEC;
using UnityEngine;

namespace _Game.Scripts.LiveOps
{
    // BECAREFUL: Complex logic, do not modify without understanding the implications.
    public sealed class TotalPlaytimeTracker : MonoBehaviour
    {
        private const string TotalPlaytimePrefsKey = "total_active_playtime_ms_v1";
        private const float PersistIntervalSeconds = 900f;
        private const double AfkTimeoutSeconds = 180d;

        private static TotalPlaytimeTracker _instance;

        private bool _hasFocus;
        private bool _isPaused;
        private bool _isTracking;
        private bool _isAfk;
        private bool _userPropertySyncEnabled;
        private bool _userPropertySyncPending;
        private double _segmentStartedAt;
        private double _lastActivityAt;
        private long _totalActiveMilliseconds;
        private CoroutineHandle _checkpointRoutine;

#if UNITY_EDITOR || UNITY_STANDALONE
        private Vector3 _lastMousePosition;
#endif

        public static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            var trackerObject = new GameObject(nameof(TotalPlaytimeTracker));
            trackerObject.AddComponent<TotalPlaytimeTracker>();
        }

        public static void EnableUserPropertySync()
        {
            Initialize();

            _instance._userPropertySyncEnabled = true;
            if (_instance._isTracking)
            {
                _instance.CommitAndSync();
            }
            else if (_instance._hasFocus && !_instance._isPaused)
            {
                _instance.SyncUserProperty();
            }
            else
            {
                _instance._userPropertySyncPending = true;
            }
        }

        public static void ManualSyncUserProperty()
        {
            if (_instance == null)
                return;

            UserProperty.SetTotalPlaytime(_instance._totalActiveMilliseconds);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _hasFocus = Application.isFocused;
            _isPaused = false;
            _totalActiveMilliseconds = Math.Max(0L, ObscuredPrefs.Get(TotalPlaytimePrefsKey, 0L));

            RefreshTrackingState();
        }

        private void Update()
        {
            if (!_hasFocus || _isPaused)
            {
                return;
            }

            var now = Time.realtimeSinceStartupAsDouble;
            if (HasUserActivity())
            {
                _lastActivityAt = now;

                if (_isAfk)
                {
                    _isAfk = false;
                    StartTracking();
                }

                return;
            }

            if (!_isAfk && now - _lastActivityAt >= AfkTimeoutSeconds)
            {
                EnterAfk(_lastActivityAt + AfkTimeoutSeconds);
            }
        }

        private IEnumerator<float> CheckpointRoutine()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(PersistIntervalSeconds);

                if (!_isTracking)
                {
                    yield break;
                }

                CommitAndPersist(Time.realtimeSinceStartupAsDouble);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            RefreshTrackingState();
        }

        private void OnApplicationPause(bool isPaused)
        {
            _isPaused = isPaused;
            RefreshTrackingState();
        }

        private void OnApplicationQuit()
        {
            StopTrackingAndPersist();
        }

        private void OnDestroy()
        {
            Timing.KillCoroutines(_checkpointRoutine);

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void RefreshTrackingState()
        {
            if (_hasFocus && !_isPaused)
            {
                _isAfk = false;
                StartTracking();
            }
            else
            {
                StopTrackingAndPersist();
            }
        }

        private void StartTracking()
        {
            if (_isTracking)
            {
                return;
            }

            var now = Time.realtimeSinceStartupAsDouble;
            _segmentStartedAt = now;
            _lastActivityAt = now;
            _isTracking = true;
            _checkpointRoutine = Timing.RunCoroutine(CheckpointRoutine(), Segment.SlowUpdate);

#if UNITY_EDITOR || UNITY_STANDALONE
            _lastMousePosition = Input.mousePosition;
#endif

            if (_userPropertySyncPending)
            {
                SyncUserProperty();
            }
        }

        private void StopTrackingAndPersist()
        {
            if (!_isTracking)
            {
                return;
            }

            Timing.KillCoroutines(_checkpointRoutine);
            CommitCurrentSegment(Time.realtimeSinceStartupAsDouble);
            _isTracking = false;
            Persist();
            _userPropertySyncPending = _userPropertySyncEnabled;
        }

        private void EnterAfk(double activeUntil)
        {
            if (!_isTracking)
            {
                return;
            }

            Timing.KillCoroutines(_checkpointRoutine);
            CommitCurrentSegment(activeUntil);
            _isTracking = false;
            _isAfk = true;
            PersistAndSync();
        }

        private bool HasUserActivity()
        {
            if (Input.touchCount > 0 || Input.anyKeyDown)
            {
                return true;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            var mousePosition = Input.mousePosition;
            var mouseMoved = mousePosition != _lastMousePosition;
            _lastMousePosition = mousePosition;

            if (mouseMoved || Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                return true;
            }
#endif

            return false;
        }

        private void CommitAndSync()
        {
            CommitCurrentSegment(Time.realtimeSinceStartupAsDouble);
            SyncUserProperty();
        }

        private void CommitAndPersist(double now)
        {
            CommitCurrentSegment(now);
            PersistAndSync();
        }

        private void CommitCurrentSegment(double now)
        {
            if (!_isTracking)
            {
                return;
            }

            var elapsedSeconds = Math.Max(0d, now - _segmentStartedAt);
            var elapsedMilliseconds = (long)Math.Floor(elapsedSeconds * 1000d);

            if (elapsedMilliseconds > 0)
            {
                _totalActiveMilliseconds += elapsedMilliseconds;
            }

            _segmentStartedAt = now;
        }

        private void PersistAndSync()
        {
            Persist();
            SyncUserProperty();
        }

        private void Persist()
        {
            ObscuredPrefs.Set(TotalPlaytimePrefsKey, _totalActiveMilliseconds);
            ObscuredPrefs.Save();
        }

        private void SyncUserProperty()
        {
            if (_userPropertySyncEnabled)
            {
                UserProperty.SetTotalPlaytime(_totalActiveMilliseconds);
                _userPropertySyncPending = false;
            }
        }
    }
}
