using Unity.VisualScripting;
using UnityEngine;

public class StartBGM : MonoBehaviour
{
    public bool isStartBgm;
    public string bgmName;
    private void Start()
    {
        if (isStartBgm)
        {
            AudioManager.instance.Play(bgmName);
        }
    }

}