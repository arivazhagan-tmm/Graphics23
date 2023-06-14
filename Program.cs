using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace A25;

class Point2D {
   public readonly int X;
   public readonly int Y;
   public Point2D (double x, double y) => (X, Y) = ((int)x, (int)y);
   public static double Distance (Point2D p1, Point2D p2) => Math.Sqrt (Math.Pow (p2.X - p1.X, 2) + Math.Pow (p2.Y - p1.Y, 2));
}

class MyWindow : Window {
   public MyWindow () {
      Width = 800; Height = 600;
      Left = 50; Top = 50;
      WindowStyle = WindowStyle.None;
      Image image = new Image () {
         Stretch = Stretch.None,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
      RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);

      mBmp = new WriteableBitmap ((int)Width, (int)Height,
         96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      image.Source = mBmp;
      Content = image;
      MouseMove += OnMouseMove;
      MouseLeftButtonDown += OnMouseLeftButtonDown;
      //DrawMandelbrot (-0.5, 0, 1);
   }
   void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
      var pos = e.GetPosition (this);
      if (mStartPt == null)
         mStartPt = new (pos.X, pos.Y);
      else {
         mEndPt = new (pos.X, pos.Y);
         DrawLine (mStartPt, mEndPt);
         mStartPt = null;
      }
   }

   void DrawLine (Point2D startPt, Point2D endPt) {
      var pts = GetCLPoints (startPt, endPt);
      foreach (var pt in pts) {
         try {
            mBmp.Lock ();
            mBase = mBmp.BackBuffer;
            SetPixel (pt.X, pt.Y, 255);
            mBmp.AddDirtyRect (new Int32Rect (pt.X, pt.Y, 1, 1));
         } finally {
            mBmp.Unlock ();
         }
      }
   }

   /// <summary> Returns list of co-linear points lie between start point and end point </summary>
   List<Point2D> GetCLPoints (Point2D startPt, Point2D endPt) {
      List<Point2D> pts = new ();
      var dist = Point2D.Distance (startPt, endPt);
      var dx = (endPt.X - startPt.X) / dist;
      var dy = (endPt.Y - startPt.Y) / dist;
      for (int i = 1; i <= (int)dist; i++) {
         var pt = new Point2D (startPt.X + dx * i, startPt.Y + dy * i);
         pts.Add (pt);
      }
      return pts;
   }

   void DrawMandelbrot (double xc, double yc, double zoom) {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         int dx = mBmp.PixelWidth, dy = mBmp.PixelHeight;
         double step = 2.0 / dy / zoom;
         double x1 = xc - step * dx / 2, y1 = yc + step * dy / 2;
         for (int x = 0; x < dx; x++) {
            for (int y = 0; y < dy; y++) {
               Complex c = new Complex (x1 + x * step, y1 - y * step);
               SetPixel (x, y, Escape (c));
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, dx, dy));
      } finally {
         mBmp.Unlock ();
      }
   }

   byte Escape (Complex c) {
      Complex z = Complex.Zero;
      for (int i = 1; i < 32; i++) {
         if (z.NormSq > 4) return (byte)(i * 8);
         z = z * z + c;
      }
      return 0;
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         try {
            mBmp.Lock ();
            mBase = mBmp.BackBuffer;
            var pt = e.GetPosition (this);
            int x = (int)pt.X, y = (int)pt.Y;
            SetPixel (x, y, 255);
            mBmp.AddDirtyRect (new Int32Rect (x, y, 1, 1));
         } finally {
            mBmp.Unlock ();
         }
      }
   }

   void DrawGraySquare () {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         for (int x = 0; x <= 255; x++) {
            for (int y = 0; y <= 255; y++) {
               SetPixel (x, y, (byte)x);
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, 256, 256));
      } finally {
         mBmp.Unlock ();
      }
   }

   void SetPixel (int x, int y, byte gray) {
      unsafe {
         var ptr = (byte*)(mBase + y * mStride + x);
         *ptr = gray;
      }
   }

   WriteableBitmap mBmp;
   int mStride;
   nint mBase;
   Point2D? mStartPt; // start point of line
   Point2D? mEndPt; // end point of line
}

internal class Program {
   [STAThread]
   static void Main (string[] args) {
      Window w = new MyWindow ();
      w.Show ();
      Application app = new Application ();
      app.Run ();
   }
}
