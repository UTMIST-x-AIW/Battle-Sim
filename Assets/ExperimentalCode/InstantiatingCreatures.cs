using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ExperimentalCode
{
    
    public class InstantiatingCreatures : MonoBehaviour
    {
        [SerializeField] private GameObject prefab; 
        [SerializeField] private int numOfInstances;
        [SerializeField, Range(0.5f,10f)] private float spawnArea = 5f;
        [SerializeField] private Ease easeType = Ease.Linear;
        
        private readonly List<GameObject> _instantiatedPrefabArray = new List<GameObject>();


        public void MakeNormalCreatures()
        {
            for (int i = 0; i < numOfInstances; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-spawnArea, spawnArea),
                    Random.Range(-spawnArea, spawnArea),
                    0);
                GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity);
                ParenthoodManager.AssignParent(instance);
                _instantiatedPrefabArray.Add(instance);
                
            }
        }

        public void GrowCreatures()
        {
            for (int i = 0; i < numOfInstances; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-spawnArea, spawnArea),
                    Random.Range(-spawnArea, spawnArea),
                    0);

                GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity);
                ParenthoodManager.AssignParent(instance);
                _instantiatedPrefabArray.Add(instance);
                AnimatingDoTweenUtilities.PlayGrow(instance, easeType);
            }
        }
        
        public void DieCreatures()
        {
            foreach (var go in _instantiatedPrefabArray)
            {
                AnimatingDoTweenUtilities.PlayDeathAnimation(go);
            }
        }

        private void OnDisable()
        {
            ParenthoodManager.ClearParentDict();
        }
    }
}