using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Camera mainCamera;
    public Camera playerCamera;
    public GameObject mainCanvas;
    public GameObject playerCanvas;

    private float followDistance = 100f;
    private float followSpeed = 5f;

    private Transform currentPlayer;

    void Start()
    {
        ActivateMainCamera();
    }

    void Update()
    {
        if (currentPlayer != null)
        {
            FollowPlayer();
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandlePlayerClick();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ActivateMainCamera();
        }
    }

    private void ActivateMainCamera()
    {
        mainCamera.gameObject.SetActive(true);
        mainCanvas.SetActive(true);
        playerCamera.gameObject.SetActive(false);
        playerCanvas.SetActive(false);
        currentPlayer = null;
    }

    private void ActivatePlayerCamera()
    {
        mainCamera.gameObject.SetActive(false);
        mainCanvas.SetActive(false);
        playerCamera.gameObject.SetActive(true);
        playerCanvas.SetActive(true);
    }
    
    private void HandlePlayerClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Player"))
            {
                currentPlayer = hit.transform;
                ActivatePlayerCamera();
            }
        }
    }

    private void FollowPlayer()
    {
        Vector3 targetPosition = currentPlayer.position - currentPlayer.forward * followDistance;
        targetPosition.y = currentPlayer.position.y;
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPosition, followSpeed * Time.deltaTime);
        
        playerCamera.transform.LookAt(currentPlayer);
    }
}
