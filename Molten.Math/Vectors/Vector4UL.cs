using System;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Molten.Math
{
	///<summary>A <see cref = "ulong"/> vector comprised of four components.</summary>
	[StructLayout(LayoutKind.Sequential, Pack=8)]
	public partial struct Vector4UL : IFormattable
	{
		///<summary>The X component.</summary>
		public ulong X;

		///<summary>The Y component.</summary>
		public ulong Y;

		///<summary>The Z component.</summary>
		public ulong Z;

		///<summary>The W component.</summary>
		public ulong W;


		///<summary>The size of <see cref="Vector4UL"/>, in bytes.</summary>
		public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Vector4UL));

		///<summary>A Vector4UL with every component set to 1UL.</summary>
		public static readonly Vector4UL One = new Vector4UL(1UL, 1UL, 1UL, 1UL);

		/// <summary>The X unit <see cref="Vector4UL"/>.</summary>
		public static readonly Vector4UL UnitX = new Vector4UL(1UL, 0, 0, 0);

		/// <summary>The Y unit <see cref="Vector4UL"/>.</summary>
		public static readonly Vector4UL UnitY = new Vector4UL(0, 1UL, 0, 0);

		/// <summary>The Z unit <see cref="Vector4UL"/>.</summary>
		public static readonly Vector4UL UnitZ = new Vector4UL(0, 0, 1UL, 0);

		/// <summary>The W unit <see cref="Vector4UL"/>.</summary>
		public static readonly Vector4UL UnitW = new Vector4UL(0, 0, 0, 1UL);

		/// <summary>Represents a zero'd Vector4UL.</summary>
		public static readonly Vector4UL Zero = new Vector4UL(0, 0, 0, 0);

		 /// <summary>
        /// Gets a value indicting whether this instance is normalized.
        /// </summary>
        public bool IsNormalized
        {
            get => MathHelper.IsOne((X * X) + (Y * Y) + (Z * Z) + (W * W));
        }

        /// <summary>
        /// Gets a value indicting whether this vector is zero
        /// </summary>
        public bool IsZero
        {
            get => X == 0 && Y == 0 && Z == 0 && W == 0;
        }

#region Constructors
		///<summary>Creates a new instance of <see cref = "Vector4UL"/>.</summary>
		public Vector4UL(ulong x, ulong y, ulong z, ulong w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		/// <summary>
        /// Initializes a new instance of the <see cref="Vector4UL"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the X, Y, Z and W components of the vector. This must be an array with 4 elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than two elements.</exception>
        public Vector4UL(ulong[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 4)
                throw new ArgumentOutOfRangeException("values", "There must be 4 and only 4 input values for Vector4UL.");

			X = values[0];
			Y = values[1];
			Z = values[2];
			W = values[3];
        }

		/// <summary>
        /// Initializes a new instance of the <see cref="Vector4UL"/> struct from an unsafe pointer. The pointer should point to an array of four elements.
        /// </summary>
		public unsafe Vector4UL(ulong* ptr)
		{
			X = ptr[0];
			Y = ptr[1];
			Z = ptr[2];
			W = ptr[3];
		}
#endregion

#region Instance Functions
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                hashCode = (hashCode * 397) ^ W.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        /// <returns>The length of the vector.</returns>
        /// <remarks>
        /// <see cref="Vector2F.LengthSquared"/> may be preferred when only the relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        public ulong Length()
        {
            return (ulong)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
        }

        /// <summary>
        /// Calculates the squared length of the vector.
        /// </summary>
        /// <returns>The squared length of the vector.</returns>
        /// <remarks>
        /// This method may be preferred to <see cref="Vector2F.Length"/> when only a relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        public ulong LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        public void Normalize()
        {
            ulong length = Length();
            if (!MathHelper.IsZero(length))
            {
                ulong inv = 1.0f / length;
                X *= inv;
                Y *= inv;
            }
        }

		/// <summary>
        /// Creates an array containing the elements of the current <see cref="Vector4UL"/>.
        /// </summary>
        /// <returns>A four-element array containing the components of the vector.</returns>
        public ulong[] ToArray()
        {
            return new ulong[] { X, Y, Z, W};
        }

		/// <summary>
        /// Reverses the direction of the current <see cref="Vector4UL"/>.
        /// </summary>
        /// <returns>A <see cref="Vector4UL"/> facing the opposite direction.</returns>
		public Vector4UL Negate()
		{
			return new Vector4UL(-X, -Y, -Z, -W);
		}
		

		/// <summary>Clamps the component values to within the given range.</summary>
        /// <param name="min">The minimum value of each component.</param>
        /// <param name="max">The maximum value of each component.</param>
        public void Clamp(ulong min, ulong max)
        {
			X = X < min ? min : X > max ? max : X;
			Y = Y < min ? min : Y > max ? max : Y;
			Z = Z < min ? min : Z > max ? max : Z;
			W = W < min ? min : W > max ? max : W;
        }

		/// <summary>Clamps the component values to within the given range.</summary>
        /// <param name="min">The minimum value of each component.</param>
        /// <param name="max">The maximum value of each component.</param>
        public void Clamp(Vector4UL min, Vector4UL max)
        {
			X = X < min.X ? min.X : X > max.X ? max.X : X;
			Y = Y < min.Y ? min.Y : Y > max.Y ? max.Y : Y;
			Z = Z < min.Z ? min.Z : Z > max.Z ? max.Z : Z;
			W = W < min.W ? min.W : W > max.W ? max.W : W;
        }
#endregion

#region To-String

		/// <summary>
        /// Returns a <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </returns>
        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", 
			X.ToString(format, CultureInfo.CurrentCulture), Y.ToString(format, CultureInfo.CurrentCulture), Z.ToString(format, CultureInfo.CurrentCulture), W.ToString(format, CultureInfo.CurrentCulture));
        }

		/// <summary>
        /// Returns a <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", X, Y, Z, W);
        }

		/// <summary>
        /// Returns a <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", X, Y, Z, W);
        }

		/// <summary>
        /// Returns a <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this <see cref="Vector4UL"/>.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", X.ToString(format, formatProvider), Y.ToString(format, formatProvider), Z.ToString(format, formatProvider), W.ToString(format, formatProvider));
        }
#endregion

#region Add operators
		public static Vector4UL operator +(Vector4UL left, Vector4UL right)
		{
			return new Vector4UL(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
		}

		public static Vector4UL operator +(Vector4UL left, ulong right)
		{
			return new Vector4UL(left.X + right, left.Y + right, left.Z + right, left.W + right);
		}

		/// <summary>
        /// Assert a <see cref="Vector4UL"/> (return it unchanged).
        /// </summary>
        /// <param name="value">The <see cref="Vector4UL"/> to assert (unchanged).</param>
        /// <returns>The asserted (unchanged) <see cref="Vector4UL"/>.</returns>
        public static Vector4UL operator +(Vector4UL value)
        {
            return value;
        }
#endregion

#region Subtract operators
		public static Vector4UL operator -(Vector4UL left, Vector4UL right)
		{
			return new Vector4UL(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
		}

		public static Vector4UL operator -(Vector4UL left, ulong right)
		{
			return new Vector4UL(left.X - right, left.Y - right, left.Z - right, left.W - right);
		}

		/// <summary>
        /// Negate/reverse the direction of a <see cref="Vector4UL"/>.
        /// </summary>
        /// <param name="value">The <see cref="Vector4UL"/> to reverse.</param>
        /// <returns>The reversed <see cref="Vector4UL"/>.</returns>
        public static Vector4UL operator -(Vector4UL value)
        {
            return new Vector4UL(-value.X, -value.Y, -value.Z, -value.W);
        }
#endregion

#region division operators
		public static Vector4UL operator /(Vector4UL left, Vector4UL right)
		{
			return new Vector4UL(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
		}

		public static Vector4UL operator /(Vector4UL left, ulong right)
		{
			return new Vector4UL(left.X / right, left.Y / right, left.Z / right, left.W / right);
		}
#endregion

#region Multiply operators
		public static Vector4UL operator *(Vector4UL left, Vector4UL right)
		{
			return new Vector4UL(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
		}

		public static Vector4UL operator *(Vector4UL left, ulong right)
		{
			return new Vector4UL(left.X * right, left.Y * right, left.Z * right, left.W * right);
		}
#endregion

#region Properties

#endregion

#region Static Methods
        /// <summary>Checks to see if any value (x, y, z, w) are within 0.0001 of 0.
        /// If so this method truncates that value to zero.</summary>
        /// <param name="power">The power.</param>
        /// <param name="vec">The vector.</param>
        public static Vector4UL Pow(Vector4UL vec, ulong power)
        {
            return new Vector4UL()
            {
                X = (ulong)Math.Pow(vec.X, power),
                Y = (ulong)Math.Pow(vec.Y, power),
            };
        }

		/// <summary>
        /// Calculates the dot product of two <see cref="Vector4UL"/> vectors.
        /// </summary>
        /// <param name="left">First <see cref="Vector4UL"/> source vector</param>
        /// <param name="right">Second <see cref="Vector4UL"/> source vector.</param>
        public static ulong Dot(Vector4UL left, Vector4UL right)
        {
			return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

		/// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">First source position <see cref="Vector4UL"/> vector.</param>
        /// <param name="tangent1">First source tangent <see cref="Vector4UL"/> vector.</param>
        /// <param name="value2">Second source position <see cref="Vector4UL"/> vector.</param>
        /// <param name="tangent2">Second source tangent <see cref="Vector4UL"/> vector.</param>
        /// <param name="amount">Weighting factor.</param>
        public static Vector4UL Hermite(ref Vector4UL value1, ref Vector4UL tangent1, ref Vector4UL value2, ref Vector4UL tangent2, ulong amount)
        {
            double squared = amount * amount;
            double cubed = amount * squared;
            double part1 = ((2.0D * cubed) - (3.0D * squared)) + 1.0D;
            double part2 = (-2.0D * cubed) + (3.0D * squared);
            double part3 = (cubed - (2.0D * squared)) + amount;
            double part4 = cubed - squared;

			return new Vector4UL()
			{
				X = (ulong)((((value1.X * part1) + (value2.X * part2)) + (tangent1.X * part3)) + (tangent2.X * part4)),
				Y = (ulong)((((value1.Y * part1) + (value2.Y * part2)) + (tangent1.Y * part3)) + (tangent2.Y * part4)),
				Z = (ulong)((((value1.Z * part1) + (value2.Z * part2)) + (tangent1.Z * part3)) + (tangent2.Z * part4)),
				W = (ulong)((((value1.W * part1) + (value2.W * part2)) + (tangent1.W * part3)) + (tangent2.W * part4)),
			};
        }

		/// <summary>
        /// Returns a <see cref="Vector4UL"/> containing the 2D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="Vector4UL"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="Vector4UL"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="Vector4UL"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        public static Vector4UL Barycentric(ref Vector4UL value1, ref Vector4UL value2, ref Vector4UL value3, ulong amount1, ulong amount2)
        {
			return new Vector4UL(
				(value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X)), 
				(value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y)), 
				(value1.Z + (amount1 * (value2.Z - value1.Z))) + (amount2 * (value3.Z - value1.Z)), 
				(value1.W + (amount1 * (value2.W - value1.W))) + (amount2 * (value3.W - value1.W))
			);
        }

		/// <summary>
        /// Performs a linear interpolation between two <see cref="Vector4UL"/>.
        /// </summary>
        /// <param name="start">The start vector.</param>
        /// <param name="end">The end vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <remarks>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static Vector4UL Lerp(ref Vector4UL start, ref Vector4UL end, double amount)
        {
			return new Vector4UL()
			{
				X = (ulong)((1D - amount) * start.X + amount * end.X),
				Y = (ulong)((1D - amount) * start.Y + amount * end.Y),
				Z = (ulong)((1D - amount) * start.Z + amount * end.Z),
				W = (ulong)((1D - amount) * start.W + amount * end.W),
			};
        }

		/// <summary>
        /// Returns a <see cref="Vector4UL"/> containing the smallest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source <see cref="Vector4UL"/>.</param>
        /// <param name="right">The second source <see cref="Vector4UL"/>.</param>
        /// <returns>A <see cref="Vector4UL"/> containing the smallest components of the source vectors.</returns>
		public static Vector4UL Min(Vector4UL left, Vector4UL right)
		{
			return new Vector4UL()
			{
				X = (left.X < right.X) ? left.X : right.X,
				Y = (left.Y < right.Y) ? left.Y : right.Y,
				Z = (left.Z < right.Z) ? left.Z : right.Z,
				W = (left.W < right.W) ? left.W : right.W,
			};
		}

		/// <summary>
        /// Returns a <see cref="Vector4UL"/> containing the largest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source <see cref="Vector4UL"/>.</param>
        /// <param name="right">The second source <see cref="Vector4UL"/>.</param>
        /// <returns>A <see cref="Vector4UL"/> containing the largest components of the source vectors.</returns>
		public static Vector4UL Max(Vector4UL left, Vector4UL right)
		{
			return new Vector4UL()
			{
				X = (left.X > right.X) ? left.X : right.X,
				Y = (left.Y > right.Y) ? left.Y : right.Y,
				Z = (left.Z > right.Z) ? left.Z : right.Z,
				W = (left.W > right.W) ? left.W : right.W,
			};
		}

		/// <summary>
        /// Calculates the squared distance between two <see cref="Vector4UL"/> vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector</param>
        /// <param name="result">When the method completes, contains the squared distance between the two vectors.</param>
        /// <remarks>Distance squared is the value before taking the square root. 
        /// Distance squared can often be used in place of distance if relative comparisons are being made. 
        /// For example, consider three points A, B, and C. To determine whether B or C is further from A, 
        /// compare the distance between A and B to the distance between A and C. Calculating the two distances 
        /// involves two square roots, which are computationally expensive. However, using distance squared 
        /// provides the same information and avoids calculating two square roots.
        /// </remarks>
		public static void DistanceSquared(ref Vector4UL value1, ref Vector4UL value2, out ulong result)
        {
            ulong x = value1.X - value2.X;
            ulong y = value1.Y - value2.Y;
            ulong z = value1.Z - value2.Z;
            ulong w = value1.W - value2.W;

            result = (x * x) + (y * y) + (z * z) + (w * w);
        }

		/// <summary>
        /// Calculates the squared distance between two <see cref="Vector4UL"/> vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The squared distance between the two vectors.</returns>
        /// <remarks>Distance squared is the value before taking the square root. 
        /// Distance squared can often be used in place of distance if relative comparisons are being made. 
        /// For example, consider three points A, B, and C. To determine whether B or C is further from A, 
        /// compare the distance between A and B to the distance between A and C. Calculating the two distances 
        /// involves two square roots, which are computationally expensive. However, using distance squared 
        /// provides the same information and avoids calculating two square roots.
        /// </remarks>
		public static ulong DistanceSquared(ref Vector4UL value1, ref Vector4UL value2)
        {
            ulong x = value1.X - value2.X;
            ulong y = value1.Y - value2.Y;
            ulong z = value1.Z - value2.Z;
            ulong w = value1.W - value2.W;

            return (x * x) + (y * y) + (z * z) + (w * w);
        }

		/// <summary>Clamps the component values to within the given range.</summary>
        /// <param name="value">The <see cref="Vector4UL"/> value to be clamped.</param>
        /// <param name="min">The minimum value of each component.</param>
        /// <param name="max">The maximum value of each component.</param>
        public static Vector4UL Clamp(Vector4UL value, ulong min, ulong max)
        {
			return new Vector4UL()
			{
				X = value.X < min ? min : value.X > max ? max : value.X,
				Y = value.Y < min ? min : value.Y > max ? max : value.Y,
				Z = value.Z < min ? min : value.Z > max ? max : value.Z,
				W = value.W < min ? min : value.W > max ? max : value.W,
			};
        }

		/// <summary>Clamps the component values to within the given range.</summary>
        /// <param name="value">The <see cref="Vector4UL"/> value to be clamped.</param>
        /// <param name="min">The minimum value of each component.</param>
        /// <param name="max">The maximum value of each component.</param>
        public static Vector4UL Clamp(Vector4UL value, Vector4UL min, Vector4UL max)
        {
			return new Vector4UL()
			{
				X = value.X < min.X ? min.X : value.X > max.X ? max.X : value.X,
				Y = value.Y < min.Y ? min.Y : value.Y > max.Y ? max.Y : value.Y,
				Z = value.Z < min.Z ? min.Z : value.Z > max.Z ? max.Z : value.Z,
				W = value.W < min.W ? min.W : value.W > max.W ? max.W : value.W,
			};
        }
#endregion

#region Indexers
		/// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the X, Y, Z or W component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the X component, 1 for the Y component and so on.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 3].</exception>
        
		public ulong this[int index]
		{
			get
			{
				switch(index)
				{
					case 0: return X;
					case 1: return Y;
					case 2: return Z;
					case 3: return W;
				}
				throw new ArgumentOutOfRangeException("index", "Indices for Vector4UL run from 0 to 3, inclusive.");
			}

			set
			{
				switch(index)
				{
					case 0: X = value; break;
					case 1: Y = value; break;
					case 2: Z = value; break;
					case 3: W = value; break;
				}
				throw new ArgumentOutOfRangeException("index", "Indices for Vector4UL run from 0 to 3, inclusive.");
			}
		}
#endregion
	}
}

