using System;
using System.Runtime.InteropServices;

namespace OpenTK {
	
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
	public struct Vector3 {
		
		public float X, Y, Z;

		public Vector3( float x, float y, float z ) {
			X = x; Y = y; Z = z;
		}

		public Vector3(Vector3 v) {
			X = v.X; Y = v.Y; Z = v.Z;
		}
		
		public static bool operator == ( Vector3 left, Vector3 right ) {
			return left.Equals(right);
		}

		public static bool operator != ( Vector3 left, Vector3 right ) {
			return !left.Equals(right);
		}

		public override string ToString() {
			return String.Format("({0}, {1}, {2})", X, Y, Z);
		}

		public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		public override bool Equals(object obj) {
			return (obj is Vector3) && this.Equals( (Vector3)obj );
		}
		
		public bool Equals( Vector3 other ) {
			return X == other.X && Y == other.Y && Z == other.Z;
		}
	}
}
