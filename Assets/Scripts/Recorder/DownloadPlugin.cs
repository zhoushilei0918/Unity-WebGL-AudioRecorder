using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 前端下载工具
/// </summary>
public class DownloadPlugin
{
    #region 调用前端

    [DllImport("__Internal")]
    private static extern void Download(string base64str, string fileName);

    #endregion

    public static void DownloadFile(string base64str, string fileName)
    {
        Download(base64str, fileName);
    }
}
