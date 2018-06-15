using System;
using System.Collections.Generic;
using UnityEngine;

public class AgentCamera1 : MonoBehaviour
{

    public CameraFormat m_eAgentCamFormat;
    public int m_iResWidth = 40, m_iResHeight = 16;

    private Material m_RenderDepthMat;
    private Material m_RenderGrayScale;
    private RenderTexture m_AgentRT;
    private Texture2D m_AgentTex;
    private Camera m_AgentCam;
    private Rect m_Rect;

    //The Agent Camera Resolution
    private int m_iChannelByteCount;
    private byte[] m_bSingleChannelRawData;

    public enum CameraFormat
    {
        Channel8Bit,
        Channel16bit,
        Channel32Bit,
        WhiteBlack8Bit,
    };

    // Use this for initialization
    void Start()
    {
        m_AgentCam = GetComponent<Camera>();
        m_AgentCam.depthTextureMode = DepthTextureMode.Depth;
        m_RenderDepthMat = new Material(Shader.Find("Hidden/RenderDepth"));
        m_RenderGrayScale = new Material(Shader.Find("Hidden/RenderGrayScale"));

        RenderTextureFormat rtFormat = RenderTextureFormat.Default;
        TextureFormat texFormat = TextureFormat.ARGB4444;
        switch (m_eAgentCamFormat)
        {
            case CameraFormat.Channel8Bit:
                rtFormat = RenderTextureFormat.ARGB32;
                texFormat = TextureFormat.RGBA32;
                m_iChannelByteCount = 1;
                break;
            case CameraFormat.Channel16bit:
                rtFormat = RenderTextureFormat.ARGBHalf;
                texFormat = TextureFormat.RGBAHalf;
                m_iChannelByteCount = 2;
                break;
            case CameraFormat.Channel32Bit:
                rtFormat = RenderTextureFormat.ARGBFloat;
                texFormat = TextureFormat.RGBAFloat;
                m_iChannelByteCount = 4;
                break;
            case CameraFormat.WhiteBlack8Bit:
                rtFormat = RenderTextureFormat.ARGB32;
                texFormat = TextureFormat.RGBA32;
                m_iChannelByteCount = 1;
                break;
        }
        m_AgentRT = new RenderTexture(m_iResWidth, m_iResHeight, 32, rtFormat);
        m_AgentCam.targetTexture = m_AgentRT;
        m_AgentCam.enabled = false;

        //The Texture used to read RenderTexture data back should be the same format to it.
        //Only these format support ReadPixels: RGBA32, ARGB32, RGB24, RGBAFloat or RGBAHalf 
        m_AgentTex = new Texture2D(m_iResWidth, m_iResHeight, texFormat, false);

        m_bSingleChannelRawData = new byte[m_iResWidth * m_iResHeight * m_iChannelByteCount];

        m_Rect = new Rect(transform.position.z / 100.0f * (m_iResWidth+10), 
            transform.position.y/100.0f * (m_iResHeight + 10) + 30,
            m_iResWidth, m_iResHeight);
    }

    // Update is called once per frame
    void Update()
    {
        GetDepth();
    }

    void GetDepth()
    {
        var prevActiveRT = RenderTexture.active;

        RenderTexture.active = m_AgentRT;

        m_AgentCam.Render();

        m_AgentTex.ReadPixels(new Rect(0, 0, m_iResWidth, m_iResHeight), 0, 0);
        m_AgentTex.Apply();

        RenderTexture.active = prevActiveRT;
    }

    //The RenderTexture use 4 channels, and due to RenderDepthMaterail,
    //4 channels carry the same value, so only one channel is needed. 
    bool GetDepthRawData(ref byte[] singleChannelData)
    {
        if (singleChannelData.Length != m_iResWidth * m_iResHeight * m_iChannelByteCount)
            return false;
        var data4Channel = m_AgentTex.GetRawTextureData();
        int picker = 0;
        for (; picker < m_iResWidth * m_iResHeight; picker++)
        {
            Array.Copy(data4Channel, picker * (m_iChannelByteCount * 4), singleChannelData,
                picker * m_iChannelByteCount, m_iChannelByteCount);
        }
        return true;
    }

    //Since Brain observation need vector of float
    //Channel8Bit and Channel16bit will be significant slow due to convertion
    //but they will be faster on GPU. So total impact should be test.
    //And now using ReadPixels to send brain the GPU data is not good especially
    //when you use GPU to train!
    public bool GetDepthDataFloat(ref float[] observationVector)
    {
        if (observationVector.Length != m_iResWidth * m_iResHeight)
            return false;
        GetDepthRawData(ref m_bSingleChannelRawData);
        switch (m_eAgentCamFormat)
        {
            case CameraFormat.Channel8Bit:
            case CameraFormat.WhiteBlack8Bit:
                var i = 0;
                foreach (var bit in m_bSingleChannelRawData)
                {
                    observationVector[i] = bit / 255.0f; //or bit/255.0f;
                    i++;
                }
                break;
            case CameraFormat.Channel16bit:
                for (int j = 0; j < observationVector.Length; j++)
                {
                    observationVector[j] = SystemHalf.Half.ToHalf(m_bSingleChannelRawData[j * 2]);
                }
                break;
            case CameraFormat.Channel32Bit:
                Buffer.BlockCopy(m_bSingleChannelRawData, 0, observationVector, 0,
                    m_iResWidth * m_iResHeight * m_iChannelByteCount);
                break;
        }
        return true;
    }

    private void OnGUI()
    {
        //Draw the RenderTexture for debug use
        Graphics.DrawTexture(m_Rect, m_AgentRT);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_eAgentCamFormat != CameraFormat.WhiteBlack8Bit)
            Graphics.Blit(source, destination, m_RenderDepthMat);
        else
            Graphics.Blit(source, destination, m_RenderGrayScale);
    }

}
