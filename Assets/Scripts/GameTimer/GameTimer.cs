using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
	public static GameTimer Instance { get; private set; }

	[SerializeField] private TMP_Text timerText;

	public float ElapsedTime { get; private set; }
	public bool IsRunning { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		if (!IsRunning)
			return;

		ElapsedTime += Time.deltaTime;

		if (timerText != null)
		{
			int minutes = Mathf.FloorToInt(ElapsedTime / 60f);
			int seconds = Mathf.FloorToInt(ElapsedTime % 60f);

			timerText.text = $"游戏时间： {minutes:00}:{seconds:00}";
		}
	}

	public void StartTimer()
	{
		IsRunning = true;
	}

	public void StopTimer()
	{
		IsRunning = false;
	}

	public void ResetTimer()
	{
		ElapsedTime = 0f;
		IsRunning = false;
	}
}