using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;//引用音频库


//使用前提：脚本一定要挂载在GameObject上
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField]
    public AudioType[] AudioTypes;

    private void Awake()//核心代码，保证单例唯一，且切换场景后不摧毁物体
    {
        if (instance == null)
        {
            instance = this;// 如果实例不存在，将当前对象设为实例

        }
        else
        {
            Destroy(gameObject);
            return;//不执行DontDestroyOnLoad
        }

        DontDestroyOnLoad(gameObject);//当前脚本挂载的游戏物体

        //核心，传递AudioType参数
        #region 
        foreach (AudioType type in AudioTypes)//传参至source
        {
            type.Source = gameObject.AddComponent<AudioSource>();

            type.Source.clip = type.Clip;
            type.Source.name = type.Name;
            type.Source.volume = type.Volume;
            type.Source.pitch = type.Pitch;
            type.Source.loop = type.Loop;

            if (type.Group != null)//如果音频轨道存在
            {
                type.Source.outputAudioMixerGroup = type.Group;
            }
        }
        #endregion

    }


    public void Play(string name)
    {
        foreach (AudioType type in AudioTypes)
        {
            if (type.Name == name)
            {
                type.Source.Play();
                return;
            }
        }

        Debug.LogWarning("没找到音频名字:" + name);
    }

    public void Pause(string name)
    {
        foreach (AudioType type in AudioTypes)
        {
            if (type.Name == name)
            {
                type.Source.Pause();
                return;
            }
        }

        Debug.LogWarning("没找到音频名字:" + name);
    }

    public void Stop(string name)
    {
        foreach (AudioType type in AudioTypes)
        {
            if (type.Name == name)
            {
                type.Source.Stop();
                return;
            }
        }

        Debug.LogWarning("没找到音频名字:" + name);
    }   
}
