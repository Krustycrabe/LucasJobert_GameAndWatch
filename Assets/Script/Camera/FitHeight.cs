using UnityEngine;

[ExecuteAlways]
public class CameraFitHeight : MonoBehaviour
{
    [Tooltip("Hauteur visible en 'world units' = la hauteur de ta zone design.")]
    public float designWorldHeight = 10f;

    Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void Update()
    {
        // Utile en Editor + rotations
        Apply();
    }

    void Apply()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam || !cam.orthographic) return;

        // OrthographicSize = moitié de la hauteur visible
        cam.orthographicSize = designWorldHeight * 0.5f;
    }
}