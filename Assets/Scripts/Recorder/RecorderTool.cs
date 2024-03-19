using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RecorderTool
{
    /// <summary>
    /// 将 ushort 的 float 数组转成 16 位的 Btye 数组
    /// </summary>
    public static byte[] UShortToByte(float[] datas)
    {
        int count = datas.Length;
        byte[] byteDatas = new byte[count << 1];
        for (int i = 0; i < count; i++)
        {
            byteDatas[i * 2] = (byte)(datas[i] / ushort.MaxValue);
            byteDatas[i * 2 + 1] = (byte)(datas[i]);
        }
        return byteDatas;
    }

    /// <summary>
    /// 将 Btye 数组转成 16 位的 ushort 的 float 数组
    /// </summary>
    public static float[] ByteToUShort(byte[] datas)
    {
        int len = datas.Length >> 1;
        float[] floatDatas = new float[len];
        for (int i = 0; i < len; i++)
        {
            floatDatas[i] = ((short)((datas[i * 2 + 1] << 8) | datas[i * 2])) / (ushort.MaxValue * 1.0f);
        }
        return floatDatas;
    }

    public static byte[] GetWAVHeader(int samples, int channels, int frequency)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Wav 头部
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samples * 2);
                byte[] wavBytes = stream.ToArray();
                return wavBytes;
            }
        }
    }

    public static byte[] PCM16toWAV(float[] datas, int channels, int frequency)
    {
        int samples = datas.Length;
        /* 创建一个 WAV 文件，在 PCM 上加一个 Wav 的头部 */
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Wav 头部
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samples * 2);

                // PCM 数据
                for (int i = 0; i < datas.Length; i++)
                {
                    /* 乘上 short.MaxValue 可以转为 16 位数的 1 和 0 的数据 */
                    writer.Write((short)(datas[i] * short.MaxValue));
                }
                byte[] wavBytes = stream.ToArray();
                return wavBytes;
            }
        }
    }
}
