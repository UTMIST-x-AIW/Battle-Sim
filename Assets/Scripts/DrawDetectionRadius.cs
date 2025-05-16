using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection.Metadata;
using static Creature;

public class DrawDetectionRadius : MonoBehaviour
{
    private List<GameObject> listOfActiveCreatures;
    private NEATTest neatTest;




    private void OnEnable()
    {
        ObjectPoolManager.OnListChanged += SyncList;
    }
    
    
    private void Start()
    {
        neatTest = NEATTest.Instance;
        Debug.LogWarning(neatTest + "is not null");
    }


    private void OnDisable()
    {
        ObjectPoolManager.OnListChanged -= SyncList;
    }

    void SyncList(List<GameObject> updatedList)
    {
        //listOfActiveCreatures.Clear();
        listOfActiveCreatures = updatedList;
    }

    private void OnDrawGizmos()
    {
        if (listOfActiveCreatures == null) return;
        foreach (GameObject obj in listOfActiveCreatures)
        {
            // Only draw if visualization is enabled and NEATTest reference exists
            if (neatTest != null)
            {
                Debug.Log("NEATTEST was not null");
                if (neatTest.showDetectionRadius)
                {
                    Creature objCreatureComponenet = obj.GetComponent<Creature>();
                    //// Set color to be semi-transparent and match creature type
                    Color gizmoColor = (objCreatureComponenet.creatureType == CreatureType.Albert)
                            ? new Color(1f, 0.5f, 0f, 0.1f) : new Color(0f, 0.5f, 1f, 0.1f);  // Orange for Albert, Blue for Kai
                    Gizmos.color = gizmoColor;

                    // Draw filled circle for better visibility
                    Gizmos.DrawSphere(obj.transform.position, CreatureObserver.DETECTION_RADIUS);

                    // Draw wire frame with more opacity for better edge definition
                    gizmoColor.a = 0.3f;
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawWireSphere(obj.transform.position, CreatureObserver.DETECTION_RADIUS);
                }

                // Draw chop range if enabled
                if (neatTest.showChopRange)
                {
                    Gizmos.color = neatTest.chopRangeColor;
                    Gizmos.DrawWireSphere(transform.position, 1.5f);
                }
            }
        }
    }
}
