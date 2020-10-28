using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathExt
{

    public static int FloorMod(this int i, int m)
    {
        int r = i % m;
        if (r < 0)
        {
            return r + m;
        }
        else
        {
            return r;
        } 
    }

}
