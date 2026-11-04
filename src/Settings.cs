using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace WindowSweaters {
  [DataContract] internal sealed class UserSettings {
    [DataMember] public bool Enabled=true;
    [DataMember] public double Width=12;
    [DataMember] public double Rows=6;
    [DataMember] public string Pattern="by-app";
    [DataMember] public bool AppsOn=true;
    [DataMember] public List<string> Exceptions=new List<string>();
    [OnDeserializing] void Initialize(StreamingContext context){
      Enabled=true;
      Width=12;
      Rows=6;
      Pattern="by-app";
      AppsOn=true;
      Exceptions=new List<string>();
    }
    internal bool IsAppEnabled(string key){
      return AppsOn != Exceptions.Any(x=>string.Equals(x,key,StringComparison.OrdinalIgnoreCase));
    }
    internal void SetAppEnabled(string key,bool on){
      Exceptions.RemoveAll(x=>string.Equals(x,key,StringComparison.OrdinalIgnoreCase));
      if(on!=AppsOn)Exceptions.Add(key);
    }
    internal void SetAllApps(bool on){
      AppsOn=on;
      Exceptions.Clear();
    }
    internal void Normalize(){
      if(double.IsNaN(Width)||double.IsInfinity(Width)||Width<2||Width>60)Width=12;
      if(double.IsNaN(Rows)||double.IsInfinity(Rows)||Rows<1.5||Rows>256)Rows=6;
      if(string.IsNullOrWhiteSpace(Pattern)||Pattern.Length>63)Pattern="by-app";
      Exceptions=(Exceptions??new List<string>()).Where(x=>!string.IsNullOrWhiteSpace(x)&&x.Length<260).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
  }
  internal sealed class SettingsStore {
    internal readonly string Folder;
    internal string Warning="";
    internal SettingsStore(string folder){
      Folder=folder;
      Directory.CreateDirectory(folder);
      Directory.CreateDirectory(Path.Combine(folder,"charts"));
    }
    UserSettings Read(string file){
      using(var s=File.OpenRead(file)){
        var result=(UserSettings)new DataContractJsonSerializer(typeof(UserSettings)).ReadObject(s);
        result.Normalize();
        return result;
      }
    }
    internal UserSettings Load(){
      string file=Path.Combine(Folder,"settings.json");
      if(!File.Exists(file))return new UserSettings();
      try{
        return Read(file);
      }
      catch(Exception){
        Warning="设置文件无法读取，已尝试恢复上次备份。";
        try{
          return Read(file+".bak");
        }
        catch(Exception){
          return new UserSettings();
        }
      }
    }
    internal void Save(UserSettings settings){
      settings.Normalize();
      string file=Path.Combine(Folder,"settings.json"),tmp=file+".tmp";
      using(var s=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)){
        new DataContractJsonSerializer(typeof(UserSettings)).WriteObject(s,settings);
        s.Flush(true);
      }
      if(File.Exists(file))File.Replace(tmp,file,file+".bak",true);
      else File.Move(tmp,file);
    }
  }
}
