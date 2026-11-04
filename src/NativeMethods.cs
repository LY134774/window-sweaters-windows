using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
namespace WindowSweaters
{
  internal static class NativeMethods
  {
    internal delegate bool EnumProc(IntPtr window,IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback,IntPtr parameter);
    [DllImport("user32.dll")] internal static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool QueryFullProcessImageName(IntPtr process,int flags,StringBuilder text,ref int length);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    internal static readonly IntPtr Topmost=new IntPtr(-1);
    internal const uint NoActivate=0x10, Show=0x40;
    [StructLayout(LayoutKind.Sequential)] internal struct RECT {
      internal int Left,Top,Right,Bottom;
      internal Rectangle ToRectangle(){
        return Rectangle.FromLTRB(Left,Top,Right,Bottom);
      }
    }
    [StructLayout(LayoutKind.Sequential)] internal struct POINT {
      internal int X,Y;
      internal POINT(int x,int y){
        X=x;
        Y=y;
      }
    }
    [StructLayout(LayoutKind.Sequential)] internal struct SIZE {
      internal int Width,Height;
      internal SIZE(int w,int h){
        Width=w;
        Height=h;
      }
    }
    [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct BLEND {
      internal byte Operation,Flags,Alpha,Format;
    }
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr window,StringBuilder text,int length);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] internal static extern int GetWindowLong(IntPtr window,int index);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window,out RECT rect);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] internal static extern int GetFrame(IntPtr window,int attribute,out RECT rect,int size);
    [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] internal static extern int GetCloaked(IntPtr window,int attribute,out int value,int size);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr window,IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc,IntPtr value);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr window,IntPtr dc,ref POINT point,ref SIZE size,IntPtr source,ref POINT origin,int key,ref BLEND blend,uint flags);
    [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);
  }
}
