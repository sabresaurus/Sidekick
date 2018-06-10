using UnityEngine;

#pragma warning disable 0414

/// <summary>
/// Test class used for checking DontDestroyOnLoad scene inspection
/// </summary>
public class TestDontDestroyOnLoad : MonoBehaviour
{
    [SerializeField]
    float testFloat = 123.45f;

	void Start()
	{
        DontDestroyOnLoad(this.gameObject);
	}
}
