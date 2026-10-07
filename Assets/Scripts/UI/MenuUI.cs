using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
	[Header("游戏场景设置")]
	[SerializeField] private string gameSceneName = "MainGameScene（备份）";

	private bool isStartingGame;

	private void Start()
	{
		if (AudioManager.instance != null)
		{
			AudioManager.instance.Play("标题音乐");
		}
	}

	/// <summary>
	/// 开始游戏按钮调用
	/// </summary>
	public void StartGame()
	{
		if (isStartingGame)
			return;

		StartCoroutine(LoadGameSceneRoutine());
	}

	private IEnumerator LoadGameSceneRoutine()
	{
		isStartingGame = true;
		Time.timeScale = 1f;

		if (ScreenFader.Instance != null)
			yield return ScreenFader.Instance.FadeOut();

		SceneManager.LoadScene(gameSceneName);
	}

	/// <summary>
	/// 退出游戏按钮调用
	/// </summary>
	public void QuitGame()
	{
		Application.Quit();
	}
}