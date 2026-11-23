using UnityEngine;

public class HillManager : MonoBehaviour
{
    public bool hilling = false;
    public ParticleSystem hilleffect;

    void Update()
    {
        if (hilleffect == null) return; // защита на всякий случай

        if (hilling)
        {
            if (!hilleffect.isPlaying)
                hilleffect.Play();
        }
        else
        {
            if (hilleffect.isPlaying)
                hilleffect.Stop();
        }
    }
}
