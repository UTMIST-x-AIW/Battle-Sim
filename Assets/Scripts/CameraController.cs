using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;
    private Vector3 originalPosition;
    private float originalSize;
    private Camera cam;
    
    [Header("Follow Settings")]
    public float followSpeed = 5f;      // Speed for following creatures
    public float zoomInSize = 3f;       // Camera size when zoomed in
    public float zoomOutSpeed = 10f;    // Speed for zooming out (faster than follow speed)
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        originalPosition = transform.position;
        originalSize = cam.orthographicSize;
    }
    
    private void LateUpdate()
    {
        if (target != null)
        {
            // Calculate desired position (keeping z-coordinate unchanged)
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            
            // Smoothly move camera
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            
            // Smoothly adjust zoom
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomInSize, followSpeed * Time.deltaTime);
        }
        else
        {
            // Smoothly return to original position and zoom
            transform.position = Vector3.Lerp(transform.position, originalPosition, zoomOutSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, originalSize, zoomOutSpeed * Time.deltaTime);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void ResetCamera()
    {
        target = null;
    }
} 