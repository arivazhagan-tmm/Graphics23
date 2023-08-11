// Geometry.cs - Contains some basic Geometry structs (Complex numbers, Points, Vectors)
// ---------------------------------------------------------------------------------------
using static System.Math;
namespace GrayBMP;

/// <summary>A number in the complex plane of the form (X + iY)</summary>
readonly struct Complex {
   public Complex (double x, double y) => (X, Y) = (x, y);
   public override string ToString () => $"{X} + i{Y}";

   public double Norm => Math.Sqrt (X * X + Y * Y);
   public double NormSq => X * X + Y * Y;

   public static readonly Complex Zero = new (0, 0);

   public static Complex operator + (Complex a, Complex b)
      => new (a.X + b.X, a.Y + b.Y);
   public static Complex operator * (Complex a, Complex b)
      => new (a.X * b.X - a.Y * b.Y, a.X * b.Y + a.Y * b.X);

   public readonly double X, Y;
}

/// <summary>A point in 2D space, with double-precision coordinates (X, Y)</summary>
readonly record struct Point2 (double X, double Y) {
   public (int X, int Y) Round () => ((int)(X + 0.5), (int)(Y + 0.5));
   /// <summary> Returns the projected point at given distance at given angle</summary>
   public static Point2 Project (Point2 p, double distance, double theta) => new (p.X + (distance * Cos (theta)), p.Y + (distance * Sin (theta)));
   /// <summary> Returns vertices of polygon which encloses the given two points</summary>
   public static Point2[] GetPolygonVertices (Point2 p1, Point2 p2, double offset) {
      var vertices = new Point2[8];
      double angle = Atan2 (p2.Y - p1.Y, p2.X - p1.X), radFactor = PI / 180; // Factor to convert degree to radians
      int index = 0, proAngle = 90; // Projection Angle
      for (; proAngle <= 270; proAngle += 60) {
         double tmpAngle = angle + proAngle * radFactor;
         vertices[index] = Project (p1, offset, tmpAngle);
         vertices[index + 4] = Project (p2, -offset, tmpAngle);
         index++;
      }
      return vertices;
   }
}

/// <summary>A Line in 2 dimensions (A -> B)</summary>
readonly record struct Line (Point2 A, Point2 B);

/// <summary>A drawing is a collection of lines</summary>
class Drawing {
   public void AddLine (Line line) => mLines.Add (line);

   public IReadOnlyList<Line> Lines => mLines;
   List<Line> mLines = new ();
}
