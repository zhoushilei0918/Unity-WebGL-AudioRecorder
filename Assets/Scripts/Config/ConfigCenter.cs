using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.Events;

/// <summary>
/// 配置中心
/// 
/// 使用方式:
///     依赖 com.unity.nuget.newtonsoft-json@3.0 
///     可以将配置写在 StreamingAssets/ConfigCenter/config.json 中
///     通过 key-value 的形式进行读取
/// </summary>
public class ConfigCenter : SingletonMgr<ConfigCenter>
{
    // 配置缓存
    private Dictionary<string, string> m_configDic = new Dictionary<string, string>();

    private event UnityAction m_onLoaded;

    private bool m_loadCompleted = false;

    #region 读取配置并缓存

    protected override void OnInit()
    {
        StartCoroutine(ReadConfig());
    }

    private IEnumerator ReadConfig()
    {
        /* WebGL 下使用 UnityWebRequest 访问 StreamAsset */
        string streamAssetRoot = Application.streamingAssetsPath;
        string dir = Path.Combine(streamAssetRoot, "Config");
        string uriString = Path.Combine(dir, "config.json");

        // 调用
        Uri uri = new Uri(uriString);
        UnityWebRequest www = UnityWebRequest.Get(uri);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            JObject pairs = JsonConvert.DeserializeObject<JObject>(www.downloadHandler.text);
            foreach (var item in pairs)
            {
                if (!m_configDic.ContainsKey(item.Key))
                {
                    m_configDic.Add(item.Key, item.Value.ToString());
                }
                else
                {
                    m_configDic[item.Key] = item.Value.ToString();
                }
            }
        }
        this.m_loadCompleted = true;
        this.m_onLoaded.Invoke();
        this.m_onLoaded = null;
    }

    #endregion

    #region 获得配置值

    public static void GetAppSetting(string key, UnityAction<string> callback)
    {
        if (!ConfigCenter.Instance.m_loadCompleted)
        {
            ConfigCenter.Instance.m_onLoaded += () =>
            {
                GetAppSettingCore(key, callback);
            };
        }
        else
        {
            GetAppSettingCore(key, callback);
        }
    }

    private static void GetAppSettingCore(string key, UnityAction<string> callback)
    {
        if (ConfigCenter.Instance.m_configDic.ContainsKey(key))
        {
            callback.Invoke(ConfigCenter.Instance.m_configDic[key]);
        }
        else
        {
            callback.Invoke("");
        }
    }

    #endregion
}
