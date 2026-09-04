using UnityEngine;

public static class CustomLogger
{
    public static void Log<T>(this T _, object message)
    {
        Log(typeof(T).Name, message);
    }
    
    public static void Log(string tag, object message)
    {
        Debug.Log($"[{tag}] {message}");
    }

    public static void LogWarning<T>(this T _, object message)
    {
        LogWarning(typeof(T).Name, message);
    }
    
    public static void LogWarning(string tag, object message)
    {
        Debug.LogWarning($"[{tag}] {message}");
    }
    
    public static void LogError<T>(this T _, object message)
    {
        LogError(typeof(T).Name, message);
    }
    
    public static void LogError(string tag, object message)
    {
        Debug.LogError($"[{tag}] {message}");
    }
}