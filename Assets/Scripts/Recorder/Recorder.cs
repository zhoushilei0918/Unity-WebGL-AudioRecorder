using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 录音相关的方法
/// </summary>
public class Recorder : SingletonMgr<Recorder>
{
    /// <summary>
    /// 当前的 AudioClip
    /// </summary>
    public AudioClip m_curAudioClip = null;

    #region 调用前端

    [DllImport("__Internal")]
    private static extern void StartRecord();

    [DllImport("__Internal")]
    private static extern void StopRecord();

    #endregion

    /// <summary>
    /// 是否正在播放录音
    /// </summary>
    public bool m_isPlay = false;

    /// <summary>
    /// 播放器
    /// </summary>
    public AudioSource m_audioSource = null;

    /// <summary>
    /// 缓存原始数据
    /// </summary>
    private string m_cacheBase64Str = null;
    private byte[] m_cacheBuffer = null;

    protected override void OnInit()
    {
        this.m_audioSource = this.gameObject.AddComponent<AudioSource>();
        this.m_audioSource.loop = false;
    }

    /// <summary>
    /// 开始播放
    /// </summary>
    public void Start()
    {
        if (!this.m_isPlay)
        {
            this.m_isPlay = true;
            this.m_curAudioClip = null;
            this.m_cacheBase64Str = null;
            this.m_cacheBuffer = null;
            StartRecord();
        }
    }

    /// <summary>
    /// 结束播放
    /// </summary>
    public void Stop()
    {
        if (this.m_isPlay)
        {
            StopRecord();
        }
    }

    /// <summary>
    /// 提供前端在结束时调用
    /// </summary>
    /// <param name="base64str">录音的 Base64 数据</param>
    private void GetAudioData(string base64str)
    {
        // 将 Base64 字符串转换为 byte[]
        byte[] data = Convert.FromBase64String(base64str);
        this.m_cacheBase64Str = base64str;
        this.m_cacheBuffer = data;

        string guid = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6);
        string clipName = $"Record-{guid}";

        // 创建 AudioClip
        AudioClip audioClipTmp = AudioClip.Create(clipName, data.Length / 2, 1, 16000, false);
        audioClipTmp.SetPCM16Data(data);
        this.m_curAudioClip = audioClipTmp;

        // 标记处理完成
        this.m_isPlay = false;
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void PlayClip()
    {
        if (this.m_isPlay)
        {
            Debug.Log("正在录音，请结束录音后播放！");
            return;
        }

        if (this.m_curAudioClip != null)
        {
            this.m_audioSource.clip = this.m_curAudioClip;
            this.m_audioSource.Play();
        }
        else
        {
            Debug.Log("当前无录音片段！");
        }
    }

    public void DownloadPCM16()
    {
        if (this.m_isPlay)
        {
            Debug.Log("正在录音，请结束录音后播放！");
            return;
        }
        if (!string.IsNullOrEmpty(this.m_cacheBase64Str) && this.m_curAudioClip != null)
        {
            DownloadPlugin.DownloadFile(this.m_cacheBase64Str, $"{this.m_curAudioClip.name}.pcm");
        }
        else
        {
            Debug.Log("当前无录音片段！");
        }
    }

    public void DownloadWAV()
    {
        if (this.m_isPlay)
        {
            Debug.Log("正在录音，请结束录音后播放！");
            return;
        }
        if (this.m_cacheBuffer != null && this.m_curAudioClip != null)
        {
            int samples = this.m_cacheBuffer.Length;

            // 获得头部部分
            byte[] headerByte = RecorderTool.GetWAVHeader(samples, 1, 16000);
            int lenHeader = headerByte.Length;
            int lenBody = this.m_cacheBuffer.Length;
            int len = lenHeader + lenBody;

            // 合并为一个
            byte[] all = new byte[len];
            Buffer.BlockCopy(headerByte, 0, all, 0, lenHeader);
            Buffer.BlockCopy(this.m_cacheBuffer, 0, all, lenHeader * sizeof(byte), lenBody);

            string wavBase64str = Convert.ToBase64String(all);
            DownloadPlugin.DownloadFile(wavBase64str, $"{this.m_curAudioClip.name}.wav");
        }
        else
        {
            Debug.Log("当前无录音片段！");
        }
    }
}
