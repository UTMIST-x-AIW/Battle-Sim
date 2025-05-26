using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ExperimentalCode
{
    
    public class InstantiatingCreatures : MonoBehaviour
    {
        [SerializeField] private GameObject prefab; 
        [SerializeField] private int numOfInstances;
        [SerializeField] private float spawnArea = 5f;
        [SerializeField] private Ease easeType = Ease.Linear;
        
        private List<GameObject> instantiatedPrefabArray = new List<GameObject>();


        public void GrowCreatures()
        {
            for (int i = 0; i < numOfInstances; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-spawnArea, spawnArea),
                    Random.Range(-spawnArea, spawnArea),
                    0);

                GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity);
                instantiatedPrefabArray.Add(instance);
                AnimatingCreatureDOTween.Grow(instance, easeType);
            }
        }
        
        public void DieCreatures()
        {
            foreach (var go in instantiatedPrefabArray)
            {
                AnimatingCreatureDOTween.PlayDeathAnimation(go);
            }
        }

        public void MakeNormalCreatures()
        {
            for (int i = 0; i < numOfInstances; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-spawnArea, spawnArea),
                    Random.Range(-spawnArea, spawnArea),
                    0);
                GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity);
                instantiatedPrefabArray.Add(instance);
                
            }
        }
    }
}