using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGFXModel.Utilities
{
    // SB: Imported from Ohana3DS Rebirth RenderBase.cs, with adaptions

    /// <summary>
    ///     2-D Vector.
    /// </summary>
    public class Vector2
    {
        public float X;
        public float Y;

        /// <summary>
        ///     Creates a new 2-D Vector.
        /// </summary>
        /// <param name="_x">The X position</param>
        /// <param name="_y">The Y position</param>
        public Vector2(float _x, float _y)
        {
            X = _x;
            Y = _y;
        }

        /// <summary>
        ///     Creates a new 2-D Vector.
        /// </summary>
        /// <param name="vector">The 2-D Vector</param>
        public Vector2(Vector2 vector)
        {
            X = vector.X;
            Y = vector.Y;
        }

        /// <summary>
        ///     Creates a new 2-D Vector.
        /// </summary>
        public Vector2()
        {
        }

        public static Vector2 Read(Utility utility)
        {
            return new Vector2(utility.ReadFloat(), utility.ReadFloat());
        }

        public void Write(Utility utility)
        {
            utility.Write(X);
            utility.Write(Y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this == (Vector2)obj;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^
                Y.GetHashCode();
        }

        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("X:{0}; Y:{1}", X, Y);
        }
    }

    /// <summary>
    ///     3-D Vector.
    /// </summary>
    public class Vector3
    {
        public float X;
        public float Y;
        public float Z;

        /// <summary>
        ///     Creates a new 3-D Vector.
        /// </summary>
        /// <param name="_x">The X position</param>
        /// <param name="_y">The Y position</param>
        /// <param name="_z">The Z position</param>
        public Vector3(float _x, float _y, float _z)
        {
            X = _x;
            Y = _y;
            Z = _z;
        }

        /// <summary>
        ///     Creates a new 3-D Vector.
        /// </summary>
        /// <param name="vector">The 3-D vector</param>
        public Vector3(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        /// <summary>
        ///     Creates a new 3-D Vector.
        /// </summary>
        public Vector3()
        {
        }

        public static Vector3 Read(Utility utility)
        {
            return new Vector3(utility.ReadFloat(), utility.ReadFloat(), utility.ReadFloat());
        }

        public void Write(Utility utility)
        {
            utility.Write(X);
            utility.Write(Y);
            utility.Write(Z);
        }

        /// <summary>
        ///     Transform the 3-D Vector with a matrix.
        /// </summary>
        /// <param name="input">Input vector</param>
        /// <param name="matrix">The matrix</param>
        /// <returns></returns>
        public static Vector3 transform(Vector3 input, Matrix matrix)
        {
            Vector3 output = new Vector3();
            output.X = input.X * matrix.M11 + input.Y * matrix.M21 + input.Z * matrix.M31 + matrix.M41;
            output.Y = input.X * matrix.M12 + input.Y * matrix.M22 + input.Z * matrix.M32 + matrix.M42;
            output.Z = input.X * matrix.M13 + input.Y * matrix.M23 + input.Z * matrix.M33 + matrix.M43;
            return output;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this == (Vector3)obj;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^
                Y.GetHashCode() ^
                Z.GetHashCode();
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator *(Vector3 a, float b)
        {
            return new Vector3(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector3 operator *(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static Vector3 operator /(Vector3 a, float b)
        {
            return new Vector3(a.X / b, a.Y / b, a.Z / b);
        }

        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return !(a == b);
        }

        public float length()
        {
            return (float)Math.Sqrt(dot(this, this));
        }

        public Vector3 normalize()
        {
            return this / length();
        }

        public static float dot(Vector3 a, Vector3 b)
        {
            float x = a.X * b.X;
            float y = a.Y * b.Y;
            float z = a.Z * b.Z;

            return x + y + z;
        }

        public override string ToString()
        {
            return string.Format("X:{0}; Y:{1}; Z:{2}", X, Y, Z);
        }
    }

    /// <summary>
    ///     4-D Vector.
    /// </summary>
    public class Vector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        /// <summary>
        ///     Creates a new 4-D Vector.
        /// </summary>
        /// <param name="_x">The X position</param>
        /// <param name="_y">The Y position</param>
        /// <param name="_z">The Z position</param>
        /// <param name="_w">The W position</param>
        public Vector4(float _x, float _y, float _z, float _w)
        {
            X = _x;
            Y = _y;
            Z = _z;
            W = _w;
        }

        /// <summary>
        ///     Creates a new 4-D Vector.
        /// </summary>
        /// <param name="vector">The 4-D vector</param>
        public Vector4(Vector4 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
            W = vector.W;
        }

        /// <summary>
        ///     Creates a Quaternion from a Axis/Angle.
        /// </summary>
        /// <param name="vector">The Axis vector</param>
        /// <param name="angle">The Angle</param>
        public Vector4(Vector3 vector, float angle)
        {
            X = (float)(Math.Sin(angle * 0.5f) * vector.X);
            Y = (float)(Math.Sin(angle * 0.5f) * vector.Y);
            Z = (float)(Math.Sin(angle * 0.5f) * vector.Z);
            W = (float)Math.Cos(angle * 0.5f);
        }

        /// <summary>
        ///     Creates a new 4-D Vector.
        /// </summary>
        public Vector4()
        {
        }

        public static Vector4 Read(Utility utility)
        {
            return new Vector4
            {
                X = utility.ReadFloat(),
                Y = utility.ReadFloat(),
                Z = utility.ReadFloat(),
                W = utility.ReadFloat()
            };
        }

        public void Write(Utility utility)
        {
            utility.Write(X);
            utility.Write(Y);
            utility.Write(Z);
            utility.Write(W);
        }

        /// <summary>
        ///     Converts the Quaternion representation on this Vector to the Euler representation.
        /// </summary>
        /// <returns>The Euler X, Y and Z rotation angles in radians</returns>
        public Vector3 toEuler()
        {
            Vector3 output = new Vector3();

            output.Z = (float)Math.Atan2(2 * (X * Y + Z * W), 1 - 2 * (Y * Y + Z * Z));
            output.Y = -(float)Math.Asin(2 * (X * Z - W * Y));
            output.X = (float)Math.Atan2(2 * (X * W + Y * Z), -(1 - 2 * (Z * Z + W * W)));

            return output;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this == (Vector4)obj;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^
                Y.GetHashCode() ^
                Z.GetHashCode() ^
                W.GetHashCode();
        }

        public static Vector4 operator *(Vector4 a, float b)
        {
            return new Vector4(a.X * b, a.Y * b, a.Z * b, a.W * b);
        }

        public static Vector4 operator *(Vector4 a, Vector4 b)
        {
            return new Vector4(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
        }

        public static bool operator ==(Vector4 a, Vector4 b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
        }

        public static bool operator !=(Vector4 a, Vector4 b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("X:{0}; Y:{1}; Z:{2}; W:{3}", X, Y, Z, W);
        }
    }

    
    /// <summary>
    ///     Matrix, used to transform vertices on a model.
    ///     Transformations includes rotation, translation and scaling.
    /// </summary>
    public class Matrix
    { //4x4
        float[,] matrix;

        public Matrix()
        {
            matrix = new float[4, 4];
            matrix[0, 0] = 1.0f;
            matrix[1, 1] = 1.0f;
            matrix[2, 2] = 1.0f;
            matrix[3, 3] = 1.0f;
        }

        public float M11 { get { return matrix[0, 0]; } set { matrix[0, 0] = value; } }
        public float M12 { get { return matrix[0, 1]; } set { matrix[0, 1] = value; } }
        public float M13 { get { return matrix[0, 2]; } set { matrix[0, 2] = value; } }
        public float M14 { get { return matrix[0, 3]; } set { matrix[0, 3] = value; } }

        public float M21 { get { return matrix[1, 0]; } set { matrix[1, 0] = value; } }
        public float M22 { get { return matrix[1, 1]; } set { matrix[1, 1] = value; } }
        public float M23 { get { return matrix[1, 2]; } set { matrix[1, 2] = value; } }
        public float M24 { get { return matrix[1, 3]; } set { matrix[1, 3] = value; } }

        public float M31 { get { return matrix[2, 0]; } set { matrix[2, 0] = value; } }
        public float M32 { get { return matrix[2, 1]; } set { matrix[2, 1] = value; } }
        public float M33 { get { return matrix[2, 2]; } set { matrix[2, 2] = value; } }
        public float M34 { get { return matrix[2, 3]; } set { matrix[2, 3] = value; } }

        public float M41 { get { return matrix[3, 0]; } set { matrix[3, 0] = value; } }
        public float M42 { get { return matrix[3, 1]; } set { matrix[3, 1] = value; } }
        public float M43 { get { return matrix[3, 2]; } set { matrix[3, 2] = value; } }
        public float M44 { get { return matrix[3, 3]; } set { matrix[3, 3] = value; } }

        public float this[int col, int row]
        {
            get
            {
                return matrix[col, row];
            }
            set
            {
                matrix[col, row] = value;
            }
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            Matrix c = new Matrix();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }
                    c[i, j] = sum;
                }
            }

            return c;
        }

        public static Vector3 operator *(Matrix m, Vector3 v)
        {
            var v4 = new Vector4(v, 1);

            var result = m * v4;

            return new Vector3(result.X, result.Y, result.Z);
        }

        public static Vector4 operator *(Matrix m, Vector4 v)
        {
            return new Vector4
                (
                    m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14 * v.W,
                    m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24 * v.W,
                    m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34 * v.W,
                    m.M41 * v.X + m.M42 * v.Y + m.M43 * v.Z + m.M44 * v.W
                );
        }

        /// <summary>
        ///     Gets the Inverse of the Matrix.
        /// </summary>
        /// <returns></returns>
        public Matrix invert()
        {
            var A3434 = M33 * M44 - M34 * M43;
            var A2434 = M32 * M44 - M34 * M42;
            var A2334 = M32 * M43 - M33 * M42;
            var A1434 = M31 * M44 - M34 * M41;
            var A1334 = M31 * M43 - M33 * M41;
            var A1234 = M31 * M42 - M32 * M41;
            var A3424 = M23 * M44 - M24 * M43;
            var A2424 = M22 * M44 - M24 * M42;
            var A2324 = M22 * M43 - M23 * M42;
            var A3423 = M23 * M34 - M24 * M33;
            var A2423 = M22 * M34 - M24 * M32;
            var A2323 = M22 * M33 - M23 * M32;
            var A1424 = M21 * M44 - M24 * M41;
            var A1324 = M21 * M43 - M23 * M41;
            var A1423 = M21 * M34 - M24 * M31;
            var A1323 = M21 * M33 - M23 * M31;
            var A1224 = M21 * M42 - M22 * M41;
            var A1223 = M21 * M32 - M22 * M31;

            var det = M11 * (M22 * A3434 - M23 * A2434 + M24 * A2334)
                - M12 * (M21 * A3434 - M23 * A1434 + M24 * A1334)
                + M13 * (M21 * A2434 - M22 * A1434 + M24 * A1234)
                - M14 * (M21 * A2334 - M22 * A1334 + M23 * A1234);
            det = 1 / det;

            return new Matrix
            {
                M11 = det * (M22 * A3434 - M23 * A2434 + M24 * A2334),
                M12 = det * -(M12 * A3434 - M13 * A2434 + M14 * A2334),
                M13 = det * (M12 * A3424 - M13 * A2424 + M14 * A2324),
                M14 = det * -(M12 * A3423 - M13 * A2423 + M14 * A2323),
                M21 = det * -(M21 * A3434 - M23 * A1434 + M24 * A1334),
                M22 = det * (M11 * A3434 - M13 * A1434 + M14 * A1334),
                M23 = det * -(M11 * A3424 - M13 * A1424 + M14 * A1324),
                M24 = det * (M11 * A3423 - M13 * A1423 + M14 * A1323),
                M31 = det * (M21 * A2434 - M22 * A1434 + M24 * A1234),
                M32 = det * -(M11 * A2434 - M12 * A1434 + M14 * A1234),
                M33 = det * (M11 * A2424 - M12 * A1424 + M14 * A1224),
                M34 = det * -(M11 * A2423 - M12 * A1423 + M14 * A1223),
                M41 = det * -(M21 * A2334 - M22 * A1334 + M23 * A1234),
                M42 = det * (M11 * A2334 - M12 * A1334 + M13 * A1234),
                M43 = det * -(M11 * A2324 - M12 * A1324 + M13 * A1224),
                M44 = det * (M11 * A2323 - M12 * A1323 + M13 * A1223),
            };
        }

        /// <summary>
        ///     Creates a scaling Matrix with a given 3-D proportion size.
        /// </summary>
        /// <param name="scale">The Scale proportions</param>
        /// <returns></returns>
        public static Matrix scale(Vector3 scale)
        {
            Matrix output = new Matrix
            {
                M11 = scale.X,
                M22 = scale.Y,
                M33 = scale.Z
            };

            return output;
        }

        /// <summary>
        ///     Creates a scaling Matrix with a given 2-D proportion size.
        /// </summary>
        /// <param name="scale">The Scale proportions</param>
        /// <returns></returns>
        public static Matrix scale(Vector2 scale)
        {
            Matrix output = new Matrix
            {
                M11 = scale.X,
                M22 = scale.Y
            };

            return output;
        }

        /// <summary>
        ///     Uniform scales the X/Y/Z axis with the same value.
        /// </summary>
        /// <param name="scale">The Scale proportion</param>
        /// <returns></returns>
        public static Matrix scale(float scale)
        {
            Matrix output = new Matrix
            {
                M11 = scale,
                M22 = scale,
                M33 = scale
            };

            return output;
        }

        /// <summary>
        ///     Rotates about the X axis.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns></returns>
        public static Matrix rotateX(float angle)
        {
            Matrix output = new Matrix
            {
                M22 = (float)Math.Cos(angle),
                M32 = -(float)Math.Sin(angle),
                M23 = (float)Math.Sin(angle),
                M33 = (float)Math.Cos(angle)
            };

            return output;
        }

        /// <summary>
        ///     Rotates about the Y axis.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns></returns>
        public static Matrix rotateY(float angle)
        {
            Matrix output = new Matrix
            {
                M11 = (float)Math.Cos(angle),
                M31 = (float)Math.Sin(angle),
                M13 = -(float)Math.Sin(angle),
                M33 = (float)Math.Cos(angle)
            };

            return output;
        }

        /// <summary>
        ///     Rotates about the Z axis.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns></returns>
        public static Matrix rotateZ(float angle)
        {
            Matrix output = new Matrix
            {
                M11 = (float)Math.Cos(angle),
                M21 = -(float)Math.Sin(angle),
                M12 = (float)Math.Sin(angle),
                M22 = (float)Math.Cos(angle)
            };

            return output;
        }

        /// <summary>
        ///     Creates a translation Matrix with the given 3-D position offset.
        /// </summary>
        /// <param name="position">The Position offset</param>
        /// <returns></returns>
        public static Matrix translate(Vector3 position)
        {
            Matrix output = new Matrix
            {
                M41 = position.X,
                M42 = position.Y,
                M43 = position.Z
            };

            return output;
        }

        /// <summary>
        ///     Creates a translation Matrix with the given 2-D position offset.
        /// </summary>
        /// <param name="position">The Position offset</param>
        /// <returns></returns>
        public static Matrix translate(Vector2 position)
        {
            Matrix output = new Matrix
            {
                M31 = position.X,
                M32 = position.Y
            };

            return output;
        }

        public static Matrix Read(Utility utility)
        {
            var output = new Matrix();

            output.M11 = utility.ReadFloat();
            output.M21 = utility.ReadFloat();
            output.M31 = utility.ReadFloat();
            output.M41 = utility.ReadFloat();

            output.M12 = utility.ReadFloat();
            output.M22 = utility.ReadFloat();
            output.M32 = utility.ReadFloat();
            output.M42 = utility.ReadFloat();

            output.M13 = utility.ReadFloat();
            output.M23 = utility.ReadFloat();
            output.M33 = utility.ReadFloat();
            output.M43 = utility.ReadFloat();

            return output;
        }

        public void Write(Utility utility)
        {
            utility.Write(M11);
            utility.Write(M21);
            utility.Write(M31);
            utility.Write(M41);

            utility.Write(M12);
            utility.Write(M22);
            utility.Write(M32);
            utility.Write(M42);

            utility.Write(M13);
            utility.Write(M23);
            utility.Write(M33);
            utility.Write(M43);
        }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            for (int Row = 0; Row < 3; Row++)
            {
                for (int Col = 0; Col < 4; Col++)
                {
                    SB.Append(string.Format("M{0}{1}: {2,-16}", Row + 1, Col + 1, this[Col, Row]));
                }

                SB.Append(Environment.NewLine);
            }

            return SB.ToString();
        }
    }

    public class Matrix3x3
    {
        private Matrix m;

        private static readonly Matrix3x3 _identity = new Matrix3x3(new Matrix());

        public static Matrix Identity { get { return _identity; } }

        public float M11 { get { return m.M11; } set { m.M11 = value; } }
        public float M12 { get { return m.M12; } set { m.M12 = value; } }
        public float M13 { get { return m.M13; } set { m.M13 = value; } }

        public float M21 { get { return m.M21; } set { m.M21 = value; } }
        public float M22 { get { return m.M22; } set { m.M22 = value; } }
        public float M23 { get { return m.M23; } set { m.M23 = value; } }

        public float M31 { get { return m.M31; } set { m.M31 = value; } }
        public float M32 { get { return m.M32; } set { m.M32 = value; } }
        public float M33 { get { return m.M33; } set { m.M33 = value; } }

        private Matrix3x3()
        {
            m = new Matrix();
        }

        public Matrix3x3(Matrix Matrix)
        {
            m = Matrix;
        }

        public Matrix3x3(float m11, float m12, float m13,
                         float m21, float m22, float m23,
                         float m31, float m32, float m33)
        {
            m = new Matrix
            {
                M11 = m11,
                M12 = m12,
                M13 = m13,
                M14 = 0,
                M21 = m21,
                M22 = m22,
                M23 = m23,
                M24 = 0,
                M31 = m31,
                M32 = m32,
                M33 = m33,
                M34 = 0,
                M41 = 0,
                M42 = 0,
                M43 = 0,
                M44 = 1
            };
        }

        public Matrix ToMatrix4x4()
        {
            return m;
        }

        public static implicit operator Matrix(Matrix3x3 m)
        {
            return m.ToMatrix4x4();
        }

        public override string ToString()
        {
            return m.ToString();
        }

        public static Matrix3x3 Read(Utility utility)
        {
            var m = new Matrix3x3();

            // CHECKME -- row major or column major???

            m.M11 = utility.ReadFloat();
            m.M12 = utility.ReadFloat();
            m.M13 = utility.ReadFloat();

            m.M21 = utility.ReadFloat();
            m.M22 = utility.ReadFloat();
            m.M23 = utility.ReadFloat();

            m.M31 = utility.ReadFloat();
            m.M32 = utility.ReadFloat();
            m.M33 = utility.ReadFloat();

            return m;
        }

        public void Write(Utility utility)
        {
            // CHECKME -- row major or column major???

            utility.Write(m.M11);
            utility.Write(m.M12);
            utility.Write(m.M13);

            utility.Write(m.M21);
            utility.Write(m.M22);
            utility.Write(m.M23);

            utility.Write(m.M31);
            utility.Write(m.M32);
            utility.Write(m.M33);
        }
    }
}
