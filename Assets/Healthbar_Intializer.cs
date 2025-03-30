using UnityEngine;

public class HealthbarIntializer : MonoBehaviour
{
    Material _healthBarMat = null;
    void Awake()
    {
        //This creates a variant copy of the default healthbar material for each healthbar gameobject
        // This is so each healthbar can independently change its _Health Property
        
        Renderer renderer = GetComponent<Renderer>();
        if (Shader.Find("Custom/HealthBar"))
        {
            _healthBarMat = new Material(Shader.Find("Custom/HealthBar"));
            renderer.material = _healthBarMat;
        }
        else
        {
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
