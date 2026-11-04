using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace WindowSweaters {
  internal sealed class AppProfile {
    internal string Name,Pattern;
    internal Color Color;
    internal bool Personal;
  }
  internal sealed class AppProfiles {
    readonly List<AppProfile> builtin=new List<AppProfile>(),personal=new List<AppProfile>();
    readonly string file;
    readonly Dictionary<string,string> aliases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
      {
        "winword","Microsoft Word"
      },{
        "excel","Microsoft Excel"
      },{
        "powerpnt","Microsoft PowerPoint"
      },{
        "outlook","Microsoft Outlook"
      },{
        "ms-teams","Microsoft Teams"
      },{
        "photoshop","Adobe Photoshop"
      },{
        "illustrator","Adobe Illustrator"
      }
    };
    internal AppProfiles(string folder){
      file=Path.Combine(folder,"apps.conf");
      BuiltInData.Profiles(this);
      if(!File.Exists(file))File.WriteAllText(file,"# 软件名 = #RRGGBB 图案名（可省略图案）\n# 例如 Code = #237CAF atelier-vscode\n");
      Reload();
    }
    internal void AddBuiltIn(string name,uint color,string pattern){
      builtin.Add(new AppProfile{
        Name=name,Color=Color.FromArgb(unchecked((int)color)),Pattern=pattern
      });
    }
    internal void Reload(){
      personal.Clear();
      foreach(string line in File.ReadAllLines(file)){
        Match m=Regex.Match(line,@"^\s*([^#=]+?)\s*=\s*#([0-9a-fA-F]{6})(?:\s+(?:""([^""]{1,63})""|([\w-]{1,63})))?\s*(?:#.*)?$");
        if(m.Success)personal.Add(new AppProfile{
          Name=m.Groups[1].Value.Trim(),Color=Color.FromArgb(unchecked((int)(0xff000000u|uint.Parse(m.Groups[2].Value,NumberStyles.HexNumber)))),Pattern=m.Groups[3].Success?m.Groups[3].Value:m.Groups[4].Value,Personal=true
        });
      }
    }
    internal AppProfile Find(string name){
      var rule=personal.Where(x=>name.StartsWith(x.Name,StringComparison.OrdinalIgnoreCase)).OrderByDescending(x=>x.Name.Length).FirstOrDefault();
      if(rule!=null)return rule;
      string normalized;
      if(aliases.TryGetValue(name,out normalized))name=normalized;
      return builtin.Where(x=>name.StartsWith(x.Name,StringComparison.OrdinalIgnoreCase)).OrderByDescending(x=>x.Name.Length).FirstOrDefault();
    }
    internal void SetCustom(string name,Color color,string pattern){
      if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny(new[]{
        '=','\n','\r','#'
      })>=0)throw new ArgumentException("软件名无效");
      var lines=File.ReadAllLines(file).Where(x=>!Regex.IsMatch(x,@"^\s*"+Regex.Escape(name)+@"\s*=",RegexOptions.IgnoreCase)).ToList();
      if(pattern!=null&&(pattern.Length>63||pattern.IndexOfAny(new[]{
        '"','\r','\n'
      })>=0))throw new ArgumentException("图案名无效");
      string token=string.IsNullOrEmpty(pattern)?"":Regex.IsMatch(pattern,@"^[\w-]+$")?pattern:"\""+pattern+"\"";
      lines.Add(name+" = #"+(color.ToArgb()&0xffffff).ToString("X6")+(token.Length==0?"":" "+token));
      Write(lines);
    }
    internal void RemoveCustom(string name){
      Write(File.ReadAllLines(file).Where(x=>!Regex.IsMatch(x,@"^\s*"+Regex.Escape(name)+@"\s*=",RegexOptions.IgnoreCase)).ToList());
    }
    void Write(List<string> lines){
      string tmp=file+".tmp";
      File.WriteAllLines(tmp,lines);
      File.Replace(tmp,file,file+".bak",true);
      Reload();
    }
  }
}
