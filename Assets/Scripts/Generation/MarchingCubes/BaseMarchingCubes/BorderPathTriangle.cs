using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class BorderPathTriangle : PathTriangle
    {

        //Todo: Change function to interface
        public BorderPathTriangle(ICubeEntity e, Triangle t, Func<PathTriangle, Color> f):base(e, t,f)
        {
        }
        public BorderPathTriangle(ICubeEntity e, Triangle t, uint steepnessAndColorData) : base(e, t, steepnessAndColorData)
        {
        }

        public override List<PathTriangle> GetCircumjacent()
        {
            List<PathTriangle> result = new List<PathTriangle>();

            //if (neighbours == null)
            //{
            //    neighbours = e.GetNeighboursOf(this).ToArray();
            //}

            //for (int i = 0; i < TRIANGLE_NEIGHBOUR_COUNT; i++)
            //{
            //    if (neighbours[i].Slope < MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING)
            //    {
            //        result.Add(neighbours[i]);
            //    }
            //}
            return result;
        }

    }

}