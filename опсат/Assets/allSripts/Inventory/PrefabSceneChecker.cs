using UnityEngine;

public class PrefabSceneChecker : MonoBehaviour
{
    public GameObject targetObject;

    [Tooltip("ћожно выбрать из списка или написать вручную")]
    public string prefabTag;

    void Update()
    {
        if (string.IsNullOrEmpty(prefabTag))
        {
            targetObject.SetActive(false);
            return;
        }

        targetObject.SetActive(GameObject.FindWithTag(prefabTag) != null);
    }
}
