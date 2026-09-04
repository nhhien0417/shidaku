using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Monitors internet connectivity with minimal performance impact.
/// Polls Application.internetReachability (very cheap) at a low frequency,
/// and only runs an actual DNS ping when reachability status changes.
/// </summary>
public class InternetChecker : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static InternetChecker Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Tooltip("How often (seconds) to poll Application.internetReachability")]
    [SerializeField] private float pollInterval = 1f;

    [Tooltip("How often (seconds) to do a real DNS ping even if reachability hasn't changed. Keeps IsInternetAvailable always up to date.")]
    [SerializeField] private float periodicPingInterval = 120f;

    [Tooltip("Do a real DNS ping check on Start to set the initial state")]
    [SerializeField] protected bool checkOnStart = true;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired on every status change. Parameter is true = connected.</summary>
    public event Action<bool> OnInternetStatusChanged;

    // ── Public state ─────────────────────────────────────────────────────────
    /// <summary>Last known internet availability (updated after each DNS ping).</summary>
    public bool IsInternetAvailable { get; private set; }

    // ── Private ──────────────────────────────────────────────────────────────
    private NetworkReachability _lastReachability;
    private bool _isPinging;          // guard – avoid overlapping pings
    private bool _lastPingFailed;
    private Coroutine _pollCoroutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _lastReachability = Application.internetReachability;
    }

    protected virtual IEnumerator Start()
    {
        if (checkOnStart)
            TriggerPing();

        StartChecker();

        yield return null;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        StopChecker();
    }

    /// <summary>Re-check immediately when the app window regains focus.</summary>
    protected virtual void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            TriggerPing();
    }

    /// <summary>Re-check immediately when the app resumes from background (mobile).</summary>
    protected virtual void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
            TriggerPing();
    }

    // ── Poll coroutine ───────────────────────────────────────────────────────
    /// <summary>
    /// Runs forever, waking up every <see cref="pollInterval"/> seconds.
    /// Only reads Application.internetReachability – near-zero cost.
    /// Also runs a real DNS ping every <see cref="periodicPingInterval"/> seconds
    /// to keep <see cref="IsInternetAvailable"/> accurate even when reachability is stable.
    /// </summary>
    private IEnumerator PollReachability()
    {
        var wait = new WaitForSecondsRealtime(pollInterval);
        float timeSinceLastPing = 0f;

        while (true)
        {
            yield return wait;

            timeSinceLastPing += pollInterval;
            NetworkReachability current = Application.internetReachability;

            bool reachabilityChanged = current != _lastReachability;
            bool periodicPingDue = timeSinceLastPing >= periodicPingInterval;

            if (reachabilityChanged)
            {
                Debug.Log($"[InternetChecker] Reachability changed: {_lastReachability} → {current}");
                _lastReachability = current;
            }

            if (reachabilityChanged || periodicPingDue || _lastPingFailed)
            {
                timeSinceLastPing = 0f;
                TriggerPing();
            }
        }
    }

    // ── Ping logic ───────────────────────────────────────────────────────────
    /// <summary>Starts an async DNS ping if one is not already running.</summary>
    protected async void TriggerPing()
    {
        if (_isPinging) return;
        _isPinging = true;

        try
        {
            bool result = await NetworkUtils.CheckInternetConnectionAvailable();
            _lastPingFailed = !result;
            ApplyResult(result);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InternetChecker] Ping error: {ex.Message}");
            _lastPingFailed = true;
        }
        finally
        {
            _isPinging = false;
        }
    }

    private void ApplyResult(bool isConnected)
    {
        // Suppress duplicate events
        if (isConnected == IsInternetAvailable) return;

        IsInternetAvailable = isConnected;
        Debug.Log($"[InternetChecker] Internet status → {(isConnected ? "CONNECTED" : "DISCONNECTED")}");

        OnInternetStatusChanged?.Invoke(isConnected);
    }

    // ── Public API ───────────────────────────────────────────────────────────
    /// <summary>
    /// Manually trigger an internet check outside of the normal polling cycle.
    /// </summary>
    public void ForceCheck() => TriggerPing();

    protected void StartChecker()
    {
        StopChecker();
        _pollCoroutine = StartCoroutine(PollReachability());
    }

    protected void StopChecker()
    {
        if (_pollCoroutine != null)
            StopCoroutine(_pollCoroutine);
    }
}

