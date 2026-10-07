using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransUI : MonoBehaviour
{
	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
}
