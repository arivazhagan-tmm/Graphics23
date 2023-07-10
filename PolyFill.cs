namespace GrayBMP {
   public class PolyFill {
      public PolyFill (List<Polygon> polygons, List<Point2D> vertices) => (mPolygons, mVertices) = (polygons, vertices);
      List<Polygon> mPolygons;
      List<Point2D> mVertices;
      /// <summary> Fills each polygon based on intersection algorithm</summary>
      public void Fill (GrayBMP bmp, int color) {
         bmp.Begin ();
         int yMin = mVertices.Select (v => v.Y).Min (),
             yMax = mVertices.Select (v => v.Y).Max ();
         double start = yMin + 1, end = yMax - yMin;
         for (; start < end; start++) {
            var intersectPolys = mPolygons.Where (p => p.Vertices[0].Y <= start && start <= p.Vertices[^1].Y).ToList ();
            if (intersectPolys.Count > 0) {
               foreach (var poly in intersectPolys) {
                  var intersectPts = new List<Point2D> ();
                  foreach (var line in poly.Lines) {
                     var tmp = line.CLPts.Where (p => p.Y == start).ToList ();
                     tmp = tmp.OrderBy (p => p.X).DistinctBy (p => p.X).ToList ();
                     intersectPts.AddRange (tmp);
                  }
                  if (intersectPts.Count > 0) {
                     for (int j = 0; j < intersectPts.Count - 1; j++)
                        bmp.DrawLine (intersectPts[j], intersectPts[j + 1], color);
                  }
               }
            }
         }
         bmp.End ();
      }
   }
}
