using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowSweaters;
internal static class LifecycleTests {
  static int pass;
  static void Check(bool c,string s){
    if(!c)throw new Exception(s);
    pass++;
    Console.WriteLine("PASS "+s);
  }
  static T Field<T>(object o,string n){
    return (T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
  }
  static ContextMenuStrip Menu(SweaterContext c){
    typeof(SweaterContext).GetMethod("BuildMenu",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
    return Field<ContextMenuStrip>(c,"menu");
  }
  [DllImport("user32.dll")]static extern uint GetGuiResources(IntPtr p,uint kind);
  [STAThread]static int Main(){
    string folder=Path.Combine(Path.GetTempPath(),"SweaterMenu-"+Guid.NewGuid());
    try{
      Application.EnableVisualStyles();
      using(var c=new SweaterContext(folder)){
        var m=Menu(c);
        Check(m.Items.Cast<ToolStripItem>().Any(i=>i.Text=="软件开关")&&m.Items.Cast<ToolStripItem>().Any(i=>i.Text=="针织大小"),"complete menus exposed");
        var widths=(ToolStripMenuItem)m.Items.Cast<ToolStripItem>().First(i=>i.Text=="边框粗细");
        widths.DropDownItems.Cast<ToolStripItem>().First(i=>i.Text.StartsWith("8 ·")).PerformClick();
        Check(c.Settings.Width==8&&new SettingsStore(folder).Load().Width==8,"width menu updates live settings and disk");
        foreach(int thin in new[]{2,4}){
          m=Menu(c);
          widths=(ToolStripMenuItem)m.Items.Cast<ToolStripItem>().First(i=>i.Text=="边框粗细");
          var choice=widths.DropDownItems.Cast<ToolStripItem>().FirstOrDefault(i=>i.Text==thin+(thin==2?" · 极细":" · 纤细"));
          Check(choice!=null,"thin width "+thin+" is available in tray menu");
          choice.PerformClick();
          Check(c.Settings.Width==thin&&new SettingsStore(folder).Load().Width==thin,"thin width "+thin+" applies and persists");
          widths=(ToolStripMenuItem)Menu(c).Items.Cast<ToolStripItem>().First(i=>i.Text=="边框粗细");
          Check(widths.DropDownItems.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Single(i=>i.Checked).Text.StartsWith(thin+" ·"),"thin width "+thin+" remains selected when menu reopens");
        }
        c.ChoosePattern("twinkle");
        Check(c.Settings.Pattern=="twinkle"&&c.StyleFor(new AppInfo{
          Name="Code",Key="code",Executable=""
        }).Chart.Name=="twinkle","global pattern selection reaches renderer");
        c.Settings.Rows=3;
        c.ChoosePattern("zigzag");
        Check(c.Settings.Rows>=6,"selecting colorwork increases row count");
        m=Menu(c);
        m.Items[0].PerformClick();
        Check(!c.Settings.Enabled&&!new SettingsStore(folder).Load().Enabled,"pause is remembered");
        var groups=Field<System.Collections.Generic.Dictionary<IntPtr,BorderWindow[]>>(c,"groups");
        Check(groups.Values.SelectMany(x=>x).All(b=>!b.IsHandleCreated||!NativeMethods.IsWindowVisible(b.Handle)),"pause hides every app border");
        Menu(c).Items[0].PerformClick();
        Check(c.Settings.Enabled,"resume menu works");
        m=Menu(c);
        var apps=(ToolStripMenuItem)m.Items.Cast<ToolStripItem>().First(i=>i.Text=="软件开关");
        apps.DropDownItems[apps.DropDownItems.Count-1].PerformClick();
        Check(!c.Settings.IsAppEnabled("future-app")&&groups.Count==0,"all off removes windows and disables future apps");
        m=Menu(c);
        apps=(ToolStripMenuItem)m.Items.Cast<ToolStripItem>().First(i=>i.Text=="软件开关");
        apps.DropDownItems[apps.DropDownItems.Count-2].PerformClick();
        Check(c.Settings.IsAppEnabled("future-app"),"all on restores default for new apps");
        uint before=GetGuiResources(System.Diagnostics.Process.GetCurrentProcess().Handle,0);
        for(int i=0;i<40;i++)Menu(c);
        uint after=GetGuiResources(System.Diagnostics.Process.GetCurrentProcess().Handle,0);
        Check(after<=before+5,"40 menu rebuilds keep GDI stable ("+before+" -> "+after+")");
        var saved=groups.Values.SelectMany(x=>x).ToArray();
        m=Menu(c);
        m.Items[m.Items.Count-1].PerformClick();
        Check(saved.All(b=>b.IsDisposed)&&!Field<NotifyIcon>(c,"tray").Visible,"exit removes all border windows and tray");
      }
      using(var restarted=new SweaterContext(folder))Check(restarted.Settings.Width==4,"thin width survives a fresh application context");
      Console.WriteLine(pass+" passed, 0 failed");
      return 0;
    }
    catch(Exception e){
      Console.Error.WriteLine(e);
      return 1;
    }
    finally{
      if(Directory.Exists(folder))Directory.Delete(folder,true);
    }
  }
}
