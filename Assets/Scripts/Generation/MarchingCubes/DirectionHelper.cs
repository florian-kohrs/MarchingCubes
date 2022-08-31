using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{


    public enum Direction { Right = 0, Left = 1, Up = 2, Down = 3, Front = 4, Back = 5 }


    public class DirectionHelper
    {

        public static Vector3 DirectionToVector(Direction d)
        {
            switch (d)
            {
                case Direction.Left:
                    return Vector3.left;
                case Direction.Right:
                    return Vector3.right;
                case Direction.Front:
                    return Vector3.forward;
                case Direction.Back:
                    return Vector3.back;
                case Direction.Up:
                    return Vector3.up;
                case Direction.Down:
                    return Vector3.down;
                default:
                    throw new System.Exception("Bad direction value");
            }
        }

        public static Vector3 OffsetVector(Direction d, Vector3 v3, float offset)
        {
            switch (d)
            {
                case Direction.Right:
                    v3.x += offset;
                    break;
                case Direction.Left:
                    v3.x -= offset;
                    break;
                case Direction.Up:
                    v3.y += offset;
                    break;
                case Direction.Down:
                    v3.y -= offset;
                    break;
                case Direction.Front:
                    v3.z += offset;
                    break;
                case Direction.Back:
                    v3.z -= offset;
                    break;
                default:
                    throw new System.Exception("Unhandled enum case!");
            }
            return v3;
        }

        public static void OffsetIntArray(Direction d, int[] target, int offset)
        {
            switch (d)
            {
                case Direction.Right:
                    target[0] += offset;
                    break;
                case Direction.Left:
                    target[0] -= offset;
                    break;
                case Direction.Up:
                    target[1] += offset;
                    break;
                case Direction.Down:
                    target[1] -= offset;
                    break;
                case Direction.Front:
                    target[2] += offset;
                    break;
                case Direction.Back:
                    target[2] -= offset;
                    break;
                default:
                    throw new System.Exception("Unhandled enum case!");
            }
        }

    }

}