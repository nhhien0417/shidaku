using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-1)]
public class UnbiasedTimeByHttp : MonoBehaviour
{
    private static UnbiasedTimeByHttp _instance;
    public static UnbiasedTimeByHttp Instance => _instance;

    public Action<bool, DateTime> OnTimeReceive;

    public DateTime DateTime => _lastNetworkTime.AddMilliseconds(_timeSinceLastUpdate);
    public bool IsUpToDate { get; private set; }

    private ulong _timeSinceLastUpdate => (ulong)(Time.unscaledTime * 1000) - _lastUpdateNetworkTime;

    private DateTime _lastNetworkTime;
    private ulong _lastUpdateNetworkTime;

    private bool _isRefreshingAsync;
    private ulong _refreshOnFocusThreshold = 1000; // in milliseconds
    private int _currentUrlHttpsIndex = 0;
    
    private static readonly string[] SYNC_URL_LIST = new string[]
    {
        "https://www.cloudflare.com/cdn-cgi/trace",
        "http://google.com",
        "https://timer-lhh-smqs.web.app"
    };
    private static readonly string[] ASYNC_URL_LIST = new string[]
    {
        "https://www.cloudflare.com/cdn-cgi/trace",
        "https://google.com",
        "https://timer-lhh-smqs.web.app",
    };
    
    public void RefreshDateTime()
    {
        var index = 0;
        while (index < SYNC_URL_LIST.Length && !RefreshDateTime(SYNC_URL_LIST[index]))
        {
            index++;
        }
        
        #if UNITY_EDITOR
        if (index < SYNC_URL_LIST.Length)
            Debug.Log($"[UnbiasedTimeByHttp] Get server time successfully with url: {SYNC_URL_LIST[index]}");
        #endif
    }

    private bool RefreshDateTime(string url)
    {
        try
        {
            var request = HttpWebRequest.Create(url);
            request.Timeout = 4000;
            var response = request.GetResponse();
            var networkTime = response.Headers["date"];
            response.Close();

            UpdateDateTime(networkTime);
            return true;
        }
        catch (WebException webException)
        {
            Debug.LogWarning($"[UnbiasedTimeByHttp] Get server time has web-exception: {webException.Message} - with status-code: {webException.Status}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UnbiasedTimeByHttp] Error: {e.Message}");
        }

        return false;
    }

    public async Task RefreshDateTimeAsync()
    {
        if (_isRefreshingAsync)
            return;
        _isRefreshingAsync = true;

        var url = ASYNC_URL_LIST[_currentUrlHttpsIndex];
        var request = UnityWebRequest.Get(url);
        request.SendWebRequest();
        while (!request.isDone)
        {
            await Task.Delay(100);
        }

        var needStartAnotherRequest = false;
        switch (request.result)
        {
            case UnityWebRequest.Result.Success:
                try
                {
                    var networkTime = request.GetResponseHeader("date");
                    UpdateDateTime(networkTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnbiasedTimeByHttp] Error: {e.Message}");
                }
                break;

            default:
                if (_currentUrlHttpsIndex < ASYNC_URL_LIST.Length - 1)
                    needStartAnotherRequest = true;
                _currentUrlHttpsIndex = (_currentUrlHttpsIndex + 1) % ASYNC_URL_LIST.Length;
                Debug.LogWarning($"[UnbiasedTimeByHttp] Get server time has error: {request.error} - with status-code: {request.result}");
                break;
        }

        _isRefreshingAsync = false;
        
        if (needStartAnotherRequest)
            await RefreshDateTimeAsync();
    }

    private async void StartSyncDateTime()
    {
        try
        {
            var delayEachCheck = new int[]
            {
                1, 1, 2, 5, 10, 15, 30
            };
            var checkIndex = 0;
        
            while (!IsUpToDate)
            {
                await RefreshDateTimeAsync();
                if (!IsUpToDate)
                {
                    await Task.Delay(delayEachCheck[checkIndex] * 1000);
                    if (checkIndex < delayEachCheck.Length - 1)
                        checkIndex++;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UnbiasedTimeByHttp] SyncDateTime Error: {e.Message}");
        }
    }

    private void UpdateDateTime(string networkTime)
    {
        _lastNetworkTime = DateTime.ParseExact(networkTime, "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AdjustToUniversal);
        _lastUpdateNetworkTime = (ulong)(Time.unscaledTime * 1000);
        IsUpToDate = true;
        OnTimeReceive?.Invoke(true, this.DateTime);

        Debug.Log($"[UnbiasedTimeByHttp] Datetime updated: UTC {this.DateTime} - UTCLocal {DateTime.UtcNow}");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        _lastNetworkTime = DateTime.UtcNow;
        _lastUpdateNetworkTime = (ulong)(Time.unscaledTime * 1000);
        IsUpToDate = false;

        StartSyncDateTime();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (_timeSinceLastUpdate >= _refreshOnFocusThreshold)
                RefreshDateTimeAsync();
        }
    }
}
