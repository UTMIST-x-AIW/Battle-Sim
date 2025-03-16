using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCameraController : MonoBehaviour
{
    public Camera playerCamera; // Assign the player-specific camera
    public Canvas playerCanvas; // Assign the stats canvas
    private Camera mainCamera;  // Reference to the main camera
    private GameObject selectedPlayer;
    
    private float followSpeed = 5f;
    private float followDistance = 5f;

    void Start()
    {
        mainCamera = Camera.main;
        playerCamera.gameObject.SetActive(false); // Disable player camera initially
        playerCanvas.gameObject.SetActive(false); // Disable player canvas initially
    }

    void Update()
    {
        // Detect mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, Mathf.Infinity);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    Debug.Log("Player clicked!");

                    // Set the selected player
                    selectedPlayer = hit.collider.gameObject;

                    // Enable player-specific camera and canvas
                    playerCamera.transform.position = selectedPlayer.transform.position + new Vector3(0, 0, -followDistance);
                    playerCamera.gameObject.SetActive(true);
                    playerCanvas.gameObject.SetActive(true);
                    // mainCamera.gameObject.SetActive(false);

                    
                    break; // Stop once the first player is selected
                }
            }
        }
        
        if (selectedPlayer != null)
        {
            Debug.Log("player still selected");
            Vector3 targetPosition = selectedPlayer.transform.position + new Vector3(0, 0, -10);
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPosition, Time.deltaTime * followSpeed);
            UpdatePlayerStats(selectedPlayer.GetComponent<Stats>());
        }

        // Switch back to main camera on right-click
        if (Input.GetMouseButtonDown(1))
        {
            playerCamera.gameObject.SetActive(false);
            playerCanvas.gameObject.SetActive(false);
            // mainCamera.gameObject.SetActive(true);
            selectedPlayer = null;
        }
    }

    void UpdatePlayerStats(Stats playerStats)
    {
        if (playerStats != null)
        {
            playerCanvas.transform.Find("HealthText").GetComponent<TextMeshProUGUI>().text =    $"Health: {playerStats.health}";
            playerCanvas.transform.Find("MaxHealthText").GetComponent<TextMeshProUGUI>().text = $"Max Health: {playerStats.maxHealth}";
            playerCanvas.transform.Find("SpeedText").GetComponent<TextMeshProUGUI>().text =     $"Move Speed: {playerStats.moveSpeed}";
            playerCanvas.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>().text =  $"Attack Cooldown: {playerStats.attackCooldown}";
            playerCanvas.transform.Find("RangeText").GetComponent<TextMeshProUGUI>().text =     $"Range: {playerStats.range}";
            playerCanvas.transform.Find("DamageText").GetComponent<TextMeshProUGUI>().text =    $"Damage: {playerStats.damage}";
        }
    }
}
