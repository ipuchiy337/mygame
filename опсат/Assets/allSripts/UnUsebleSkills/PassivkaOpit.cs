using UnityEngine;

public class PassivkaOpit : MonoBehaviour
{
    public UnPasivkiManager pasMan;
    public UpXp upxp;

    public int MaxOpit = 2;

 
    public void AddXp()
    {
        upxp.maxXp += MaxOpit;
        pasMan.plusOpit++;
    }
}
