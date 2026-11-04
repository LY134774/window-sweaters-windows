using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
namespace WindowSweaters {
  internal sealed class Chart {
    static int nextToken;
    internal readonly int Token=System.Threading.Interlocked.Increment(ref nextToken);
    internal readonly string Name;
    internal readonly int Width,Height;
    internal readonly Color[] Pixels;
    internal bool Custom;
    internal bool Fitted {
      get {
        return new[]{
          "atelier-finder","atelier-terminal","atelier-grok","atelier-granola","atelier-illustrator","atelier-chrome"
        }
        .Contains(Name);
      }
    }
    internal bool SolidCorners {
      get{
        return Name=="atelier-whatsapp";
      }
    }
    internal Color Cuff {
      get{
        return !Custom && (Name=="atelier-notes"||Name=="atelier-calendar")?Pixels.First(x=>x.A>0):Color.Empty;
      }
    }
    internal Chart(string name,string[] rows,uint[] palette){
      Name=name;
      Width=rows[0].Length;
      Height=rows.Length;
      Pixels=new Color[Width*Height];
      for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){
        char c=rows[y][x];
        Pixels[y*Width+x]=c=='.'?Color.Transparent:Color.FromArgb(unchecked((int)palette[c-'a']));
      }
    }
    internal Chart(string name,Bitmap bitmap){
      Name=name;
      Custom=true;
      Width=bitmap.Width;
      Height=bitmap.Height;
      Pixels=new Color[Width*Height];
      for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)Pixels[y*Width+x]=bitmap.GetPixel(x,y);
    }
    internal Color Pixel(int x,int y){
      return Pixels[((y%Height+Height)%Height)*Width+(x%Width+Width)%Width];
    }
  }
  internal sealed class PatternCatalog {
    readonly Dictionary<string,Chart> charts=new Dictionary<string,Chart>(StringComparer.OrdinalIgnoreCase);
    internal readonly string Folder;
    internal readonly List<string> Warnings=new List<string>();
    internal int Generation;
    internal readonly string[] Featured={
      "zigzag","picnic","ribbon","posy","twinkle","candy-stripe"
    };
    internal List<Chart> All {
      get{
        return charts.Values.OrderBy(x=>x.Name).ToList();
      }
    }
    internal PatternCatalog(string folder){
      Folder=folder;
      Directory.CreateDirectory(folder);
      Reload();
    }
    internal void Add(Chart c){
      charts[c.Name]=c;
    }
    internal Chart Find(string name){
      Chart c;
      return name!=null&&charts.TryGetValue(name,out c)?c:null;
    }
    internal double EffectiveRows(string pattern,double rows){
      Chart c=Find(pattern);
      return c==null?Math.Max(3,rows):Math.Max(Math.Max(6,c.Height),rows);
    }
    internal void Reload(){
      Warnings.Clear();
      charts.Clear();
      BuiltInData.Charts(this);
      foreach(string file in Directory.GetFiles(Folder,"*.png").OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)){
        try{
          string name=Path.GetFileNameWithoutExtension(file);
          if(new[]{
            "by-app","plain","rib"
          }
          .Contains(name,StringComparer.OrdinalIgnoreCase))throw new InvalidDataException("文件名与内置模式重名，请换一个文件名");
          if(name.Length==0||name.Length>63)throw new InvalidDataException("名称应不超过63个字符");
          if(new FileInfo(file).Length>2*1024*1024)throw new InvalidDataException("文件应小于2MB");
          using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var img=Image.FromStream(stream)){
            if(img.Width<1||img.Height<1||img.Width>256||img.Height>256)throw new InvalidDataException("图案尺寸应为1至256像素");
            if(!charts.ContainsKey(name)&&charts.Count>=128)throw new InvalidDataException("最多支持128个图案");
            using(var bmp=new Bitmap(img))Add(new Chart(name,bmp));
          }
        }
        catch(Exception ex){
          Warnings.Add(Path.GetFileName(file)+"："+ex.Message);
        }
      }
      Generation++;
    }
    internal string Title(string pattern){
      switch(pattern){
        case "by-app":return "按软件自动选择";
        case "plain":return "平针";
        case "rib":return "罗纹";
        case "zigzag":return "锯齿";
        case "picnic":return "野餐格纹";
        case "ribbon":return "丝带条纹";
        case "posy":return "小蝴蝶结";
        case "twinkle":return "小星星";
        case "candy-stripe":return "糖果条纹";
        default:return pattern.StartsWith("atelier-")?pattern.Substring(8)+" 专属图案":pattern;
      }
    }
  }
}
