using UnityEngine;

[ExecuteInEditMode]
public class PlayerShaderPropertiesSetter : MonoBehaviour
{
    //SP = Shader property
    private static int PlayerPosition = Shader.PropertyToID("_PlayerWorldPos");
    public Transform _transform;

    private void Start()
    {
        _transform = transform;
    }

    private void Update()
    {
        Shader.SetGlobalVector("_PlayerWorldPos", transform.position);

    }
}