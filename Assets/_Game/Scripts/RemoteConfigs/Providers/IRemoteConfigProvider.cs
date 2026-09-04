
using System;

namespace RemoteConfigs
{
    public interface IRemoteConfigProvider
    {
        public void Initialize(Action<bool> onRemoteConfigFetched, Action<bool> onRemoteConfigRefetched);
        public T GetConfig<T>(string key, T defaultValue) where T : IConvertible;
    }
}
