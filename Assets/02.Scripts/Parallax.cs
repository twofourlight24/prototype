using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float parallaxFactor = 0.5f;
    private Vector3 lastCamPos;
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0f);
        lastCamPos = cam.position;
    }
}
