using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

	[System.Serializable]
	public struct Biom
	{
		public float amplitude;
		public float lacunarity;
		[Range(0, 1)]
		public float persistence;
		public float frequency;
		[Range(0.001f, 100)]
		public float scale;
		public float heightOffset;

		public int colorIndex;

		//public byte rFlat;
		//public byte gFlat;
		//public byte bFlat;
		//public byte aFlat;

		//public byte rSteep;
		//public byte gSteep;
		//public byte bSteep;
		//public byte aSteep;

		public const int SIZE = sizeof(float) * 6 + sizeof(int) * 1;

	}
}