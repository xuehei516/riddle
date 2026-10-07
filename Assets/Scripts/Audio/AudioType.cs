using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;//引用音频库

[System.Serializable]//自定义类得加前缀System
public class AudioType//无需继承自MonoBehaviour，音频数据库
{
    [HideInInspector]
    public AudioSource Source;
    public AudioClip Clip;
    public AudioMixerGroup Group;

    public string Name;

    [Range(0f,1f)]//用滑动条控制
    public float Volume;

    [Range(0.1f, 5f)]
    public float Pitch;
    public bool Loop;
}
