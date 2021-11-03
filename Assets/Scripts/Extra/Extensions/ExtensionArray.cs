using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionArray
{

    public static T[] Fill<T>(this T[] destinationArray, T value)
    {
        int arrayToFillHalfLength = destinationArray.Length / 2;
        int copyLength;
        destinationArray[0] = value;
        for (copyLength = 1; copyLength < arrayToFillHalfLength; copyLength <<= 1)
        {
            Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);
        }

        Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
        return destinationArray;
    }

}
