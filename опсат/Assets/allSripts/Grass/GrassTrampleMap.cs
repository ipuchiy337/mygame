using UnityEngine;

public class GrassTrampleMap : MonoBehaviour
{
    [Header("Area")]
    public float worldSize = 50f;          // размер зоны в мире (метры)
    public Vector3 areaCenter;             // можно привязать к игроку

    [Header("Render")]
    public int textureResolution = 512;
    public float fadeSpeed = 1.5f;         // скорость восстановления травы

    private RenderTexture _trampleMap;
    private RenderTexture _prevMap;
    private Camera _topDownCam;
    public LayerMask affectorLayer;        // слой объектов, которые мнут траву
    public Shader blobShader;              // простой unlit-шейдер, рисующий круг по нормали скорости

    void Start()
    {
        _trampleMap = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RHalf);
        _prevMap = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RHalf);

        GameObject camGO = new GameObject("TrampleCam");
        camGO.transform.SetParent(transform);
        _topDownCam = camGO.AddComponent<Camera>();
        _topDownCam.orthographic = true;
        _topDownCam.orthographicSize = worldSize * 0.5f;
        _topDownCam.nearClipPlane = 0.1f;
        _topDownCam.farClipPlane = 100f;
        _topDownCam.cullingMask = affectorLayer;
        _topDownCam.clearFlags = CameraClearFlags.SolidColor;
        _topDownCam.backgroundColor = Color.black;
        _topDownCam.targetTexture = _trampleMap;
        _topDownCam.enabled = false; // рендерим вручную

        Shader.SetGlobalVector("_TrampleAreaCenter", areaCenter);
        Shader.SetGlobalFloat("_TrampleAreaSize", worldSize);
    }

    void LateUpdate()
    {
        _topDownCam.transform.position = areaCenter + Vector3.up * 20f;
        _topDownCam.transform.rotation = Quaternion.Euler(90, 0, 0);

        _topDownCam.Render();

        // затухание — можно сделать через compute shader/blit с decay-материалом
        Shader.SetGlobalTexture("_TrampleMap", _trampleMap);
        Shader.SetGlobalVector("_TrampleAreaCenter", areaCenter);
        Shader.SetGlobalFloat("_TrampleAreaSize", worldSize);
    }
}