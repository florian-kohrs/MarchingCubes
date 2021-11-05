using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class FrameTimer : MonoBehaviour
{

    public static Stopwatch watch = new Stopwatch();

    public static long MillisecondsSinceFrame => watch.ElapsedMilliseconds;


    public static bool HasTimeLeftInFrame => watch.ElapsedMilliseconds < 15;


    private void Update()
    {
        watch.Restart();
    }

}
