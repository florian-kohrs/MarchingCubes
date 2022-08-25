using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class FrameTimer : MonoBehaviour
{

    protected static Stopwatch watch = new Stopwatch();

    public static long MillisecondsSinceFrame => watch.ElapsedMilliseconds;


    public static bool HasTimeLeftInFrame => watch.ElapsedMilliseconds < 15;

    private void Start()
    {
        watch.Restart();
    }

    public static void RestartWatch()
    {
        watch.Restart();
    }

    //private void Update()
    //{
    //    watch.Restart();
    //}

}
