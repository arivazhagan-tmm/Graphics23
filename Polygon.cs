namespace GrayBMP {
   public readonly struct Polygon {
      public Polygon (List<Line> lines) {
         Lines = lines;
         Vertices = lines.Select (l => l.StartPt).OrderBy (v => v.Y).ThenBy (v => v.X).ToList ();
      }
      public readonly List<Point2D> Vertices;
      public readonly List<Line> Lines;
   }
   public readonly struct Line {
      public Line (int x0, int y0, int x1, int y1) {
         StartPt = new (x0, y0);
         EndPt = new (x1, y1);
         CLPts = Point2D.GetCLPoints (StartPt, EndPt).ToArray ();
      }
      public readonly Point2D[] CLPts;
      public readonly Point2D StartPt;
      public readonly Point2D EndPt;
   }
   public readonly struct Point2D {
      public Point2D (int x, int y) => (X, Y) = (x, y);
      public static Point2D Origin => new (0, 0);
      public readonly int X;
      public readonly int Y;
      public bool IsEqual (Point2D p) => p.X == X && p.Y == Y;
      public static double Distance (Point2D p1, Point2D p2) => Math.Sqrt (Math.Pow (p2.X - p1.X, 2) + Math.Pow (p2.Y - p1.Y, 2));
      public static List<Point2D> GetCLPoints (Point2D p1, Point2D p2) {
         List<Point2D> pts = new () { p1, p2 };
         double n = Distance (p1, p2);
         double dx = (p2.X - p1.X) / n,
                dy = (p2.Y - p1.Y) / n;
         for (int i = 1; i <= (int)n; i++) {
            var pt = p1 + (dx * i, dy * i);
            if (!pts.Any (pt.IsEqual))
               pts.Add (pt);
         }
         return pts;
      }
      public static Point2D operator + (Point2D p1, (double dx, double dy) del) => new (p1.X + (int)del.dx, p1.Y + (int)del.dy);
   }
}
