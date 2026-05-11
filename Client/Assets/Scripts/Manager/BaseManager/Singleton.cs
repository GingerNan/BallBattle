/// <summary>
/// 纯C#单例基类
/// 不需要挂载到GameObject上，可以直接调用
/// </summary>
public class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();
 
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }
 
    protected Singleton() { }
}