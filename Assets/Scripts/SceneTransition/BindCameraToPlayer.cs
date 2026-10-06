using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class BindCameraToPlayer : MonoBehaviour
{
	private void Start()
	{
		Player player = FindObjectOfType<Player>();
		if (player == null)
		{
			Debug.LogError("找不到玩家，无法设置虚拟摄像机的 Follow");
			return;
		}

		GetComponent<CinemachineVirtualCamera>().Follow = player.transform;
	}
}