using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class VisibilityMaskGenerator : MonoBehaviour
{
    [Header("Player & mask")]
    public Transform player;                // игрок (центр лучей)
    public int textureSize = 128;           // разрешение маски (меньше = быстрее)
    public float worldSizeX = 20f;          // область маски в мире по X
    public float worldSizeZ = 20f;          // область маски в мире по Z
    public Vector3 maskWorldCenter = Vector3.zero; // центр маски в мире (XZ)

    [Header("Raycast settings")]
    public LayerMask obstacleMask;          // слои, считающиеся препятствиями
    public int rays = 512;                  // сколько лучей от игрока (360°)
    public float maxDistance = 10f;         // радиус видимости (world units)
    public int updateEveryNFrames = 2;      // обновлять не каждую, а каждые N фреймов
    public bool visualize = false;          // для отладки в сцене

    [Header("Output")]
    public Texture2D visibilityTexture;     // для инспектора (можно посмотреть)
    public Material targetMaterial;         // материал(ы), которым назначать маску

    int frameCounter = 0;

    void Start()
    {
        if (visibilityTexture == null || visibilityTexture.width != textureSize)
        {
            visibilityTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false);
            ClearTexture();
        }

        UpdateMaterialMaskParams();
    }

    void Update()
    {
        if (player == null) return;

        frameCounter++;
        if (frameCounter % Mathf.Max(1, updateEveryNFrames) != 0) return;

        // clear mask (start hidden)
        ClearTexture();

        // cast rays in 360 degrees
        for (int i = 0; i < rays; i++)
        {
            float ang = (i / (float)rays) * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
            RaycastHit hit;
            Vector3 origin = player.position + Vector3.up * 0.5f; // немного над землей

            float reached = maxDistance;
            if (Physics.Raycast(origin, dir, out hit, maxDistance, obstacleMask))
            {
                reached = hit.distance;
            }

            // draw segment from origin to origin + dir*reached into texture (sample line)
            DrawLineInMask(origin, dir, reached);
        }

        visibilityTexture.Apply();
        ApplyToMaterial();

        if (visualize) SceneViewRepaint(); // editor visualization helper
    }

    void ClearTexture()
    {
        Color clear = new Color(0, 0, 0, 1); // hidden = 0 in red channel
        Color[] arr = visibilityTexture.GetPixels();
        for (int i = 0; i < arr.Length; i++) arr[i] = clear;
        visibilityTexture.SetPixels(arr);
    }

    
    void DrawLineInMask(Vector3 origin, Vector3 dir, float len)
    {
        
        int steps = Mathf.CeilToInt((len / Mathf.Max(worldSizeX, worldSizeZ)) * textureSize * 2f);
        steps = Mathf.Clamp(steps, 4, 256);

        for (int s = 0; s <= steps; s++)
        {
            float t = (s / (float)steps) * len;
            Vector3 worldPoint = origin + dir * t;
            
            float u = ((worldPoint.x - maskWorldCenter.x) / worldSizeX) * 0.5f + 0.5f;
            float v = ((worldPoint.z - maskWorldCenter.z) / worldSizeZ) * 0.5f + 0.5f;

            int px = Mathf.FloorToInt(u * (textureSize - 1));
            int py = Mathf.FloorToInt(v * (textureSize - 1));
            if (px < 0 || px >= textureSize || py < 0 || py >= textureSize) continue;

          
            visibilityTexture.SetPixel(px, py, Color.white);
        }
    }

    void ApplyToMaterial()
    {
        if (targetMaterial != null)
        {
            targetMaterial.SetTexture("_VisibilityTex", visibilityTexture);
            targetMaterial.SetVector("_MaskWorldCenter", maskWorldCenter);
            targetMaterial.SetVector("_MaskWorldSize", new Vector4(worldSizeX, 0, worldSizeZ, 0));
        }
    }

    void UpdateMaterialMaskParams()
    {
        if (targetMaterial != null && visibilityTexture != null)
        {
            ApplyToMaterial();
        }
    }

    // editor-only repaint
    void SceneViewRepaint()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    // simple gizmo visualize
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(maskWorldCenter, new Vector3(worldSizeX, 0.1f, worldSizeZ));
        if (player != null && visualize)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, maxDistance);
        }
    }
}
