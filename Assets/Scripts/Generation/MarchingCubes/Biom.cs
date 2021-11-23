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

		public const int SIZE = sizeof(float) * 6;

	}
}