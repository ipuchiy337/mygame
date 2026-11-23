using UnityEngine;

[CreateAssetMenu(menuName = "Grass/Settings")]
public class GrassSettings : ScriptableObject
{
    [Header("Wind")]
    public float windStrength = 0.3f;
    public float windSpeed = 1f;
    public float windScale = 0.1f;
    public Vector2 windDirection = new Vector2(1, 0.3f);

    [Header("Bending")]
    public float bendStrength = 1.5f;
    public float bendFadeSpeed = 1.5f;

    [Header("Density / LOD")]
    public float density = 10f;
    public float renderDistance = 40f;

    public void Apply(Material grassMat)
    {
        grassMat.SetFloat("_WindStrength", windStrength);
        grassMat.SetFloat("_WindSpeed", windSpeed);
        grassMat.SetFloat("_BendStrength", bendStrength);
        // è ò.ä.
    }
}