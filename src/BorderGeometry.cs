using System;
using System.Drawing;
namespace WindowSweaters
{
  internal static class BorderGeometry
  {
    internal static int Thickness(uint dpi) {
      return Thickness(dpi,18);
    }
    internal static int Thickness(uint dpi,double width) {
      return Math.Max(2,(int)Math.Round(width*Math.Max(96u,dpi)/96.0));
    }
    internal static bool ShouldDecorate(bool visible,bool minimized,bool cloaked,bool own,bool normalClass,bool framed,bool fullscreen)
    {
      return visible && !minimized && !cloaked && !own && normalClass && framed && !fullscreen;
    }
    // Order is top, bottom, left, right. The side strips omit the corners.
    internal static Rectangle[] Calculate(Rectangle frame,int thickness,bool maximized) {
      return Calculate(frame,thickness,maximized,0);
    }
    internal static Rectangle[] Calculate(Rectangle frame,int thickness,bool maximized,int radius)
    {
      if(thickness<=0 || frame.Width<=2*thickness || frame.Height<=2*thickness || frame.Width>20000 || frame.Height>20000) return new Rectangle[0];
      Rectangle outer=frame;
      if(!maximized) outer.Inflate(thickness,thickness);
      int cap=thickness+radius;
      if(outer.Width<=2*cap||outer.Height<=2*cap)return new Rectangle[0];
      return new Rectangle[] {
        new Rectangle(outer.Left,outer.Top,outer.Width,cap),
        new Rectangle(outer.Left,outer.Bottom-cap,outer.Width,cap),
        new Rectangle(outer.Left,outer.Top+cap,cap,outer.Height-2*cap),
        new Rectangle(outer.Right-cap,outer.Top+cap,cap,outer.Height-2*cap)
      };
    }
  }
}
