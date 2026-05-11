using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
 
public interface IEventInfo
{
 
}
 
public class EventInfo<T,K> : IEventInfo
{
    public UnityAction<T, K> actions = delegate { };
    public EventInfo(UnityAction<T, K> action)
    {
        actions += action;
    }
}
 
public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions = delegate { };
    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}
 
public class EventInfo : IEventInfo
 
{
    public UnityAction actions = delegate { };
 
    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}
 
public class EventCenter : Singleton<EventCenter>
{
    Dictionary<GameEvent, IEventInfo> eventDict = new Dictionary<GameEvent, IEventInfo>();
 
    //触发事件
    public void EventTrigger(GameEvent gameEvent)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo).actions?.Invoke();
        }
    }
 
    public void EventTrigger<T>(GameEvent gameEvent, T value)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo<T>).actions?.Invoke(value);
        }
    }
 
    public void EventTrigger<T,K>(GameEvent gameEvent, T value1,K value2)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo<T,K>).actions?.Invoke(value1,value2);
        }
    }
 
 
    #region 添加事件监听器
    public void AddEventListener(GameEvent gameEvent, UnityAction action)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo).actions += action;
        }
        else
        {
            eventDict.Add(gameEvent, new EventInfo(action) as IEventInfo);
        }
    }
 
    public void AddEventListener<T>(GameEvent gameEvent, UnityAction<T> action)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo<T>).actions += action;
        }
        else
        {
            eventDict.Add(gameEvent, new EventInfo<T>(action) as IEventInfo);
        }
    }
 
    public void AddEventListener<T,K>(GameEvent gameEvent, UnityAction<T,K> action)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo<T,K>).actions += action;
        }
        else
        {
            eventDict.Add(gameEvent, new EventInfo<T,K>(action) as IEventInfo);
        }
    }
    #endregion
 
    #region 移除事件添加器
    public void RemoveEventListener(GameEvent gameEvent, UnityAction action)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo).actions -= action;
        }
    }
 
    public void RemoveEventListener<T>(GameEvent gameEvent, UnityAction<T> action)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo<T>).actions -= action;
        }
    }
 
    public void RemoveEventListener<T,K>(GameEvent gameEvent, UnityAction<T,K> action)
    {
        if (eventDict.ContainsKey(gameEvent))
        {
            (eventDict[gameEvent] as EventInfo<T,K>).actions -= action;
        }
    }
    #endregion
 
    //清空事件
    public void Clear()
    {
        eventDict.Clear();
    }
}