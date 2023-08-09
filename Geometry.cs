// Geometry.cs - Contains some basic Geometry structs (Complex numbers, Points, Vectors)
// ---------------------------------------------------------------------------------------
using static System.Math;
using static GrayBMP.Orientation;
namespace GrayBMP;

/// <summary>A number in the complex plane of the form (X + iY)</summary>
readonly record struct Complex (double X, double Y) {
   public double Norm => Math.Sqrt (X * X + Y * Y);
   public double NormSq => X * X + Y * Y;

   public static readonly Complex Zero = new (0, 0);

   public static Complex operator + (Complex a, Complex b)
      => new (a.X + b.X, a.Y + b.Y);
   public static Complex operator * (Complex a, Complex b)
      => new (a.X * b.X - a.Y * b.Y, a.X * b.Y + a.Y * b.X);
}

/// <summary>A point in 2D space, with double-precision coordinates (X, Y)</summary>
readonly record struct Point2 (double X, double Y) {
   public (int X, int Y) Round () => ((int)(X + 0.5), (int)(Y + 0.5));

   public double AngleTo (Point2 b) => Math.Atan2 (b.Y - Y, b.X - X);
   public Point2 RadialMove (double r, double th) => new (X + r * Cos (th), Y + r * Sin (th));
   public double DistanceTo (Point2 p) => Sqrt (Pow (p.X - X, 2) + Pow (p.Y - Y, 2));
   /// <summary> Checks the point is inside the polygon or not</summary>
   public bool IsInside (Polygon poly) {
      var pt = new Point2 (poly.Bound.X1 + 1, Y);
      var hLine = new Edge (this, pt); // Horizontal line
      var intersections = poly.Edges.Where (e => e.IsIntersect (hLine)).Count ();
      return intersections % 2 != 0;
   }
   /// <summary> Returns the point located far away</summary>
   public Point2 FarthestPoint (List<Point2> pts, out double distance) {
      var count = pts.Count;
      var dist = new double[count]; // Distances
      for (int i = 0; i < count; i++)
         dist[i] = DistanceTo (pts[i]);
      distance = dist.Max ();
      var index = dist.ToList ().IndexOf (distance);
      return pts[index];
   }
   /// <summary> Returns centroid given points</summary>
   public static Point2 Centroid (IEnumerable<Point2> pts) {
      double x = 0, y = 0;
      int count = pts.Count ();
      pts.ToList ().ForEach (p => { x += p.X; y += p.Y; });
      return new Point2 (x / count, y / count);
   }
   /// <summary> Returns convex hull around the given points using intersection logic</summary>
   public static Point2[] ConvexHull (Point2[] pts) {
      var refPoint = pts[0];
      var convHull = new List<Point2> { refPoint };
      var poly = new Polygon (pts);
      for (int i = 1, len = pts.Length; i < len; i++) {
         var tmp = pts[i];
         var angle = refPoint.AngleTo (tmp);
         Point2 p = tmp.RadialMove (0.5, angle);
         if (!p.IsInside (poly)) {
            convHull.Add (tmp);
            refPoint = tmp;
         }
      }
      return convHull.ToArray ();
   }
   /// <summary> Returns convex hull around the given points using graham's scan</summary>
   public static Point2[] ConvexHull (List<Point2> pts) {
      Point2 refPoint = pts[0];
      pts.Sort ((p1, p2) => {
         return refPoint.Orientation (p1, p2) switch {
            Colinear => 0,
            Clockwise => 1,
            _ => -1,
         };
      });
      var convHull = new List<Point2> () { refPoint, pts[1] };
      for (int i = 2, count = pts.Count; i < count;) {
         while (convHull.Count >= 2 && convHull[^2].Orientation (convHull[^1], pts[i]) is Clockwise)
            convHull.RemoveAt (convHull.Count - 1);
         convHull.Add (pts[i++]);
      }
      return convHull.ToArray ();
   }
   /// <summary> Returns orientation of point comparing with given point1 and point2</summary>
   public Orientation Orientation (Point2 p1, Point2 p2) {
      Vector2 v1 = p1 - this, v2 = p2 - p1;
      double value = v1.Y * v2.X - v1.X * v2.Y;
      return value.CompareTo (0.0) switch {
         0 => Colinear,
         > 0 => Clockwise,
         _ => CounterClockwise,
      };
   }
   public static Vector2 operator - (Point2 a, Point2 b) => new (a.X - b.X, a.Y - b.Y);
   public static Point2 operator + (Point2 p, Vector2 v) => new (p.X + v.X, p.Y + v.Y);
}

/// <summary>A Vector2 in 2D space</summary>
readonly record struct Vector2 (double X, double Y) {
   /// <summary>Length of the vector</summary>
   public double Length => Sqrt (X * X + Y * Y);

   public double Dot (Vector2 b) => X * b.X + Y * b.Y;

   public double ZCross (Vector2 b) => X * b.Y - b.X * Y;

   public static Vector2 operator + (Vector2 a, Vector2 b) => new (a.X + b.X, a.Y + b.Y);
   public static Vector2 operator * (Vector2 a, double f) => new (a.X * f, a.Y * f);
   public static Vector2 operator - (Vector2 a) => new (-a.X, -a.Y);
}

class Matrix2 {
   public Matrix2 (double m11, double m12, double m21, double m22, double dx, double dy)
      => (M11, M12, M21, M22, DX, DY) = (m11, m12, m21, m22, dx, dy);

   public static Matrix2 Translation (Vector2 v)
      => new (1, 0, 0, 1, v.X, v.Y);
   public static Matrix2 Scaling (double f)
      => new (f, 0, 0, f, 0, 0);
   public static Matrix2 Rotation (double theta) {
      var (s, c) = (Sin (theta), Cos (theta));
      return new (c, s, -s, c, 0, 0);
   }

   public static Point2 operator * (Point2 p, Matrix2 m)
      => new (p.X * m.M11 + p.Y * m.M21 + m.DX, p.X * m.M12 + p.Y * m.M22 + m.DY);

   public static Matrix2 operator * (Matrix2 a, Matrix2 b)
      => new (a.M11 * b.M11 + a.M12 * b.M21, a.M11 * b.M12 + a.M12 * b.M22,
              a.M21 * b.M11 + a.M22 * b.M21, a.M21 * b.M12 + a.M22 * b.M22,
              a.DX * b.M11 + a.DY * b.M21 + b.DX, a.DX * b.M12 + a.DY * b.M22 + b.DY);

   public readonly double M11, M12, M21, M22, DX, DY;
}

/// <summary>Represents a bounding box in 2 dimensions</summary>
readonly struct Bound2 {
   /// <summary>Compute the bound of a set of points</summary>
   public Bound2 (IEnumerable<Point2> pts) {
      X0 = Y0 = double.MaxValue; X1 = Y1 = double.MinValue;
      foreach (var (x, y) in pts) {
         X0 = Min (X0, x); Y0 = Min (Y0, y);
         X1 = Max (X1, x); Y1 = Max (Y1, y);
      }
   }

   public override string ToString ()
      => $"{Round (X0, 3)},{Round (Y0, 3)} to {Round (X1, 3)},{Round (Y1, 3)}";

   /// <summary>Compute the overall bound of a set of bounds (union)</summary>
   public Bound2 (IEnumerable<Bound2> bounds) {
      X0 = Y0 = double.MaxValue; X1 = Y1 = double.MinValue;
      foreach (var b in bounds) {
         X0 = Min (X0, b.X0); Y0 = Min (Y0, b.Y0);
         X1 = Max (X1, b.X1); Y1 = Max (Y1, b.Y1);
      }
   }

   public double Width => X1 - X0;
   public double Height => Y1 - Y0;
   public Point2 Midpoint => new ((X0 + X1) / 2, (Y0 + Y1) / 2);

   public bool IsEmpty => X0 >= X1;
   public readonly double X0, Y0, X1, Y1;
}

readonly record struct Edge (Point2 startPt, Point2 endPt) {
   public readonly Point2 StartPoint => startPt;
   public readonly Point2 EndPoint => endPt;
   public bool IsIntersect (Edge other) {
      var isIntersect = false;
      var yCord = other.StartPoint.Y;
      bool condition1 = yCord < EndPoint.Y && yCord > StartPoint.Y,
           condition2 = yCord < StartPoint.Y && yCord > EndPoint.Y;
      if (condition1 || condition2) {
         var factor = (yCord - startPt.Y) / (endPt.Y - startPt.Y);
         var xIntersect = startPt.X + (endPt.X - startPt.X) * factor;
         if (xIntersect > other.StartPoint.X)
            isIntersect = true;
      }
      return isIntersect;
   }
   public override string ToString () => $"({StartPoint}, {EndPoint})";
}

/// <summary>A Polygon is a set of points making a closed shape</summary>
class Polygon {
   public Polygon (IEnumerable<Point2> pts) {
      mPts = pts.ToArray ();
      mCentroid = Point2.Centroid (mPts);
      int len = mPts.Length;
      mEdges = new Edge[len];
      mEdges[^1] = new Edge (mPts[^1], mPts[0]);
      for (int i = 0; i < len - 1; i++)
         mEdges[i] = new (mPts[i], mPts[i + 1]);
   }
   public IReadOnlyList<Point2> Pts => mPts;
   public IReadOnlyList<Edge> Edges => mEdges;
   public Point2 Centroid => mCentroid;
   readonly Point2[] mPts;
   readonly Edge[] mEdges;
   readonly Point2 mCentroid;
   /// <summary>The bound of the polygon</summary>
   public Bound2 Bound {
      get {
         if (mBound.IsEmpty) mBound = new Bound2 (mPts);
         return mBound;
      }
   }
   Bound2 mBound;

   public static Polygon operator * (Polygon p, Matrix2 m)
      => new Polygon (p.Pts.Select (a => a * m));

   /// <summary>Enumerate all the 'lines' in this Polygon</summary>
   public IEnumerable<(Point2 A, Point2 B)> EnumLines (Matrix2 xfm) {
      Point2 p0 = mPts[^1] * xfm;
      for (int i = 0, n = mPts.Length; i < n; i++) {
         Point2 p1 = mPts[i] * xfm;
         yield return (p0, p1);
         p0 = p1;
      }
   }
}

/// <summary>A drawing is a collection of polygons</summary>
class Drawing {
   public void Add (Polygon poly) {
      mPolys.Add (poly);
      mBound = new ();
   }

   /// <summary> Updates covnex hull around all outer polygons</summary>
   public void UpdateConvexHull () {
      if (mPolys.Count == 1) {
         mConvexHull = Point2.ConvexHull (mPolys[0].Pts.ToArray ());
         return;
      }
      var polyCentroids = mPolys.Select (p => p.Centroid).ToList ();
      var commonCentroid = Point2.Centroid (polyCentroids);
      commonCentroid.FarthestPoint (polyCentroids, out double distance);
      // Taking 50% of farthest point's distance as limit to find outer polygons
      var distLimit = distance * 0.5;
      var outerPolys = mPolys.Where (p => p.Centroid.DistanceTo (commonCentroid) > distLimit).ToList ();
      var outerPts = new Point2[outerPolys.Count];
      for (int i = 0; i < outerPolys.Count; i++)
         outerPts[i] = commonCentroid.FarthestPoint (outerPolys[i].Pts.ToList (), out distance);
      mConvexHull = Point2.ConvexHull (outerPts.ToList ());
   }

   public IReadOnlyList<Polygon> Polys => mPolys;
   List<Polygon> mPolys = new ();
   Point2[] mConvexHull;

   public static Drawing operator * (Drawing d, Matrix2 m) {
      Drawing d2 = new Drawing ();
      foreach (var p in d.Polys) d2.Add (p * m);
      return d2;
   }

   public Bound2 Bound {
      get {
         if (mBound.IsEmpty) mBound = new (Polys.Select (a => a.Bound));
         return mBound;
      }
   }
   Bound2 mBound;

   public Bound2 GetBound (Matrix2 xfm) => new (mConvexHull.Select (p => p * xfm));
   public IEnumerable<(Point2 A, Point2 B)> ConvexLines (Matrix2 xfm) {
      Point2 p0 = mConvexHull[^1] * xfm;
      for (int i = 0, n = mConvexHull.Length; i < n; i++) {
         Point2 p1 = mConvexHull[i] * xfm;
         yield return (p0, p1);
         p0 = p1;
      }
   }
   /// <summary>Enumerate all the lines in this drawing</summary>
   public IEnumerable<(Point2 A, Point2 B)> EnumLines (Matrix2 xfm)
      => mPolys.SelectMany (a => a.EnumLines (xfm));
}

public enum Orientation { Colinear, Clockwise, CounterClockwise }
