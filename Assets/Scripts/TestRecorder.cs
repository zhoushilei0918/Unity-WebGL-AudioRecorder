using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 测试录音
/// </summary>
public class TestRecorder : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Q))
        {
            Recorder.Instance.Start();
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            Recorder.Instance.Stop();
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            Recorder.Instance.PlayClip();
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            Recorder.Instance.DownloadPCM16();
        }
        if (Input.GetKeyUp(KeyCode.T))
        {
            Recorder.Instance.DownloadWAV();
        }
    }
}
