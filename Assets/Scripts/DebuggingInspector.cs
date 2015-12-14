using UnityEngine;

public class DebuggingInspector : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
