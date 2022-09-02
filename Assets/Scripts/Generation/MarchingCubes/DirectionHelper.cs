using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{


    public enum Direction { Right = 0, Left = 1, Up = 2, Down = 3, Front = 4, Back = 5 }


    public class DirectionHelper
    {

        protected static Direction MaybeFlipDirection(int sign, Direction d)
        {
            int directionIndex = (int)d;
            if (sign < 0)
                directionIndex += 1;
            return (Direction)(directionIndex);
        }

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

        protected static Direction SingleDirectionVectorToDirection(Vector3Int d)
        {
            if (d.x < 0)
                return Direction.Left;

            if (d.x > 0)
                return Direction.Right;

            if (d.z < 0)
                return Direction.Back;

            if (d.z > 0)
                return Direction.Front;

            if (d.y < 0)
                return Direction.Down;

            if (d.y < 0)
                return Direction.Up;

            throw new System.ArgumentException();
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

        public static Vector3Int OffsetVector3Int(Direction d, Vector3Int target, int offset)
        {
            switch (d)
            {
                case Direction.Right:
                    target.x += offset;
                    break;
                case Direction.Left:
                    target.x -= offset;
                    break;
                case Direction.Up:
                    target.y += offset;
                    break;
                case Direction.Down:
                    target.y -= offset;
                    break;
                case Direction.Front:
                    target.z += offset;
                    break;
                case Direction.Back:
                    target.z -= offset;
                    break;
                default:
                    throw new Exception("Unhandled enum case!");
            }
            return target;
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
                    throw new Exception("Unhandled enum case!");
            }
        }


        public static Direction[] GetAllCombination(Vector3Int v)
        {
            Direction up = MaybeFlipDirection(v.y, Direction.Up);
            Direction[] r = new Direction[] {
                MaybeFlipDirection(v.x,Direction.Right),
                up,
                MaybeFlipDirection(v.z,Direction.Front),
                MaybeFlipDirection(-v.y,Direction.Up),
                MaybeFlipDirection(-v.x,Direction.Right),
                up,
                MaybeFlipDirection(-v.z,Direction.Front)
            };
            return r;
        }

        public static Direction[] GetReducedCombination(Vector3Int v)
        {
            Direction[] r = new Direction[3];
            if (v.z == 0)
            {
                r[0] = MaybeFlipDirection(v.x, Direction.Right);
                r[1] = MaybeFlipDirection(v.y, Direction.Up);
                r[2] = MaybeFlipDirection(-v.x, Direction.Right);
            }
            else if (v.x == 0)
            {
                r[0] = MaybeFlipDirection(v.y, Direction.Up);
                r[1] = MaybeFlipDirection(v.z, Direction.Front);
                r[2] = MaybeFlipDirection(-v.y, Direction.Up);
            }
            else
            {
                r[0] = MaybeFlipDirection(v.x, Direction.Right);
                r[1] = MaybeFlipDirection(v.z, Direction.Front);
                r[2] = MaybeFlipDirection(-v.x, Direction.Right);
            }
            return r;
        }

        public static Direction[] GetAllNonDefaultAxisCombinations(Vector3Int v)
        {
            int count = v.CountNonZeros();
            if (count == 3)
                return GetAllCombination(v);
            else if (count == 2)
                return GetReducedCombination(v);
            else if (count == 1)
                return new Direction[] { SingleDirectionVectorToDirection(v) };
            else
                return Array.Empty<Direction>();
        }


    }

}