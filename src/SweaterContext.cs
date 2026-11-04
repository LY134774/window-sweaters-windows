using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace WindowSweaters {
  internal sealed class SweaterContext:ApplicationContext {
    readonly SettingsStore store;
    internal readonly UserSettings Settings;
    internal readonly PatternCatalog Catalog;
    internal readonly AppProfiles Profiles;
    readonly WindowTracker tracker=new WindowTracker();
    readonly IconPalette palette=new IconPalette();
    readonly Dictionary<IntPtr,BorderWindow[]> groups=new Dictionary<IntPtr,BorderWindow[]>();
    readonly Timer timer=new Timer{
      Interval=50
    };
    readonly NotifyIcon tray=new NotifyIcon();
    readonly ContextMenuStrip menu=new ContextMenuStrip();
    readonly Icon icon;
    List<WindowSnapshot> snapshots=new List<WindowSnapshot>();
    bool disposed,refreshing;
    AppSettingsForm appEditor;
    internal SweaterContext():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowSweaters")){
    }
    internal SweaterContext(string folder){
      try{
        store=new SettingsStore(folder);
        Settings=store.Load();
        Catalog=new PatternCatalog(Path.Combine(folder,"charts"));
        Profiles=new AppProfiles(folder);
        if(Settings.Pattern!="by-app"&&Settings.Pattern!="plain"&&Settings.Pattern!="rib"&&Catalog.Find(Settings.Pattern)==null)Settings.Pattern="by-app";
        icon=CreateYarnIcon();
        tray.Icon=icon;
        tray.ContextMenuStrip=menu;
        tray.Visible=true;
        tray.DoubleClick+=delegate{
          SetEnabled(!Settings.Enabled);
        };
        menu.Opening+=delegate{
          BuildMenu();
        };
        timer.Tick+=delegate{
          RefreshSafely();
        };
        timer.Start();
        UpdateTray();
        RefreshSafely();
        if(store.Warning.Length>0)tray.ShowBalloonTip(6000,"设置已恢复",store.Warning,ToolTipIcon.Warning);
        else tray.ShowBalloonTip(4000,"针织边框已启动","右键右下角毛线图标，可调图案、粗细、针脚和各软件开关。",ToolTipIcon.Info);
      }
      catch{
        Dispose();
        throw;
      }
    }
    internal void SetEnabled(bool value){
      Settings.Enabled=value;
      Changed();
    }
    internal void Changed(){
      try{
        store.Save(Settings);
      }
      catch(Exception ex){
        MessageBox.Show("本次调节已生效，但保存失败，下次启动可能恢复旧设置。\n"+ex.Message,"针织边框",MessageBoxButtons.OK,MessageBoxIcon.Warning);
      }
      UpdateTray();
      RefreshSafely();
    }
    void UpdateTray(){
      tray.Text=!Settings.Enabled?"针织边框 · 已暂停":!Settings.AppsOn&&Settings.Exceptions.Count==0?"针织边框 · 所有软件已关闭":"针织边框 · 运行中";
    }
    void RefreshSafely(){
      if(disposed||refreshing)return;
      refreshing=true;
      try{
        if(!Settings.Enabled){
          foreach(var b in groups.Values)foreach(var f in b)f.HideBorder();
          return;
        }
        snapshots=tracker.Scan();
        var live=new HashSet<IntPtr>();
        foreach(var target in snapshots){
          if(!Settings.IsAppEnabled(target.App.Key))continue;
          int band=BorderGeometry.Thickness(target.Dpi,Settings.Width);
          int radius=target.Maximized?0:(Environment.OSVersion.Version.Build>=22000?Math.Max(4,(int)(8*target.Dpi/96)):0);
          var rects=BorderGeometry.Calculate(target.Frame,band,target.Maximized,radius);
          if(rects.Length!=4)continue;
          live.Add(target.Handle);
          BorderWindow[] borders;
          if(!groups.TryGetValue(target.Handle,out borders)){
            borders=new[]{
              new BorderWindow(),new BorderWindow(),new BorderWindow(),new BorderWindow()
            };
            groups.Add(target.Handle,borders);
          }
          var style=StyleFor(target.App);
          Rectangle outer=target.Frame;
          if(!target.Maximized)outer.Inflate(band,band);
          for(int i=0;i<4;i++){
            Rectangle local=rects[i];
            local.Offset(-outer.X,-outer.Y);
            borders[i].Present(rects[i],local,outer.Size,band,radius,style,target.Above);
          }
        }
        foreach(IntPtr h in groups.Keys.Where(x=>!live.Contains(x)).ToArray()){
          foreach(var b in groups[h])b.Dispose();
          groups.Remove(h);
        }
      }
      catch(Exception ex){
        Settings.Enabled=false;
        foreach(var g in groups.Values)foreach(var b in g)b.HideBorder();
        UpdateTray();
        tray.ShowBalloonTip(8000,"针织边框已暂停","窗口绘制出现问题："+ex.Message,ToolTipIcon.Warning);
      }
      finally{
        refreshing=false;
      }
    }
    internal RenderStyle StyleFor(AppInfo app){
      var profile=Profiles.Find(app.Name);
      bool byApp=Settings.Pattern=="by-app";
      Color color=profile!=null&&(byApp||Settings.Pattern!="zigzag"||profile.Personal)?profile.Color:palette.Get(app);
      string name=byApp?(profile==null?new[]{
        "zigzag","picnic","twinkle"
      }
      [(uint)StableHash(app.Key)%3]:profile.Pattern):Settings.Pattern;
      Chart chart=Catalog.Find(name);
      double rows=Catalog.EffectiveRows(name,Settings.Rows);
      return new RenderStyle(color,chart,rows,name=="rib");
    }
    static int StableHash(string s){
      unchecked{
        int h=17;
        foreach(char c in s)h=h*31+c;
        return h;
      }
    }
    internal List<AppInfo> RunningApps(){
      snapshots=tracker.Scan();
      return snapshots.Select(x=>x.App).GroupBy(x=>x.Key,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).OrderBy(x=>x.DisplayName,StringComparer.CurrentCultureIgnoreCase).ToList();
    }
    ToolStripMenuItem Item(string text,Action action,bool check){
      var i=new ToolStripMenuItem(text){
        Checked=check
      };
      i.Click+=delegate{
        action();
      };
      return i;
    }
    void BuildMenu(){
      foreach(ToolStripItem i in menu.Items.Cast<ToolStripItem>().ToArray())DisposeMenuItem(i);
      menu.Items.Clear();
      menu.Items.Add(Item("显示针织边框",()=>SetEnabled(!Settings.Enabled),Settings.Enabled));
      var apps=new ToolStripMenuItem("软件开关");
      foreach(var a in RunningApps()){
        AppInfo app=a;
        apps.DropDownItems.Add(Item(app.DisplayName,()=>{
          Settings.SetAppEnabled(app.Key,!Settings.IsAppEnabled(app.Key));Changed();
        },Settings.IsAppEnabled(app.Key)));
      }
      if(apps.DropDownItems.Count==0)apps.DropDownItems.Add(new ToolStripMenuItem("暂无可装饰的普通窗口"){
        Enabled=false
      });
      apps.DropDownItems.Add(new ToolStripSeparator());
      apps.DropDownItems.Add(Item("全部开启",()=>{
        Settings.SetAllApps(true);Changed();
      },Settings.AppsOn&&Settings.Exceptions.Count==0));
      apps.DropDownItems.Add(Item("全部关闭",()=>{
        Settings.SetAllApps(false);Changed();
      },!Settings.AppsOn&&Settings.Exceptions.Count==0));
      menu.Items.Add(apps);
      var patterns=new ToolStripMenuItem("图案");
      foreach(string n in new[]{
        "by-app","plain","rib"
      }
      .Concat(Catalog.Featured)){
        string name=n;
        var choice=Item(Catalog.Title(name),()=>ChoosePattern(name),Settings.Pattern==name);
        if(Catalog.Find(name)!=null)choice.Image=KnitRenderer.RenderStrip(new Rectangle(24,0,52,18),new Size(100,100),18,0,new RenderStyle(Color.FromArgb(132,159,144),Catalog.Find(name),Catalog.EffectiveRows(name,6),false));
        patterns.DropDownItems.Add(choice);
      }
      var custom=new ToolStripMenuItem("软件专属和自定义图案");
      foreach(var c in Catalog.All.Where(x=>!Catalog.Featured.Contains(x.Name))){
        string name=c.Name;
        custom.DropDownItems.Add(Item(Catalog.Title(name),()=>ChoosePattern(name),Settings.Pattern==name));
      }
      patterns.DropDownItems.Add(custom);
      menu.Items.Add(patterns);
      var widths=new ToolStripMenuItem("边框粗细");
      foreach(int n in new[]{
        2,4,6,8,12,14,18,28
      }){
        int width=n;
        widths.DropDownItems.Add(Item(width+" · "+(width==2?"极细":width==4?"纤细":width<=8?"细":width<=14?"标准":"宽"),()=>{
          Settings.Width=width;Changed();
        },Settings.Width==width));
      }
      menu.Items.Add(widths);
      var sizes=new ToolStripMenuItem("针织大小");
      int[] rows={
        3,6,10,14
      };
      string[] names={
        "粗针","中针","细针","极细"
      };
      double min=MinimumRows();
      for(int n=0;n<rows.Length;n++){
        int value=rows[n];
        var item=Item(names[n]+" · "+value+" 行",()=>{
          Settings.Rows=value;Changed();
        },Settings.Rows==value);
        item.Enabled=value>=min;
        sizes.DropDownItems.Add(item);
      }
      if(Settings.Rows>14)sizes.DropDownItems.Add(new ToolStripMenuItem("当前图案需要 "+Settings.Rows+" 行"){
        Enabled=false
      });
      menu.Items.Add(sizes);
      menu.Items.Add(new ToolStripSeparator());
      menu.Items.Add("软件配色和图案…",null,delegate{
        OpenEditor();
      });
      menu.Items.Add("导入自定义 PNG 图案…",null,delegate{
        ImportPattern();
      });
      menu.Items.Add("打开自定义图案文件夹",null,delegate{
        Process.Start(Catalog.Folder);
      });
      menu.Items.Add("重新加载图案和配色",null,delegate{
        Reload();
      });
      menu.Items.Add("查看图案预览…",null,delegate{
        ShowPreview();
      });
      menu.Items.Add(new ToolStripSeparator());
      menu.Items.Add("退出",null,delegate{
        ExitThread();
      });
    }
    static void DisposeMenuItem(ToolStripItem item){
      var drop=item as ToolStripDropDownItem;
      if(drop!=null)foreach(ToolStripItem child in drop.DropDownItems.Cast<ToolStripItem>().ToArray())DisposeMenuItem(child);
      if(item.Image!=null){
        item.Image.Dispose();
        item.Image=null;
      }
      item.Dispose();
    }
    double MinimumRows(){
      if(Settings.Pattern=="plain"||Settings.Pattern=="rib")return 3;
      var chart=Catalog.Find(Settings.Pattern);
      return chart==null?6:Math.Max(6,chart.Height);
    }
    internal void ChoosePattern(string name){
      Settings.Pattern=name;
      Settings.Rows=Math.Max(Settings.Rows,MinimumRows());
      Changed();
    }
    internal void Reload(){
      try{
        Catalog.Reload();
        Profiles.Reload();
        if(Settings.Pattern!="by-app"&&Settings.Pattern!="plain"&&Settings.Pattern!="rib"&&Catalog.Find(Settings.Pattern)==null){
          Settings.Pattern="by-app";
          MessageBox.Show("原来选择的图案已不存在，已切回按软件选择。","针织边框");
        }
        Settings.Rows=Math.Max(Settings.Rows,MinimumRows());
        KnitRenderer.ClearCache();
        Changed();
        if(Catalog.Warnings.Count>0)MessageBox.Show("以下图案未加载，其余图案可继续使用：\n"+string.Join("\n",Catalog.Warnings.Take(8)),"图案加载提示",MessageBoxButtons.OK,MessageBoxIcon.Warning);
      }
      catch(Exception ex){
        MessageBox.Show("重新加载失败："+ex.Message,"针织边框");
      }
    }
    void ImportPattern(){
      using(var dialog=new OpenFileDialog{
        Title="导入针织图案（1至256像素PNG）",Filter="PNG 图案|*.png",Multiselect=false
      }){
        if(dialog.ShowDialog()!=DialogResult.OK)return;
        try{
          string importName=Path.GetFileNameWithoutExtension(dialog.FileName);
          if(importName.Length>63||new[]{
            "by-app","plain","rib"
          }
          .Contains(importName,StringComparer.OrdinalIgnoreCase))throw new InvalidDataException("文件名应不超过63个字符，并避开 by-app、plain、rib 三个内置名称。");
          using(var image=Image.FromFile(dialog.FileName)){
            if(image.Width>256||image.Height>256||new FileInfo(dialog.FileName).Length>2*1024*1024)throw new InvalidDataException("图案须在256×256像素以内，文件小于2MB。");
          }
          string dest=Path.Combine(Catalog.Folder,Path.GetFileName(dialog.FileName));
          if(!string.Equals(Path.GetFullPath(dest),Path.GetFullPath(dialog.FileName),StringComparison.OrdinalIgnoreCase)){
            if(File.Exists(dest)&&MessageBox.Show("同名图案已存在，是否覆盖？","确认覆盖",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            File.Copy(dialog.FileName,dest,true);
          }
          Reload();
          ChoosePattern(Path.GetFileNameWithoutExtension(dest));
        }
        catch(Exception ex){
          MessageBox.Show("导入失败："+ex.Message,"针织边框",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
      }
    }
    void OpenEditor(){
      if(appEditor!=null&&!appEditor.IsDisposed){
        appEditor.Activate();
        return;
      }
      appEditor=new AppSettingsForm(this);
      appEditor.Show();
    }
    void ShowPreview(){
      var form=new Form{
        Text="当前针织纹理预览",ClientSize=new Size(720,260),StartPosition=FormStartPosition.CenterScreen,BackColor=Color.FromArgb(247,244,236)
      };
      var app=RunningApps().FirstOrDefault()??new AppInfo{
        Key="preview",Name="Claude",DisplayName="预览",Executable=""
      };
      var picture=new PictureBox{
        Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.CenterImage
      };
      int band=BorderGeometry.Thickness(96,Settings.Width);
      picture.Image=KnitRenderer.RenderStrip(new Rectangle(0,0,620,160),new Size(620,160),band,0,StyleFor(app));
      form.Controls.Add(picture);
      form.FormClosed+=delegate{
        picture.Image.Dispose();
      };
      form.Show();
    }
    static Icon CreateYarnIcon(){
      using(var bmp=new Bitmap(32,32)){
        using(Graphics g=Graphics.FromImage(bmp)){
          g.SmoothingMode=SmoothingMode.AntiAlias;
          g.Clear(Color.Transparent);
          using(var b=new SolidBrush(Color.FromArgb(132,159,144)))g.FillEllipse(b,3,4,26,26);
          using(var p=new Pen(Color.FromArgb(246,240,222),2)){
            g.DrawArc(p,5,8,22,15,30,220);
            g.DrawArc(p,8,4,14,25,230,230);
            g.DrawLine(p,7,11,23,24);
            g.DrawLine(p,5,17,17,28);
          }
        }
        IntPtr h=bmp.GetHicon();
        try{
          using(var i=Icon.FromHandle(h))return (Icon)i.Clone();
        }
        finally{
          NativeMethods.DestroyIcon(h);
        }
      }
    }
    protected override void ExitThreadCore(){
      Dispose();
      base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing){
      if(disposing&&!disposed){
        disposed=true;
        timer.Stop();
        timer.Dispose();
        tray.Visible=false;
        tray.Dispose();
        if(appEditor!=null)appEditor.Dispose();
        foreach(var g in groups.Values)foreach(var b in g)b.Dispose();
        groups.Clear();
        foreach(ToolStripItem item in menu.Items.Cast<ToolStripItem>().ToArray())DisposeMenuItem(item);
        menu.Dispose();
        if(icon!=null)icon.Dispose();
        tracker.Dispose();
        KnitRenderer.ClearCache();
      }
      base.Dispose(disposing);
    }
  }
}
