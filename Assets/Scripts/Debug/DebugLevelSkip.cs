using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class DebugLevelSkip : MonoBehaviour
{
	[Header("跳转的目标场景名称")]
	[SerializeField] private string nextSceneName = "Level2";

	void Update()
	{
		if (Keyboard.current == null)
			return;

		// 按下 F2 键直接跳关（wasPressedThisFrame 相当于旧系统的 GetKeyDown）
		if (Keyboard.current.f2Key.wasPressedThisFrame)
		{
			SkipLevel();
		}
	}

	private void SkipLevel()
	{
		Debug.Log($"正在跳转到场景：{nextSceneName}");
		SceneManager.LoadScene(nextSceneName);
	}
}