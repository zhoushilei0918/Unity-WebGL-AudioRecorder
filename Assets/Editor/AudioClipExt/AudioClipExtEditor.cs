using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AudioClipExtEditor
{
    [MenuItem("Assets/Extensions/CreateAudioData", true)]
    private static bool Verify()
    {
        UnityEngine.Object selectedObj = Selection.activeObject;
        // 有选中
        if (selectedObj == null)
        {
            return false;
        }
        // 不能多选
        if (Selection.objects.Length > 1)
        {
            return false;
        }
        // 类型为 AudioClip
        if (!(selectedObj is AudioClip))
        {
            return false;
        }
        return true;
    }

    [MenuItem("Assets/Extensions/CreateAudioData", false, 1)]
    private static void CreateAudioData()
    {
        UnityEngine.Object selectedObj = Selection.activeObject;
        AudioClip clip = selectedObj as AudioClip;

        /* 确保传入的是本地资源 */
        if (!AssetDatabase.IsMainAsset(clip))
        {
            Debug.LogWarning("The AudioClip is not a main asset!");
            return;
        }

        int samples = clip.samples;             // AudioClip 的采样长度
        int channels = clip.channels;           // 单/双通道
        float length = clip.length;             // 片段总时长
        int frequency = clip.frequency;         // 频率

        // 数据
        float[] datas = new float[samples];
        clip.GetData(datas, 0);
        string datasString = string.Join(",", datas);

        string streamAssetRoot = Application.streamingAssetsPath;
        string clipFileName = $"{clip.name}.txt";
        string dir = Path.Combine(streamAssetRoot, "AudioData");
        string dataPath = Path.Combine(dir, clipFileName);

        Debug.Log("input: " + clip.name);
        Debug.Log("output: " + dataPath);

        // 如果不存在创建目录文件
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 判断是否存在这个文件
        if (!File.Exists(dataPath))
        {
            using (File.Create(dataPath)) { }
        }
        else
        {
            Debug.LogError("已经有重名的 clip 存在，无法创建数据，请修改 clip 的名称为唯一！");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(samples.ToString());
        builder.AppendLine(channels.ToString());
        builder.AppendLine(length.ToString());
        builder.AppendLine(frequency.ToString());
        builder.AppendLine(datasString.ToString());

        /* Unity 仅支持 UTF-8 */
        File.WriteAllText(dataPath, builder.ToString(), Encoding.UTF8);

        // 刷新一下
        AssetDatabase.Refresh();
    }
}
