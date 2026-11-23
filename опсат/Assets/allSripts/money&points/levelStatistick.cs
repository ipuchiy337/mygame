using UnityEngine;

public class levelStatistick : MonoBehaviour
{
    public float level;
    public float _points;
    public float _pointolvl;
    
    
    void Update()
    {
        
        if (_points == _pointolvl)
        {
            level = +1;
            if (level == +1)
            {
                _pointolvl = +50f;
            }
            if (level == 5)
            {
                _pointolvl = +125f;
            }
            if (level == 10)
            {
                _pointolvl = +425f;
            }
            if (level == 15)
            {
                _pointolvl = +1250f;
            }
            if (level == 25)
            {
                _pointolvl = +3250f;
            }
        }
    }

    
}
