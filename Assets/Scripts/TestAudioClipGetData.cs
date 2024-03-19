using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 用于测试 AudioClip.GetData 的替代方案 
/// </summary>
public class TestAudioClipGetData : MonoBehaviour
{
    public AudioClip m_audioClip;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && m_audioClip != null)
        {
            int samples = m_audioClip.samples;
            int channels = m_audioClip.channels;
            float length = m_audioClip.length;
            int frequency = m_audioClip.frequency;
            var coroutine = m_audioClip.GetData((r, datas) =>
            {
                StringBuilder build = new StringBuilder();
                build.AppendLine("当前的 AudioClip 读取状态：" + r);
                build.AppendLine("channels: " + channels);
                build.AppendLine("samples: " + samples);
                build.AppendLine("length: " + length);
                build.AppendLine("frequency: " + frequency);
                build.AppendLine("datas: ");
                build.AppendLine(string.Join(",", datas));
                print(build.ToString());
            });
            StartCoroutine(coroutine);
        }
    }
}
