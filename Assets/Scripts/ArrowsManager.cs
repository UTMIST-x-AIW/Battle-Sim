using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowsManager : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int poolSize = 500;
    [SerializeField] private float arrowSpeed = 15f;
    [SerializeField] private string arrowSortingLayer = "Arrows";
    [SerializeField] private int arrowSortingOrder = 5;
    
    private Queue<GameObject> arrowPool = new Queue<GameObject>();
    private List<GameObject> activeArrows = new List<GameObject>();
    
    // Singleton pattern
    public static ArrowsManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject arrow = CreateNewArrow();
            arrow.SetActive(false);
            arrowPool.Enqueue(arrow);
        }
    }
    
    private GameObject CreateNewArrow()
    {
        GameObject arrow = null;
        
        if (arrowPrefab != null)
        {
            arrow = Instantiate(arrowPrefab, transform);
        }
        
        return arrow;
    }
    
    
    /// <summary>
    /// Fires an arrow from the shooter position towards the target position
    /// </summary>
    /// <param name="shooterPos">Position where the arrow starts</param>
    /// <param name="targetPos">Position where the arrow should aim</param>
    /// <param name="maxDistance">Maximum distance the arrow can travel before disappearing</param>
    public void FireArrow(Vector3 shooterPos, Vector3 targetPos, float maxDistance)
    {
        GameObject arrow = GetPooledArrow();
        if (arrow == null) return;
        
        // Position and orient the arrow
        arrow.transform.position = shooterPos;
        Vector3 direction = (targetPos - shooterPos).normalized;
        
        // Rotate arrow to point in the direction of travel
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        arrow.SetActive(true);
        activeArrows.Add(arrow);
        
        // Start the arrow's movement coroutine
        StartCoroutine(MoveArrow(arrow, direction, maxDistance));
    }
    
    private GameObject GetPooledArrow()
    {
        if (arrowPool.Count > 0)
        {
            return arrowPool.Dequeue();
        }
        
        // If pool is empty, create a new arrow (expanding pool dynamically)
        return CreateNewArrow();
    }
    
    private IEnumerator MoveArrow(GameObject arrow, Vector3 direction, float maxDistance)
    {
        Vector3 startPosition = arrow.transform.position;
        float distanceTraveled = 0f;
        
        while (arrow != null && arrow.activeInHierarchy && distanceTraveled < maxDistance)
        {
            // Move the arrow
            Vector3 movement = direction * arrowSpeed * Time.deltaTime;
            arrow.transform.position += movement;
            distanceTraveled += movement.magnitude;
            
            yield return null;
        }
        
        // Return arrow to pool
        ReturnToPool(arrow);
    }
    
    private void ReturnToPool(GameObject arrow)
    {
        if (arrow != null)
        {
            activeArrows.Remove(arrow);
            arrow.SetActive(false);
            arrowPool.Enqueue(arrow);
        }
    }
    
    /// <summary>
    /// Cleans up all active arrows and returns them to the pool
    /// </summary>
    public void ClearAllArrows()
    {
        for (int i = activeArrows.Count - 1; i >= 0; i--)
        {
            ReturnToPool(activeArrows[i]);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
} 