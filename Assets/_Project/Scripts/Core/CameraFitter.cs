using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    public float boardWidth = 4.2f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        cam.orthographicSize = boardWidth / (2f * cam.aspect);
    }
}
