using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowSweaters;
internal static class MultiWindowScenario{
  [DllImport("gdi32.dll")]static extern IntPtr CreateRectRgn(int l,int t,int r,int b);
  [DllImport("user32.dll")]static extern int GetWindowRgn(IntPtr w,IntPtr r);
  [DllImport("gdi32.dll")]static extern bool PtInRegion(IntPtr r,int x,int y);
  static uint app;
  static Form first,second;
  static Timer timer;
  static int step,code;
  static List<string> results=new List<string>();
  [STAThread]static int Main(string[] args){
    app=uint.Parse(args[0]);
    Application.EnableVisualStyles();
    first=new Form{
      Text="多窗口针织边框验收",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(300,250,720,470),KeyPreview=true
    };
    first.Controls.Add(new Label{
      Text="正在测试多窗口、重叠遮挡和最大化。\n按 Esc 可以停止。",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter
    });
    second=new Form{
      Text="第二个针织验收窗口",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(650,350,560,360)
    };
    first.KeyDown+=delegate(object sender,KeyEventArgs e){
      if(e.KeyCode==Keys.Escape){
        code=2;
        results.Add("STOPPED");
        first.Close();
      }
    };
    timer=new Timer{
      Interval=600
    };
    timer.Tick+=Tick;
    first.Shown+=delegate{
      timer.Start();
    };
    try{
      Application.Run(first);
    }
    finally{
      timer.Dispose();
      second.Dispose();
      File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"多窗口验收.txt"),results.ToArray());
    }
    return code;
  }
  static void Tick(object sender,EventArgs e){
    try{
      switch(step++){
        case 0:Verify(first,"普通窗口");
        first.Location=new Point(430,330);
        break;
        case 1:Verify(first,"移动窗口");
        first.Size=new Size(870,550);
        break;
        case 2:Verify(first,"放大窗口");
        first.Size=new Size(520,340);
        break;
        case 3:Verify(first,"缩小窗口");
        first.WindowState=FormWindowState.Maximized;
        break;
        case 4:Verify(first,"最大化");
        first.WindowState=FormWindowState.Normal;
        first.Bounds=new Rectangle(300,250,720,470);
        break;
        case 5:Verify(first,"还原窗口");
        second.Show();
        break;
        case 6:Verify(second,"前方第二窗口");
        CheckOcclusion();
        first.Activate();
        break;
        case 7:Verify(first,"切回第一窗口");
        second.Hide();
        first.FormBorderStyle=FormBorderStyle.None;
        first.Bounds=Screen.FromControl(first).Bounds;
        break;
        case 8:CheckFullscreen();
        first.FormBorderStyle=FormBorderStyle.Sizable;
        first.Bounds=new Rectangle(350,280,720,470);
        break;
        case 9:Verify(first,"退出全屏恢复");
        first.Close();
        break;
      }
    }
    catch(Exception ex){
      code=1;
      results.Add("FAIL "+ex.Message);
      first.Close();
    }
  }
  static List<KeyValuePair<IntPtr,Rectangle>> Borders(){
    var list=new List<KeyValuePair<IntPtr,Rectangle>>();
    NativeMethods.EnumWindows(delegate(IntPtr w,IntPtr p){
      uint pid;NativeMethods.GetWindowThreadProcessId(w,out pid);if(pid==app&&(NativeMethods.GetWindowLong(w,-20)&0x80000)!=0&&NativeMethods.IsWindowVisible(w)){
        NativeMethods.RECT r;NativeMethods.GetWindowRect(w,out r);list.Add(new KeyValuePair<IntPtr,Rectangle>(w,r.ToRectangle()));
      }
      return true;
    },IntPtr.Zero);
    return list;
  }
  static Rectangle Frame(Form f){
    NativeMethods.RECT r;
    if(NativeMethods.GetFrame(f.Handle,9,out r,16)!=0)throw new Exception("no DWM frame");
    return r.ToRectangle();
  }
  static Rectangle[] Expected(Form f){
    var r=Frame(f);
    int t=12;
    bool zoom=f.WindowState==FormWindowState.Maximized;
    return new[]{
      new Rectangle(r.X-(zoom?0:t),r.Y-(zoom?0:t),r.Width+(zoom?0:2*t),t),new Rectangle(r.X-(zoom?0:t),r.Bottom-(zoom?t:0),r.Width+(zoom?0:2*t),t),new Rectangle(r.X-(zoom?0:t),r.Y+(zoom?t:0),t,r.Height-(zoom?2*t:0)),new Rectangle(r.Right-(zoom?t:0),r.Y+(zoom?t:0),t,r.Height-(zoom?2*t:0))
    };
  }
  static void Verify(Form f,string name){
    if(NativeMethods.GetForegroundWindow()!=f.Handle)throw new Exception(name+"：验收窗口不在前台");
    var borders=Borders();
    foreach(var rect in Expected(f))if(!borders.Exists(b=>b.Value==rect))throw new Exception(name+" 缺少边框 "+rect);
    results.Add("PASS "+name+"；"+Frame(f));
  }
  static void CheckOcclusion(){
    var borders=Borders();
    var frame=Frame(first);
    var right=new Rectangle(frame.Right,frame.Top,12,frame.Height);
    var strip=borders.Find(b=>b.Value==right);
    if(strip.Key==IntPtr.Zero)throw new Exception("后方窗口边框整体丢失");
    IntPtr region=CreateRectRgn(0,0,0,0);
    try{
      if(GetWindowRgn(strip.Key,region)==0)throw new Exception("缺少原生裁剪区");
      if(PtInRegion(region,5,150))throw new Exception("后方边框浮到前方窗口上");
      if(!PtInRegion(region,5,25))throw new Exception("未遮挡部分被错误隐藏");
    }
    finally{
      NativeMethods.DeleteObject(region);
    }
    results.Add("PASS 同时保留两个窗口边框，遮挡部分隐藏、露出部分可见");
  }
  static void CheckFullscreen(){
    if(NativeMethods.GetForegroundWindow()!=first.Handle)throw new Exception("全屏验收窗口失去焦点");
    var borders=Borders();
    if(borders.Count!=0)throw new Exception("全屏前方仍有边框："+string.Join(";",borders.ConvertAll(x=>x.Value.ToString())));
    results.Add("PASS 全屏隐藏所有被覆盖边框");
  }
}
