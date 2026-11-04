using System;
using System.Drawing;
using System.IO;
using System.Linq;
using WindowSweaters;
internal static class ParityTests {
  static int passed;
  static void Check(bool c,string name){
    if(!c)throw new Exception(name);
    passed++;
    Console.WriteLine("PASS "+name);
  }
  static int Main(){
    string folder=Path.Combine(Path.GetTempPath(),"SweatersTests-"+Guid.NewGuid());
    Directory.CreateDirectory(folder);
    try {
      var settings=new UserSettings();
      settings.Width=8;
      settings.Rows=10;
      settings.Pattern="zigzag";
      settings.Enabled=false;
      settings.SetAppEnabled("chrome",false);
      var store=new SettingsStore(folder);
      store.Save(settings);
      var read=store.Load();
      Check(read.Width==8 && read.Rows==10 && read.Pattern=="zigzag" && !read.Enabled,"settings survive restart");
      Check(!read.IsAppEnabled("CHROME") && read.IsAppEnabled("word"),"individual exclusions survive restart and ignore case");
      read.SetAllApps(false);
      read.SetAppEnabled("code",true);
      store.Save(read);
      read=store.Load();
      Check(!read.IsAppEnabled("new-app") && read.IsAppEnabled("Code"),"all off also affects future apps with exceptions");
      store.Save(read);
      File.WriteAllText(Path.Combine(folder,"settings.json"),"{broken");
      read=store.Load();
      Check(store.Warning.Length>0 && read.Width==8,"corrupt settings recover last valid backup with warning");
      File.WriteAllText(Path.Combine(folder,"settings.json"),"{}");
      read=store.Load();
      Check(read.Enabled&&read.AppsOn&&read.Width==12,"missing JSON fields use safe default preferences");
      var catalog=new PatternCatalog(Path.Combine(folder,"charts"));
      Check(catalog.All.Count>=45 && catalog.Find("atelier-word")!=null,"upstream collection loaded");
      Check(catalog.Find("zigzag").Width==12 && catalog.Find("zigzag").Height==6,"upstream zigzag matrix dimensions");
      Check(catalog.Find("zigzag").Pixel(0,0).ToArgb()==Color.FromArgb(246,240,222).ToArgb(),"upstream cream retained");
      Check(catalog.Find("zigzag").Pixel(1,0).A==0,"transparent pixels inherit app yarn");
      Check(catalog.EffectiveRows("zigzag",3)==6 && catalog.EffectiveRows("plain",3)==3,"motifs cannot be cropped by chunky setting");
      using(var png=new Bitmap(3,2)){
        png.SetPixel(0,0,Color.Red);
        png.SetPixel(1,0,Color.Transparent);
        png.Save(Path.Combine(folder,"charts","mine.png"));
      }
      catalog.Reload();
      Check(catalog.Find("mine").Custom && catalog.Find("mine").Pixel(0,0).R==255,"custom PNG loads colors and row orientation");
      File.WriteAllText(Path.Combine(folder,"charts","bad.png"),"bad");
      catalog.Reload();
      Check(catalog.Find("mine")!=null && catalog.Warnings.Count>0,"bad PNG does not remove valid patterns");
      File.WriteAllText(Path.Combine(folder,"apps.conf"),"Code = #123456 atelier-word\nCode Helper = #ABCDEF zigzag\nbad = #12xyz0\n");
      var profiles=new AppProfiles(folder);
      var p=profiles.Find("Code Helper");
      Check(p.Color.R==171 && p.Pattern=="zigzag","longest personal prefix takes priority");
      Check(profiles.Find("WINWORD").Pattern=="atelier-word","Windows executable maps to original Word profile");
      profiles.SetCustom("Code",Color.Coral,"picnic");
      profiles.Reload();
      Check(profiles.Find("Code").Color.ToArgb()==Color.Coral.ToArgb(),"UI personal color stored in apps.conf");
      profiles.SetCustom("Space App",Color.SkyBlue,"mine test");
      profiles.Reload();
      Check(profiles.Find("Space App").Pattern=="mine test","personal rule supports PNG filenames containing spaces");
      using(var reserved=new Bitmap(2,2))reserved.Save(Path.Combine(folder,"charts","plain.png"));
      catalog.Reload();
      Check(catalog.Find("plain")==null&&catalog.Warnings.Count>0,"reserved PNG names cannot replace plain or by-app modes");
      Check(catalog.Featured.All(n=>catalog.Find(n)!=null),"all six featured patterns exist");
      Console.WriteLine(passed+" passed, 0 failed");
      return 0;
    }
    catch(Exception ex){
      Console.Error.WriteLine("FAIL "+ex);
      return 1;
    }
    finally{
      Directory.Delete(folder,true);
    }
  }
}
