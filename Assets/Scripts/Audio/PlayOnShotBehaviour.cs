using UnityEngine;

public class PlayOnShotBehaviour : StateMachineBehaviour
{
    [Tooltip("在AudioManager中配置的音效名称")]
    [SerializeField] private string soundName;

    [Tooltip("是否在进入动画状态时播放音效")]
    [SerializeField] private bool playOnEnter = true;

    [Tooltip("是否在退出动画状态时播放音效")]
    [SerializeField] private bool playOnExit = false;

    [Tooltip("是否在动画更新过程中播放音效（需配合延迟使用）")]
    [SerializeField] private bool playOnUpdate = false;

    [Tooltip("音效播放延迟时间（秒）")]
    [SerializeField, Range(0f, 2f)] private float delay = 0f;

    private float timer = 0f;          // 计时器，用于延迟播放
    private bool hasPlayed = false;   // 标记音效是否已播放

    /// <summary>
    /// 进入动画状态时调用
    /// </summary>
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 重置计时器和播放状态
        timer = 0f;
        hasPlayed = false;

        // 如果需要立即播放（无延迟）且AudioManager存在
        if (playOnEnter && delay <= 0 && AudioManager.instance != null)
        {
            AudioManager.instance.Play(soundName);
        }
    }

    /// <summary>
    /// 动画状态更新时调用（每帧调用）
    /// </summary>
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 如果需要延迟播放且音效未播放
        if ((playOnUpdate || playOnEnter) && delay > 0 && !hasPlayed)
        {
            // 累计计时
            timer += Time.deltaTime;

            // 达到延迟时间后播放音效
            if (timer >= delay && AudioManager.instance != null)
            {
                AudioManager.instance.Play(soundName);
                hasPlayed = true;  // 标记已播放，防止重复触发
            }
        }
    }

    /// <summary>
    /// 退出动画状态时调用
    /// </summary>
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 如果需要退出时播放且AudioManager存在
        if (playOnExit && AudioManager.instance != null)
        {
            AudioManager.instance.Play(soundName);
        }
    }
}
#region//废弃代码，当单例不可用时使用（无混音）
//public class PlayOnShotBehaviour : StateMachineBehaviour
//{
//    public AudioClip soundToPlay;// 音效资源
//    public float volume = 1f; // 音量

//    //在进入动画状态时播放音效
//    public bool playOnEnter = true;

//    //在退出动画状态时播放音效
//    public bool playOnExit = false;

//    // 在进入状态后延迟一段时间再播放音效
//    public bool playAfterDelay = false;

//    // 延迟播放的时间(秒)
//    public float playDelay = 0.25f;

//    // 记录从进入当前状态开始经过的时间
//    private float timeSinceEntered = 0;

//    // 标记：延迟音效是否已经播放过
//    private bool hasDelayedSoundPlayed = false;

//    // 进入状态调用
//    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//    {
//        if (playOnEnter)
//        {
//            // 在动画对象的世界位置播放一次性的音效
//            // 参数：音效资源、播放位置、音量
//            AudioSource.PlayClipAtPoint
//                (soundToPlay, animator.gameObject.transform.position, volume);
//        }
//    }



//    // 每帧调用
//    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//    {
//        // 如果启用了延迟播放，且音效还没播放过
//        if (playAfterDelay && !hasDelayedSoundPlayed)
//        {
//            // 累加从进入状态以来经过的时间
//            timeSinceEntered += Time.deltaTime;

//            // 如果累计时间超过设定的延迟时间
//            if (timeSinceEntered > playDelay)
//            {
//                // 在角色当前位置播放音效
//                AudioSource.PlayClipAtPoint(soundToPlay, animator.gameObject.transform.position, volume);
//                // 标记音效已播放，防止重复触发
//                hasDelayedSoundPlayed = true;
//            }
//        }
//    }



//    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//    {
//        if (playOnExit)
//        {
//            AudioSource.PlayClipAtPoint
//                (soundToPlay, animator.gameObject.transform.position, volume);
//        }
//    }
//}
#endregion