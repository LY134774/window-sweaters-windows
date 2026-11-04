using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowSweaters;

internal static class Scenario
{
 delegate bool EnumProc(IntPtr window,IntPtr parameter);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr parameter);
 static List<string> results=new List<string>();
 static int step, exitCode;
 static uint app;
 static Form first,second;
 static Timer timer;
 [STAThread] static int Main(string[] args)
 {
  app=uint.Parse(args[0]); Application.EnableVisualStyles();
  first=new Form { Text="针织边框自动验收", StartPosition=FormStartPosition.Manual, Bounds=new Rectangle(300,250,720,470), KeyPreview=true };
  first.Controls.Add(new Label { Text="正在验证窗口跟随，请稍候。\n按 Esc 可停止验收。", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleCenter });
  first.KeyDown+=delegate(object sender,KeyEventArgs e){ if(e.KeyCode==Keys.Escape) { exitCode=2; results.Add("STOPPED by Escape"); first.Close(); } };
  second=new Form { Text="针织边框第二验收窗口", StartPosition=FormStartPosition.Manual, Bounds=new Rectangle(650,350,560,360) };
  timer=new Timer { Interval=400 };
  timer.Tick+=Tick;
  first.Shown+=delegate { timer.Start(); };
  try { Application.Run(first); }
  finally {
   timer.Stop(); timer.Dispose(); second.Dispose();
   File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"窗口跟随验收.txt"),results.ToArray());
  }
  return exitCode;
 }
 static void Tick(object sender,EventArgs e)
 {
  try {
   switch(step++) {
    case 0: Verify(first,false,"普通窗口"); first.Location=new Point(430,330); break;
    case 1: Verify(first,false,"移动后跟随"); first.Size=new Size(870,550); break;
    case 2: Verify(first,false,"放大后跟随"); first.Size=new Size(520,340); break;
    case 3: Verify(first,false,"缩小后跟随"); first.WindowState=FormWindowState.Maximized; break;
    case 4: Verify(first,false,"最大化时屏内边框"); first.WindowState=FormWindowState.Normal; break;
    case 5: Verify(first,false,"还原后跟随"); second.Show(); break;
    case 6: Verify(second,false,"切换第二窗口只保留一套边框"); second.Hide(); first.Activate(); break;
    case 7: Verify(first,false,"切回第一窗口"); first.FormBorderStyle=FormBorderStyle.None; first.Bounds=Screen.FromControl(first).Bounds; break;
    case 8: Verify(first,true,"全屏时隐藏"); first.FormBorderStyle=FormBorderStyle.Sizable; first.Bounds=new Rectangle(350,280,720,470); break;
    case 9: Verify(first,false,"退出全屏后恢复"); first.Close(); break;
   }
  } catch(Exception ex) { exitCode=1; results.Add("FAIL "+ex.Message); first.Close(); }
 }
 static void Verify(Form target,bool hidden,string name)
 {
  if(NativeMethods.GetForegroundWindow()!=target.Handle) throw new Exception(name+"：验收窗口失去焦点，无法判断");
  NativeMethods.RECT raw;
  if(NativeMethods.GetFrame(target.Handle,9,out raw,16)!=0) throw new Exception("无法读取窗口边界");
  Rectangle frame=raw.ToRectangle(); uint dpi=NativeMethods.GetDpiForWindow(target.Handle);
  var expected=BorderGeometry.Calculate(frame,BorderGeometry.Thickness(dpi),NativeMethods.IsZoomed(target.Handle));
  var actual=new List<Rectangle>(); bool styles=true;
  EnumWindows(delegate(IntPtr w,IntPtr unused) {
   uint pid; NativeMethods.GetWindowThreadProcessId(w,out pid);
   int flags=NativeMethods.GetWindowLong(w,-20);
   if(pid==app && (flags&0x80000)!=0 && NativeMethods.IsWindowVisible(w)) {
    NativeMethods.RECT r; NativeMethods.GetWindowRect(w,out r); actual.Add(r.ToRectangle());
    if((flags&0x80800A8)!=0x80800A8) styles=false;
   }
   return true;
  },IntPtr.Zero);
  if(actual.Count!=(hidden?0:4)) throw new Exception(name+"：可见边框数 "+actual.Count);
  if(!styles) throw new Exception(name+"：鼠标穿透/不抢焦点标记缺失");
  if(!hidden) foreach(var r in expected) if(!actual.Contains(r)) throw new Exception(name+"：边框位置不一致 "+r);
  results.Add("PASS "+name+"；窗口="+frame+"；边框="+actual.Count+"；DPI="+dpi);
 }
}
