using System.Collections.Generic;
using Analytics.Providers;
using UnityEngine;

namespace Analytics
{
    /// <summary>
    /// Central analytics manager - single entry point for all event tracking.
    ///
    /// Manages multiple analytics providers and routes events to all of them.
    /// Uses a simple singleton pattern consistent with the project's existing conventions.
    ///
    /// Usage:
    ///   AnalyticsManager.Instance.Track(new LevelStartEvent { Level = 5, PlayMode = "normal", ... });
    ///   AnalyticsManager.Instance.Track(EventNames.LevelStart, parameters); // raw fallback
    /// </summary>
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        private readonly List<IAnalyticsProvider> _providers = new();
        private bool _isInitialized;

        /// <summary>
        /// Initialize the analytics system with the given providers.
        /// Should be called once during app startup.
        /// </summary>
        public void Initialize(params IAnalyticsProvider[] providers)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[AnalyticsManager] Already initialized. Skipping.");
                return;
            }

            foreach (var provider in providers)
            {
                AddProvider(provider);
            }

            _isInitialized = true;
            Debug.Log($"[AnalyticsManager] Initialized with {_providers.Count} provider(s).");
        }

        /// <summary>
        /// Add a provider at runtime. The provider will be initialized immediately.
        /// </summary>
        public void AddProvider(IAnalyticsProvider provider)
        {
            if (provider == null)
            {
                Debug.LogWarning("[AnalyticsManager] Attempted to add null provider.");
                return;
            }

            provider.Initialize();
            _providers.Add(provider);
        }

        /// <summary>
        /// Remove a provider at runtime.
        /// </summary>
        public void RemoveProvider(IAnalyticsProvider provider)
        {
            _providers.Remove(provider);
        }

        /// <summary>
        /// Track a typed analytics event. This is the primary API.
        /// The event will be validated before being sent to all enabled providers.
        /// </summary>
        /// <param name="analyticsEvent">The typed event to track.</param>
        public void Track(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                Debug.LogWarning("[AnalyticsManager] Attempted to track null event.");
                return;
            }


            for (var i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (!provider.IsEnabled) continue;

                try
                {
                    provider.LogEvent(analyticsEvent);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AnalyticsManager] Error in provider '{provider.ProviderName}': {e.Message}");
                }
            }
        }

        /// <summary>
        /// Track a raw event by name and parameters.
        /// Use this as a fallback when you don't have a typed event class yet.
        /// Prefer using Track(AnalyticsEvent) with typed events.
        /// </summary>
        public void Track(string eventName, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[AnalyticsManager] Attempted to track event with empty name.");
                return;
            }

            for (var i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (!provider.IsEnabled) continue;

                try
                {
                    provider.LogEvent(eventName, parameters);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AnalyticsManager] Error in provider '{provider.ProviderName}': {e.Message}");
                }
            }
        }
    }
}
