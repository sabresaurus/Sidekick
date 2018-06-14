using UnityEngine;

/// <summary>
/// Test class to test selecting something which is destroyed in play mode
/// </summary>
public class DestroyOnPlay : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        Destroy(this.gameObject);
    }
}