using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用场景管理命名空间
using UnityEngine.InputSystem;

public class SceneTransitionTrigger : MonoBehaviour
{
	[Header("目标场景设置")]
	[Tooltip("要加载的目标场景名称")]
	[SerializeField] private string targetSceneName;

	private bool hasTriggered = false; // 防止重复触发

	private void OnTriggerEnter2D(Collider2D collision)
	{
		// 只有玩家本体掉进来时触发，且只触发一次
		if (!hasTriggered && collision.CompareTag("Player"))
		{
			hasTriggered = true;
			StartCoroutine(LoadSceneRoutine(collision.gameObject));
		}
	}

	private IEnumerator LoadSceneRoutine(GameObject player)
	{
		// 禁用玩家操作，防止切场景过程中乱按
		var playerInput = player.GetComponent<PlayerInput>();
		if (playerInput != null) 
			playerInput.DeactivateInput();

		// 触发黑屏淡出动画
		if (ScreenFader.Instance != null)
		{
			yield return ScreenFader.Instance.FadeOut(5f);
		}

		// 确保游戏时间流速正常
		Time.timeScale = 1f;

		//  加载下一个场景
		 SceneManager.LoadScene(targetSceneName);
	}
}