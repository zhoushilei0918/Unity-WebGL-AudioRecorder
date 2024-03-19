using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    public void StartRecord()
    {
        Recorder.Instance.Start();
    }

    public void StopRecord()
    {
        Recorder.Instance.Stop();
    }

    public void PlayRecord()
    {
        Recorder.Instance.PlayClip();
    }

    public void DownloadPCM()
    {
        Recorder.Instance.DownloadPCM16();
    }

    public void DownloadWav()
    {
        Recorder.Instance.DownloadWAV();
    }
}
