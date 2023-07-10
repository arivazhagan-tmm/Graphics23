// Program.cs - Entry point into the GrayBMP application   
// ---------------------------------------------------------------------------------------
using System.IO;
using System.Windows;
namespace GrayBMP;

class Program {
   [STAThread]
   static void Main () {
      // Create a LinesWin that demonstrates the Line Drawing
      var lineWin = new LinesWin ();
      var text = File.ReadAllLines ("C:/etc/leaf-fill.txt");
      int len = text.Length;
      var lines = new Line[len];
      var polygons = new List<Polygon> ();
      int count = 0;
      for (int i = 0; i < len; i++) {
         var s = text[i].Split (' ').Select (t => Convert.ToInt32 (t)).ToArray ();
         lines[i] = new (s[0], s[1], s[2], s[3]);
         if (lines[count].StartPt.IsEqual (lines[i].EndPt)) {
            polygons.Add (new (lines[count..(i + 1)].ToList ()));
            count = i + 1;
         }
      }
      polygons = polygons.Where (p => p.Lines.Count > 2).ToList ();
      var vertices = lines.Select (l => l.StartPt).ToList ();
      lineWin.InitiatePolyFill (polygons, vertices);
      lineWin.Show ();
      new Application ().Run ();
   }
   [STAThread]
   static void Main1 () {
      // Create a MandelWin that shows an animated Mandelbrot set,
      // and create an Application object to do message-pumping and keep
      // the window alive
      new MandelWin ().Show ();
      new Application ().Run ();
   }
}
