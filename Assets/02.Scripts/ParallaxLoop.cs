using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    public float parallaxFactor = 0.5f;

    private float textureUnitSizeX;
    private Transform cam;
    private Vector3 lastCamPos;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;

        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        textureUnitSizeX = sprite.bounds.size.x;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cam.position - lastCamPos;
        transform.position += new Vector3(deltaMovement.x * parallaxFactor, 0, 0);
        lastCamPos = cam.position;

        float camDistanceFromThis = Mathf.Abs(cam.position.x - transform.position.x);
        if (camDistanceFromThis >= textureUnitSizeX)
        {
            float offsetX = (cam.position.x - transform.position.x) % textureUnitSizeX;
            transform.position = new Vector3(cam.position.x + offsetX, transform.position.y, transform.position.z);
        }
    }
}
