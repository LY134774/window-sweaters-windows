using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace WindowSweaters
{
  internal sealed class BorderWindow : Form
  {
    Rectangle lastBounds;
    internal BorderWindow() {
      FormBorderStyle=FormBorderStyle.None;
      ShowInTaskbar=false;
      StartPosition=FormStartPosition.Manual;
      AutoScaleMode=AutoScaleMode.None;
      Text="针织边框图层";
    }
    protected override bool ShowWithoutActivation {
      get {
        return true;
      }
    }
    protected override CreateParams CreateParams {
      get {
        CreateParams p=base.CreateParams;
        p.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8000000 | 0x8;
        return p;
      }
    }
    protected override void WndProc(ref Message m) {
      if(m.Msg==0x84) {
        m.Result=new IntPtr(-1);
        return;
      }
      if(m.Msg==0x21) {
        m.Result=new IntPtr(3);
        return;
      }
      if(m.Msg==0x2E0) {
        m.Result=IntPtr.Zero;
        return;
      }
      base.WndProc(ref m);
    }
    internal void HideBorder() {
      if(IsHandleCreated) NativeMethods.ShowWindow(Handle,0);
    }
    string lastPaint="",lastClip="";
    internal void Present(Rectangle rect,Rectangle local,Size outer,int band,int radius,RenderStyle style,Rectangle[] blockers)
    {
      string clip=rect+":"+SystemInformation.VirtualScreen+":"+string.Join(";",Array.ConvertAll(Array.FindAll(blockers,b=>b.IntersectsWith(rect)),b=>new Rectangle(b.X-rect.X,b.Y-rect.Y,b.Width,b.Height).ToString()));
      using(Region visible=WindowTracker.VisibleRegion(rect,blockers))using(var matrix=new System.Drawing.Drawing2D.Matrix()) {
        if(visible.GetRegionScans(matrix).Length==0){
          HideBorder();
          return;
        }
        if(lastClip!=clip){
          Region old=Region;
          Region=visible.Clone();
          if(old!=null)old.Dispose();
          lastClip=clip;
        }
      }
      string paint=rect.Size+":"+local.Location+":"+outer+":"+band+":"+radius+":"+style.Key;
      bool redraw=lastPaint!=paint;
      if(redraw){
        using(Bitmap bitmap=KnitRenderer.RenderStrip(local,outer,band,radius,style))Upload(bitmap,rect.Location);
        lastPaint=paint;
      }
      if(redraw||rect!=lastBounds||!NativeMethods.IsWindowVisible(Handle))if(!NativeMethods.SetWindowPos(Handle,NativeMethods.Topmost,rect.X,rect.Y,rect.Width,rect.Height,NativeMethods.NoActivate|NativeMethods.Show))throw new Win32Exception(Marshal.GetLastWin32Error());
      lastBounds=rect;
    }
    void Upload(Bitmap bitmap,Point location)
    {
      IntPtr screen=NativeMethods.GetDC(IntPtr.Zero), memory=IntPtr.Zero, hbitmap=IntPtr.Zero, old=IntPtr.Zero;
      try {
        if(screen==IntPtr.Zero) throw new Win32Exception("无法获取屏幕绘图设备。");
        memory=NativeMethods.CreateCompatibleDC(screen);
        if(memory==IntPtr.Zero) throw new Win32Exception("无法创建绘图设备。");
        hbitmap=bitmap.GetHbitmap(Color.FromArgb(0));
        old=NativeMethods.SelectObject(memory,hbitmap);
        var point=new NativeMethods.POINT(location.X,location.Y);
        var source=new NativeMethods.POINT(0,0);
        var size=new NativeMethods.SIZE(bitmap.Width,bitmap.Height);
        var blend=new NativeMethods.BLEND {
          Alpha=255,Format=1
        };
        if(!NativeMethods.UpdateLayeredWindow(Handle,screen,ref point,ref size,memory,ref source,0,ref blend,2))
        throw new Win32Exception(Marshal.GetLastWin32Error());
      }
      finally {
        if(old!=IntPtr.Zero && memory!=IntPtr.Zero) NativeMethods.SelectObject(memory,old);
        if(hbitmap!=IntPtr.Zero) NativeMethods.DeleteObject(hbitmap);
        if(memory!=IntPtr.Zero) NativeMethods.DeleteDC(memory);
        if(screen!=IntPtr.Zero) NativeMethods.ReleaseDC(IntPtr.Zero,screen);
      }
    }
  }
}
