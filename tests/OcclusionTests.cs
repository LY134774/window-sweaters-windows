using System;
using System.Drawing;
using System.Collections.Generic;
using WindowSweaters;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
internal static class OcclusionTests{
  static int passed=4;
  static void Check(bool condition,string name){
    if(!condition)throw new Exception(name);
    passed++;
    Console.WriteLine("PASS "+name);
  }
  static bool TopBorderVisible(WindowTracker tracker,IntPtr handle){
    WindowSnapshot target=tracker.Scan().FirstOrDefault(w=>w.Handle==handle);
    if(target==null)throw new Exception("test window was not tracked");
    var strip=new Rectangle(target.Frame.Left,target.Frame.Top-4,target.Frame.Width,4);
    using(var region=WindowTracker.VisibleRegion(strip,target.Above))return region.IsVisible(strip.Width/2,2);
  }
  static void CheckSogouOverlay(Rectangle screen){
    using(var target=new Form{
      Text="Input overlay regression",StartPosition=FormStartPosition.Manual,
      Bounds=new Rectangle(screen.Left+120,screen.Top+120,520,320),ShowInTaskbar=false
    })
    using(var tracker=new WindowTracker()){
      // Let the scanner observe the real test window without changing production behavior.
      typeof(WindowTracker).GetField("own",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(tracker,(uint)0);
      target.Show();
      Application.DoEvents();
      var working=Screen.FromControl(target).WorkingArea;
      var helperBounds=new Rectangle(working.Left,working.Top,working.Width,working.Height-1);
      using(var overlay=new TestOverlay("sg_ichat_main_wnd",helperBounds,0x80088,0)){
        for(int cycle=0;cycle<3;cycle++){
          overlay.Show(true);
          Check(TopBorderVisible(tracker,target.Handle),"Sogou helper does not hide border, cycle "+cycle);
          overlay.Show(false);
          Check(TopBorderVisible(tracker,target.Handle),"border stays visible after Sogou helper hides, cycle "+cycle);
        }
      }
      var cover=new Rectangle(target.Left-10,target.Top-20,target.Width+20,60);
      using(var ordinary=new TestOverlay("SweaterNormalOverlayRegression",cover,0x80088,255)){
        ordinary.Show(true);
        Check(!TopBorderVisible(tracker,target.Handle),"other layered tool windows still occlude borders");
      }
      using(var ordinary=new TestOverlay("sg_ichat_main_wnd",cover,0x88,255)){
        ordinary.Show(true);
        Check(!TopBorderVisible(tracker,target.Handle),"non-layered windows with same class still occlude borders");
      }
    }
  }
  sealed class TestOverlay:IDisposable{
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    struct WindowClass{
      internal uint Style;
      internal IntPtr Procedure;
      internal int ClassExtra,WindowExtra;
      internal IntPtr Instance,Icon,Cursor,Background;
      internal string MenuName,ClassName;
    }
    delegate IntPtr WindowProcedure(IntPtr w,uint message,IntPtr wp,IntPtr lp);
    static readonly WindowProcedure procedure=DefWindowProc;
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern ushort RegisterClass(ref WindowClass wc);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool UnregisterClass(string name,IntPtr instance);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWindowEx(int ex,string cls,string text,int style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr DefWindowProc(IntPtr w,uint message,IntPtr wp,IntPtr lp);
    [DllImport("user32.dll",SetLastError=true)]static extern bool SetLayeredWindowAttributes(IntPtr w,uint color,byte alpha,uint flags);
    [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr w);
    readonly string name;
    readonly IntPtr instance;
    readonly Rectangle bounds;
    IntPtr handle;
    internal TestOverlay(string name,Rectangle bounds,int exStyle,byte alpha){
      this.name=name;this.bounds=bounds;instance=GetModuleHandle(null);
      var wc=new WindowClass{Procedure=Marshal.GetFunctionPointerForDelegate(procedure),Instance=instance,ClassName=name};
      if(RegisterClass(ref wc)==0)throw new Win32Exception(Marshal.GetLastWin32Error());
      try{
        handle=CreateWindowEx(exStyle,name,"",unchecked((int)0x86000000),bounds.X,bounds.Y,bounds.Width,bounds.Height,IntPtr.Zero,IntPtr.Zero,instance,IntPtr.Zero);
        if(handle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
        if((exStyle&0x80000)!=0&&!SetLayeredWindowAttributes(handle,0,alpha,2))throw new Win32Exception(Marshal.GetLastWin32Error());
      }
      catch{Dispose();throw;}
    }
    internal void Show(bool visible){
      if(visible){
        if(!NativeMethods.SetWindowPos(handle,NativeMethods.Topmost,bounds.X,bounds.Y,bounds.Width,bounds.Height,NativeMethods.NoActivate|NativeMethods.Show))throw new Win32Exception(Marshal.GetLastWin32Error());
      }else NativeMethods.ShowWindow(handle,0);
    }
    public void Dispose(){
      if(handle!=IntPtr.Zero){DestroyWindow(handle);handle=IntPtr.Zero;}
      UnregisterClass(name,instance);
    }
  }
  [STAThread] static int Main(){
    try{
      using(var r=WindowTracker.VisibleRegion(new Rectangle(10,10,100,10),new[]{
        new Rectangle(40,0,20,50)
      })){
        if(!r.IsVisible(5,5)||r.IsVisible(35,5)||!r.IsVisible(80,5))throw new Exception("foreground rectangle not subtracted in local coordinates");
      }
      using(var r=WindowTracker.VisibleRegion(new Rectangle(-100,0,80,10),new[]{
        new Rectangle(-200,-20,300,100)
      })){
        using(var m=new System.Drawing.Drawing2D.Matrix())if(r.GetRegionScans(m).Length!=0)throw new Exception("fully covered border remains visible");
      }
      var screen=System.Windows.Forms.SystemInformation.VirtualScreen;
      using(var outside=WindowTracker.VisibleRegion(new Rectangle(screen.Right+10,screen.Top,50,10),new Rectangle[0]))using(var m=new System.Drawing.Drawing2D.Matrix())if(outside.GetRegionScans(m).Length!=0)throw new Exception("offscreen strips remain native-visible");
      using(var border=new BorderWindow()){
        var style=new RenderStyle(Color.Coral,null,6,false);
        var rect=new Rectangle(screen.Left-10,screen.Top+100,40,12);
        border.Present(rect,new Rectangle(0,0,40,12),new Size(200,120),12,0,style,new Rectangle[0]);
        if(border.Region.IsVisible(5,5))throw new Exception("offscreen area not clipped");
        rect.X=screen.Left+30;
        border.Present(rect,new Rectangle(0,0,40,12),new Size(200,120),12,0,style,new Rectangle[0]);
        if(!border.Region.IsVisible(5,5))throw new Exception("moved border retains stale screen clipping");
      }
      CheckSogouOverlay(screen);
      Console.WriteLine(passed+" passed, 0 failed");
      return 0;
    }
    catch(Exception e){
      Console.Error.WriteLine(e);
      return 1;
    }
  }
}
