using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace WindowSweaters {
  internal sealed class AppInfo {
    internal string Key,Name,DisplayName,Executable;
    public override string ToString(){
      return DisplayName;
    }
  }
  internal sealed class WindowSnapshot {
    internal IntPtr Handle;
    internal Rectangle Frame;
    internal bool Maximized;
    internal uint Dpi;
    internal AppInfo App;
    internal Rectangle[] Above;
  }
  internal sealed class WindowTracker:IDisposable {
    readonly uint own=(uint)Process.GetCurrentProcess().Id;
    readonly Dictionary<uint,AppInfo> apps=new Dictionary<uint,AppInfo>();
    readonly DpiProbe probe=new DpiProbe();
    internal List<WindowSnapshot> Scan(){
      var result=new List<WindowSnapshot>();
      var blockers=new List<Rectangle>();
      var seen=new HashSet<uint>();
      var dpiCache=new Dictionary<IntPtr,uint>();
      NativeMethods.EnumWindows(delegate(IntPtr w,IntPtr p){
        try{
          if(!NativeMethods.IsWindowVisible(w)||NativeMethods.IsIconic(w))return true;int ex=NativeMethods.GetWindowLong(w,-20);if((ex&0x80020)==0x80020)return true;int cloaked;if(NativeMethods.GetCloaked(w,14,out cloaked,4)==0&&cloaked!=0)return true;NativeMethods.RECT raw;if(NativeMethods.GetFrame(w,9,out raw,16)!=0&&!NativeMethods.GetWindowRect(w,out raw))return true;Rectangle frame=raw.ToRectangle();if(frame.Width<1||frame.Height<1||!frame.IntersectsWith(SystemInformation.VirtualScreen))return true;var name=new StringBuilder(256);NativeMethods.GetClassName(w,name,256);string cls=name.ToString();if(cls=="Progman"||cls=="WorkerW")return true;uint pid;NativeMethods.GetWindowThreadProcessId(w,out pid);seen.Add(pid);bool zoom=NativeMethods.IsZoomed(w);Rectangle screen=Screen.FromHandle(w).Bounds;bool full=!zoom&&frame.Contains(screen);int style=NativeMethods.GetWindowLong(w,-16);bool framed=(style&0x00C40000)!=0;bool normal=cls!="Shell_TrayWnd"&&cls!="Shell_SecondaryTrayWnd"&&cls!="#32768";
          if(BorderGeometry.ShouldDecorate(true,false,false,pid==own,normal,framed,full)){
            AppInfo app=Resolve(pid);IntPtr monitor=NativeMethods.MonitorFromWindow(w,2);uint dpi;if(!dpiCache.TryGetValue(monitor,out dpi)){
              dpi=probe.At(screen);dpiCache.Add(monitor,dpi);
            }
            result.Add(new WindowSnapshot{
              Handle=w,Frame=frame,Maximized=zoom,Dpi=dpi,App=app,Above=blockers.ToArray()
            });
          }
          blockers.Add(frame);
        }
        catch(System.ComponentModel.Win32Exception){
        }
        catch(InvalidOperationException){
        }
        catch(ArgumentException){
        }
        return true;
      },IntPtr.Zero);
      foreach(uint id in apps.Keys.Where(x=>!seen.Contains(x)).ToArray())apps.Remove(id);
      return result;
    }
    AppInfo Resolve(uint pid){
      AppInfo info;
      if(apps.TryGetValue(pid,out info))return info;
      string exe="",name="";
      IntPtr process=NativeMethods.OpenProcess(0x1000,false,pid);
      try{
        if(process!=IntPtr.Zero){
          var b=new StringBuilder(32768);
          int length=b.Capacity;
          if(NativeMethods.QueryFullProcessImageName(process,0,b,ref length))exe=b.ToString();
        }
      }
      finally{
        if(process!=IntPtr.Zero)NativeMethods.CloseHandle(process);
      }
      if(exe.Length>0)name=Path.GetFileNameWithoutExtension(exe);
      if(name.Length==0)using(var p=Process.GetProcessById((int)pid))name=p.ProcessName;
      string display=name;
      info=new AppInfo{
        Key=name.ToLowerInvariant(),Name=name,DisplayName=display,Executable=exe
      };
      apps.Add(pid,info);
      return info;
    }
    internal static Region VisibleRegion(Rectangle strip,IEnumerable<Rectangle> blockers){
      var r=new Region(new Rectangle(Point.Empty,strip.Size));
      Rectangle screen=SystemInformation.VirtualScreen;
      screen.Offset(-strip.X,-strip.Y);
      r.Intersect(screen);
      foreach(Rectangle b in blockers)if(b.IntersectsWith(strip)){
        var local=b;
        local.Offset(-strip.X,-strip.Y);
        r.Exclude(local);
      }
      return r;
    }
    public void Dispose(){
      probe.Dispose();
    }
    sealed class DpiProbe:NativeWindow,IDisposable{
      internal DpiProbe(){
        CreateHandle(new CreateParams{
          Caption="针织缩放检测",Style=unchecked((int)0x80000000),ExStyle=0x8000080,X=0,Y=0,Width=1,Height=1
        });
      }
      internal uint At(Rectangle monitor){
        NativeMethods.SetWindowPos(Handle,IntPtr.Zero,monitor.Left+monitor.Width/2,monitor.Top+monitor.Height/2,1,1,0x14);
        uint dpi=NativeMethods.GetDpiForWindow(Handle);
        return dpi==0?96:dpi;
      }
      public void Dispose(){
        DestroyHandle();
      }
    }
  }
  internal sealed class IconPalette {
    readonly Dictionary<string,Color> colors=new Dictionary<string,Color>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> pending=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly object gate=new object();
    internal Color Get(AppInfo app){
      lock(gate){
        Color c;
        if(colors.TryGetValue(app.Key,out c))return c;
        if(!pending.Contains(app.Key)){
          pending.Add(app.Key);
          ThreadPool.QueueUserWorkItem(delegate{
            Color color=Fallback(app.Name);try{
              if(File.Exists(app.Executable))using(Icon icon=Icon.ExtractAssociatedIcon(app.Executable))if(icon!=null)using(Bitmap b=icon.ToBitmap())color=FromBitmap(b);
            }
            catch(Exception){
            }
            lock(gate){
              colors[app.Key]=color;pending.Remove(app.Key);
            }
          });
        }
        return Fallback(app.Name);
      }
    }
    static Color Fallback(string text){
      uint h=2166136261;
      foreach(char c in text.ToLowerInvariant())h=(h^c)*16777619;
      Color[] palette={
        Color.FromArgb(132,159,144),Color.FromArgb(125,163,190),Color.FromArgb(193,142,127),Color.FromArgb(160,144,184),Color.FromArgb(185,168,126)
      };
      return palette[h%palette.Length];
    }
    internal static Color FromBitmap(Bitmap b){
      var bins=new Dictionary<int,double[]>();
      double grey=0,alpha=0;
      for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
        Color c=b.GetPixel(x,y);
        if(c.A<128)continue;
        grey+=(c.R+c.G+c.B)/3.0;
        alpha++;
        if(c.GetSaturation()<.15||c.GetBrightness()<.12||c.GetBrightness()>.92)continue;
        int k=(c.R/32<<6)|(c.G/32<<3)|(c.B/32);
        double[] v;
        if(!bins.TryGetValue(k,out v))bins[k]=v=new double[4];
        double weight=.25+c.GetSaturation();
        v[0]+=weight;
        v[1]+=c.R*weight;
        v[2]+=c.G*weight;
        v[3]+=c.B*weight;
      }
      if(bins.Count==0){
        int n=alpha==0?160:(int)Math.Max(90,Math.Min(210,grey/alpha));
        return Color.FromArgb(n,n,n);
      }
      var top=bins.Values.OrderByDescending(v=>v[0]).First();
      return Color.FromArgb((int)(top[1]/top[0]*.85+225*.15),(int)(top[2]/top[0]*.85+225*.15),(int)(top[3]/top[0]*.85+225*.15));
    }
  }
}
