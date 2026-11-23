using UnityEngine;

public class LevelBonuses : MonoBehaviour
{
    public Xp exepe;
    public GunWithTracer gunTra;

    public GameObject panelGunBaf;
    //public GameObject panelPidorBaf;

    void Update()
    {
        if (exepe.level >= 1)
        {
            panelGunBaf.SetActive(true);
        }
    }

}
