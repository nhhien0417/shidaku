using System;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    public interface IRemoteConfigService
    {
        public event Action<bool> OnFetchCompleted;

        void Initialize();
        double GetRemoteDouble(string key, double defaultValue);
        float GetRemoteFloat(string key, float defaultValue);
        long GetRemoteLong(string key, long defaultValue);
        int GetRemoteInt(string key, int defaultValue);
        bool GetRemoteBool(string key, bool defaultValue);
        string GetRemoteString(string key, string defaultValue);
    }
}
