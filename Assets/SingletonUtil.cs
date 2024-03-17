using UnityEngine;

public class SingletonUtil<T> : MonoBehaviour where T : SingletonUtil<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        Instance = this as T;
    }

    protected virtual void OnEnable()
    {
        Instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        Instance = null;
    }
}
