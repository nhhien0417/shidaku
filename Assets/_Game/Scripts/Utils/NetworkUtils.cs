using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class NetworkUtils
{
    private static readonly string[] DnsUrls = new string[]
    {
        "https://cp.cloudflare.com/generate_204",
        "https://connectivitycheck.gstatic.com/generate_204",
        "https://clients3.google.com/generate_204",
        "https://8.8.4.4",
    };
    
    public static async Task<bool> CheckInternetConnectionAvailable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
            return false;
        
        var pingTasks = new List<Task<bool>>();
        foreach (var url in DnsUrls)
        {
            pingTasks.Add(PingSingleUrlAsync(url));
        }
        
        while (pingTasks.Count > 0)
        {
            var firstCompletedTask = await Task.WhenAny(pingTasks);
            if (await firstCompletedTask)
                return true;
            
            pingTasks.Remove(firstCompletedTask);
        }
        
        return false;
    }
    
    private static async Task<bool> PingSingleUrlAsync(string url)
    {
        using UnityWebRequest request = UnityWebRequest.Head(url);
        request.timeout = 3;
            
        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        Debug.Log($"[NetworkUtils] Ping {url} successful with result {request.result}");
        
        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            return false;
        }

        return true;
    }
}
