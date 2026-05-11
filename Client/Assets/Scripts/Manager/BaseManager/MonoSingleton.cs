using UnityEngine;
 
/// <summary>
/// MonoBehaviour单例基类
/// 需要挂载到GameObject上使用
/// </summary>
public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
 
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                return null;
            }
 
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
 
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";
 
                        // 可选：让单例对象在场景切换时不被销毁
                        //DontDestroyOnLoad(singletonObject);
                    }
                }
 
                return _instance;
            }
        }
    }
 
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            //DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"Another instance of {typeof(T)} already exists. Destroying this one.");
            Destroy(gameObject);
        }
    }
 
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
 
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}