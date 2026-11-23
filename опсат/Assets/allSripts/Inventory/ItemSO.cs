using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "NewItem")]
public class ItemSO : ScriptableObject 
{
    public string itemName;
    public Sprite icon;
    public int maxStackSize = 1;
    public int cost;

   
    public GameObject handItemObject;

    
    public GameObject pickupPrefab;

    public AudioClip pickUpSound;  // <-- AudioClip, не AudioSource
    public AudioClip dropSound;    // <-- AudioClip, не AudioSource

}