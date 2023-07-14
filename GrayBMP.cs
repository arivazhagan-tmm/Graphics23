// GrayBMP.cs - Contains the GrayBMP class (implementation of grayscale bitmp on top
// of a WPF WriteableBitmap class)
// ---------------------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace GrayBMP;
using static System.Math;

#region class GrayBitmap -------------------------------------------------------
/// <summary>Implements a writeable grayscale bitmap</summary>
class GrayBMP {
   #region Constructor --------------------------------------
   /// <summary>Constructs a grayscale (8 bits-per-pixel) bitmap of given size</summary>
   public GrayBMP (double width, double height) {
      mBmp = new WriteableBitmap (mWidth = (int)width, mHeight = (int)height, 96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      mBuffer = mBmp.BackBuffer;
   }
   #endregion

   #region Properties ---------------------------------------
   /// <summary>The underlying WriteableBitmap</summary>
   public WriteableBitmap Bitmap => mBmp;

   /// <summary>Pointer to the bitmap's buffer - you can obtain this only after a Begin</summary>
   public nint Buffer {
      get {
         if (mcLocks == 0) Fatal ("Buffer access outside Begin() / End()");
         return mBuffer;
      }
   }

   /// <summary>Height of the bitmap, in pixels</summary>
   public int Height => mHeight;

   /// <summary>The back-buffer stride for this bitmap</summary>
   public int Stride => mStride;

   /// <summary>Width of the bitmap, in pixels</summary>
   public int Width => mWidth;
   #endregion

   #region Methods -----------------------------------------
   /// <summary>Call Begin before you obtain the Buffer to update the bitmap</summary>
   public nint Begin () {
      if (mcLocks++ == 0) {
         mBmp.Lock ();
         mX0 = mY0 = int.MaxValue;
         mX1 = mY1 = int.MinValue;
      }
      return mBmp.BackBuffer;
   }

   /// <summary>Clear the bitmap to a given shade of gray</summary>
   public void Clear (int gray) {
      Begin ();
      unsafe {
         var ptr = (byte*)Buffer;
         System.Runtime.CompilerServices.Unsafe.InitBlock (ref *ptr, (byte)gray, (uint)(mHeight * mStride));
         Dirty (0, 0, mWidth - 1, mHeight - 1);
      }
      End ();
   }

   /// <summary>Tags a pixel as dirty</summary>
   public void Dirty (int x, int y) {
      if (x < mX0) mX0 = x; if (x > mX1) mX1 = x;
      if (y < mY0) mY0 = y; if (y > mY1) mY1 = y;
   }

   /// <summary>Tags a rectangle as dirty (x1, x2, y1, y2 need not be 'ordered')</summary>
   public void Dirty (int x1, int y1, int x2, int y2) {
      Dirty (x1, y1); Dirty (x2, y2);
   }
   /// <summary>
   /// Tags the entire bitmap as dirty
   /// </summary>
   public void Dirty ()
      => Dirty (0, 0, Width - 1, Height - 1);

   /// <summary>Draws a line between the given endpoints, with the given shade of gray</summary>
   public void DrawLine (int x1, int y1, int x2, int y2, int gray) {
      if (y1 == y2) { DrawHorizontalLine (x1, x2, y1, gray); return; }
      Begin ();
      int dx = Abs (x2 - x1), dy = -Abs (y2 - y1), error = dx + dy;
      int stepX = x1 < x2 ? 1 : -1, stepY = y1 < y2 ? 1 : -1;
      int stepYPtr = stepY * mStride;
      Check (x1, y1); Check (x2, y2); Dirty (x1, y1, x2, y2);
      byte bGray = (byte)gray;

      unsafe {
         byte* ptr = (byte*)(Buffer + y1 * mStride + x1);
         while (true) {
            *ptr = bGray;
            if (x1 == x2 && y1 == y2) break;
            int delta = 2 * error;
            if (delta >= dy) {
               if (x1 == x2) break;
               error += dy;
               x1 += stepX; ptr += stepX;
            }
            if (delta <= dx) {
               if (y1 == y2) break;
               error += dx;
               y1 += stepY; ptr += stepYPtr;
            }
         }
      }
      End ();
   }

   /// <summary>
   /// Draws a horizontal line between the two given end-points (with given shade of gray)
   /// </summary>
   public void DrawHorizontalLine (int x1, int x2, int y, int gray) {
      Begin ();
      Check (x1, y); Check (x2, y); Dirty (x1, y, x2, y);
      byte bGray = (byte)gray;
      if (x1 > x2) (x1, x2) = (x2, x1);
      unsafe {
         byte* ptr = (byte*)(Buffer + y * Stride + x1);
         for (int i = x2 - x1; i >= 0; i--)
            *ptr++ = bGray;
      }
      End ();
   }

   /// <summary> Draws a line with given pixel thickness</summary>
   public void DrawThickLine (int x0, int y0, int x1, int y1, int width, int color) {
      Point2D startPt = new (x0, y0),
              endPt = new (x1, y1);
      double offset = width * 0.5;
      for (int i = 1; i <= offset; i++) {
         var vertices = Point2D.GetPolygonVertices (startPt, endPt, i);
         for (int j = 0; j < vertices.Length - 1; j++) {
            Point2D vertex1 = vertices[j],
                    vertex2 = vertices[j + 1];
            DrawLine ((int)vertex1.X, (int)vertex1.Y, (int)vertex2.X, (int)vertex2.Y, color);
         }
      }
   }

   /// <summary>Call End after finishing the update of the bitmap</summary>
   public void End () {
      if (--mcLocks == 0) {
         if (mcLocks < 0) Fatal ("Unexpected call to GrayBitmap.End()");
         if (mX1 >= mX0 && mY1 >= mY0)
            mBmp.AddDirtyRect (new Int32Rect (mX0, mY0, mX1 - mX0 + 1, mY1 - mY0 + 1));
         mBmp.Unlock ();
      }
   }

   /// <summary>Set a given pixel to a shade of gray</summary>
   public void SetPixel (int x, int y, int gray) {
      Check (x, y); Dirty (x, y);
      var ptr = Begin () + y * mStride + x;
      unsafe { *(byte*)ptr = (byte)gray; };
      End ();
   }

   /// <summary>Set a given pixel to a shade of gray</summary>
   void SetPixelFast (int x, int y, int gray) {
      var ptr = Buffer + y * mStride + x;
      unsafe { *(byte*)ptr = (byte)gray; };
   }
   #endregion

   #region Implementation ----------------------------------
   void Check (int x, int y) {
      if (x < 0 || x >= mWidth || y < 0 || y >= mHeight)
         Fatal ($"Pixel location out of range: ({x},{y})");
   }

   // Helper to throw an exception on invalid usage
   void Fatal (string message)
      => throw new InvalidOperationException (message);

   readonly int mWidth, mHeight, mStride;
   readonly WriteableBitmap mBmp;
   readonly nint mBuffer;
   int mX0, mY0, mX1, mY1;    // The 'dirty rectangle'
   int mcLocks;               // Number of unmatched Begin() calls
   #endregion
}
#endregion

#region Struct Point2D

record struct Point2D (double X, double Y) {
   /// <summary> Returns the projected point at given distance at given angle</summary>
   public static Point2D Project (Point2D p, double d, double theta) => new (p.X + (d * Cos (theta)), p.Y + (d * Sin (theta)));
   /// <summary> Returns vertices of polygon which encloses the given two points</summary>
   public static Point2D[] GetPolygonVertices (Point2D startPt, Point2D endPt, double offset) {
      double angle = Atan2 (endPt.Y - startPt.Y, endPt.X - startPt.X);
      var pts = new Point2D[8];
      int count = 0;
      double startAngle = PI / 2,
             endAngle = 3 * PI / 2,
             increment = PI / 3;
      for (double i = startAngle; i <= endAngle; i += increment) {
         pts[count++] = Project (startPt, offset, angle + i);
         pts[count++] = Project (endPt, -offset, angle + i);
      }
      return new Point2D[] { pts[0], pts[2], pts[4], pts[6], pts[1], pts[3], pts[5], pts[7], pts[0] };
   }
}

#endregion
