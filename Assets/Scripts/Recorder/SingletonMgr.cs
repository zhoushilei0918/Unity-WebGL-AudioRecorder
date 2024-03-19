using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单例基类
/// </summary>
public class SingletonMgr<T> : MonoBehaviour
    where T : SingletonMgr<T>
{
    private static T s_instance = null;

    public static T Instance
    {
        get
        {
            if (s_instance == null)
            {
                GameObject gameObject = new GameObject();
                gameObject.hideFlags = HideFlags.DontSave;
                gameObject.transform.position = Vector3.zero;
                gameObject.name = typeof(T).Name;
                DontDestroyOnLoad(gameObject);

                s_instance = gameObject.AddComponent<T>();
                s_instance.OnInit();
            }
            return s_instance;
        }
    }

    protected virtual void OnInit() { }
}
