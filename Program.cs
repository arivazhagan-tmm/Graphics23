using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static System.Math;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace A25;

class Point2D {
   public readonly int X;
   public readonly int Y;
   public Point2D (double x, double y) => (X, Y) = ((int)(x + 0.5), (int)(y + 0.5));
   public static double Distance (Point2D p1, Point2D p2) => Sqrt (Pow (p2.X - p1.X, 2) + Pow (p2.Y - p1.Y, 2));
   public override string ToString () => $"({X},{Y})";
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
         DrawLine (mStartPt, mEndPt, 255);
         mStartPt = null;
      }
   }

   void DrawLine (Point2D startPt, Point2D endPt, byte color) {
      (int x0, int y0, int x1, int y1) = (startPt.X, startPt.Y, endPt.X, endPt.Y);
      int xMin = Min (x0, x1), yMin = Min (y0, y1), xMax = Max (x0, x1), yMax = Max (y0, y1);
      bool steepLine = yMax - yMin > xMax - xMin;
      if (steepLine)
         (x0, y0, x1, y1) = (y0, x0, y1, x1);
      if (x0 > x1)
         (x0, y0, x1, y1) = (x1, y1, x0, y0);
      int dx = x1 - x0, dy = y1 - y0, delY;
      (dy, delY) = (dy < 0) ? (-dy, -1) : (dy, 1);
      int x = x0, y = y0, error = (2 * dy) - dx;
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         while (x <= x1) {
            if (steepLine)
               SetPixel (y, x, color);
            else
               SetPixel (x, y, color);
            x++;
            if (error < 0) {
               error += 2 * dy;
            } else {
               y += delY;
               error += 2 * (dy - dx);
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (xMin, yMin, xMax - xMin + 1, yMax - yMin + 1));
      } finally {
         mBmp.Unlock ();
      }
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
