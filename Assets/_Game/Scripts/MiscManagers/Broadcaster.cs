using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Broadcaster : SingletonComponent<Broadcaster>
{
    private List<IBroadcastSubscriber> subscribers = new ();
    private Dictionary<string, List<IBroadcastSubscriber>> eventSubscribers = new ();

    public void DispatchEvent(string eventId, object data = null)
    {
        if (eventSubscribers.ContainsKey(eventId))
        {
            eventSubscribers[eventId].ForEach(subscriber => subscriber.OnEvent(eventId, data));
        }
    }
    
    public void AddSubscriber(IBroadcastSubscriber subscriber)
    {
        if (subscribers == null)
            subscribers = new List<IBroadcastSubscriber>();
        else if (subscribers.Contains(subscriber))
            return;
        
        subscribers.Add(subscriber);
        var subscribeEvents = subscriber.GetSubscribeEvents();
        subscribeEvents.ForEach(eventId =>
        {
            AddEventSubscriber(eventId, subscriber);
        });
    }
    
    public void RemoveSubscriber(IBroadcastSubscriber subscriber)
    {
        if (subscribers == null || !subscribers.Remove(subscriber))
            return;
        
        var subscribeEvents = subscriber.GetSubscribeEvents();
        subscribeEvents.ForEach(eventId =>
        {
            RemoveEventSubscriber(eventId, subscriber);
        });
    }
    
    private void Start()
    {
        subscribers = new List<IBroadcastSubscriber>(FindObjectsOfType<MonoBehaviour>(true).OfType<IBroadcastSubscriber>());
        foreach (var subscriber in subscribers)
        {
            if (subscriber is IBroadcastSubscriberManual)
                continue;
            
            var subscribeEvents = subscriber.GetSubscribeEvents();
            subscribeEvents.ForEach(eventId =>
            {
                AddEventSubscriber(eventId, subscriber);
            });
        }
    }

    private void AddEventSubscriber(string eventId, IBroadcastSubscriber subscriber)
    {
        if (!eventSubscribers.ContainsKey(eventId))
        {
            eventSubscribers[eventId] = new List<IBroadcastSubscriber>();
        }
        
        eventSubscribers[eventId].Add(subscriber);
    }
    
    private void RemoveEventSubscriber(string eventId, IBroadcastSubscriber subscriber)
    {
        if (eventSubscribers.ContainsKey(eventId))
        {
            eventSubscribers[eventId].Remove(subscriber);
        }
    }
    
    #region Sub-Classed

    public class EventId
    {
        public const string ON_USING_BOOSTER = "ON_USING_BOOSTER";
        public const string ON_USING_REVIVE = "ON_USING_REVIVE";
    }
    #endregion
}

public interface IBroadcastSubscriber
{
    List<string> GetSubscribeEvents();
    void OnEvent(string eventId, object data);
}

public interface IBroadcastSubscriberManual : IBroadcastSubscriber // This subscriber is not auto added to the broadcaster at start
{
    
}