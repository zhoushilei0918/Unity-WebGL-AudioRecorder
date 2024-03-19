using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// AudioClip 拓展
/// 
/// 使用 AudioClip.GetData(Unity<bool, float[]>) 代替原来的 AudioClip.GetData(float[], int)
/// </summary>
public static class AudioClipExtensions
{
    /// <summary>
    /// 由于在 WebGL 上使用 AudioClip.GetData 没有效果，所以采用这种预先生成的方式进行处理
    /// </summary>
    public static IEnumerator GetData(this AudioClip clip, UnityAction<bool, float[]> callback)
    {
        /* 无回调就直接不处理 */
        if (callback == null)
        {
            yield return new WaitForEndOfFrame();
        }

#if !UNITY_WEBGL
        float[] datas = new float[clip.samples];
        bool isSuccess = clip.GetData(datas, 0);
        callback.Invoke(isSuccess, datas);
#else
        /* WebGL 下使用 UnityWebRequest 访问 StreamAsset */
        string streamAssetRoot = Application.streamingAssetsPath;
        string dir = Path.Combine(streamAssetRoot, "AudioData");    // 获得所在文件夹
        string clipFileName = clip.name;                            // 获得文件名称
        clipFileName += ".txt";
        string uriString = Path.Combine(dir, clipFileName);

        // 调用
        Uri uri = new Uri(uriString);
        UnityWebRequest www = UnityWebRequest.Get(uri);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            if (callback != null)
            {
                callback.Invoke(false, new float[0]);
            }
        }
        else
        {
            string content = www.downloadHandler.text;
            /*
             * [0] AudioClip 的采样长度 
             * [1] 单/双通道
             * [2] 片段总时长
             * [3] 频率
             * [4] 数据
             */
            string[] array = content.Split('\n');
            string dataString = array[4].Trim('\r');
            int samples = clip.samples;
            if (samples == 0)
            {
                samples = Convert.ToInt32(array[0].Trim('\r')); // [0]: AudioClip 的采样长度 
            }
            float[] datas = new float[samples];
            string[] floatArray = dataString.Split(',');
            for (int i = 0; i < samples; ++i)
            {
                float f = Convert.ToSingle(floatArray[i]);
                datas[i] = f;
            }
            callback.Invoke(true, datas);
        }
#endif
    }

    /// <summary>
    /// 将 AudioClip 转为 Wav 的 byte[] 数据
    /// ！！！！可以读取处理过的，不能处理实时录音的！！！！
    /// </summary>
    public static void ToWav(this AudioClip clip, UnityAction<byte[]> callback)
    {
        if (callback == null)
        {
            return;
        }

        // 读取
        int channels = clip.channels;
        int frequency = clip.frequency;
        clip.GetData((result, datas) =>
        {
            byte[] wavBytes=  RecorderTool.PCM16toWAV(datas, channels, frequency);
            callback.Invoke(wavBytes);
        });
    }

    /// <summary>
    /// PCM 的 byte 数据写入到 AudioClip
    /// </summary>
    public static void SetPCM16Data(this AudioClip clip, byte[] datas)
    {
        float[] floatDatas = RecorderTool.ByteToUShort(datas);
        clip.SetData(floatDatas, 0);
        //Debug.Log(string.Join(",", floatDatas));
    }
}
