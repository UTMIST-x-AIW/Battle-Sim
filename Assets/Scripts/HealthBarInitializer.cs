using UnityEngine;

public class HealthbarIntializer : MonoBehaviour
{
    Material _healthBarMat = null;
    void Awake()
    {
        //This creates a variant copy of the default healthbar material for each healthbar gameobject
        // This is so each healthbar can independently change its _Health Property
        
        Renderer renderer = GetComponent<Renderer>();
        // Try several possible shader names that exist in the project
        Shader healthBarShader = Shader.Find("HealthBar 2") ?? 
                                 Shader.Find("HealthBar 2 simple") ?? 
                                 Shader.Find("Custom/HealthBar");
        
        if (healthBarShader != null)
        {
            _healthBarMat = new Material(healthBarShader);
            renderer.material = _healthBarMat;
        }
        else
        {
            Debug.LogWarning("HealthBar shader not found! Using Standard shader as fallback.");
            _healthBarMat = new Material(Shader.Find("Standard"));
            renderer.material = _healthBarMat;
        }
    }
/// <summary>
/// To change _Health use this puesoducode
///```
///void Update()
///{
/// render.material.SetFloat("_Health", your healthvalue)
///}
/// ```
/// </summary>
    
    

}
