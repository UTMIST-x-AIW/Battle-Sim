using System;
using System.Collections.Generic;
using UnityEngine;

public class RaycasterWithJobs : MonoBehaviour
{
    [SerializeField] private int rayCount = 10;
    [SerializeField, Min(0f)] private float maxDistance = 10;
    
    public Dictionary<int, RaycastHit2D> NearestHitsByTag;

    private void Start()
    {
        NearestHitsByTag = new Dictionary<int, RaycastHit2D>();
    }

    private void LateUpdate()
    {
      //  ShootRays();
    }

    /*private void ShootRays()
    {
        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? i / (float)(rayCount - 1) : 0.5f;
            float rotationAngle = Mathf.Lerp(-180, 180, t);
            Vector2 direction = Quaternion.Euler(0, 0, rotationAngle) * Vector2.right;
            int excludeSelfMask = ~(1 << LayerMask.NameToLayer("Alberts"));
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction,
                maxDistance, excludeSelfMask);
            if (hit)
            {
               // if (hit.collider == GetComponent<Collider2D>()) return;
                Debug.DrawLine(transform.position, hit.transform.position, Color.green ); 
            }
            else
            {
                Debug.DrawRay(transform.position, direction * maxDistance, Color.red );
            }
            
            
        }
    }*/

    int StringToID(string tagName)
    {
        return tagName.GetHashCode();
    }
    
    public RaycastHit2D GetNearestHitByTag(int tagID)
    {
        return NearestHitsByTag.TryGetValue(tagID, out var value) ? value : new RaycastHit2D();
    }
}
