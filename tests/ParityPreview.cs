using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WindowSweaters;
internal static class ParityPreview{
  static void Main(){
    var folder=Path.Combine(Path.GetTempPath(),"SweaterPreview-"+Guid.NewGuid());
    try{
      var catalog=new PatternCatalog(folder);
      string[] patterns={
        "atelier-claude","atelier-chrome","atelier-word","zigzag","plain","rib"
      };
      Color[] colors={
        Color.FromArgb(213,133,97),Color.FromArgb(244,240,230),Color.FromArgb(40,85,161),Color.FromArgb(132,159,144),Color.FromArgb(185,166,146),Color.FromArgb(151,162,180)
      };
      using(var image=new Bitmap(1120,640))using(Graphics g=Graphics.FromImage(image))using(var title=new Font("Microsoft YaHei UI",18,FontStyle.Bold))using(var label=new Font("Microsoft YaHei UI",11)){
        g.Clear(Color.FromArgb(242,239,232));
        g.DrawString("Windows 完整设置版 · 针织样张",title,Brushes.DarkSlateGray,35,20);
        g.DrawString("采用原版图案与毛线光照规则；这是渲染样张，非桌面截图。",label,Brushes.DimGray,38,62);
        for(int i=0;i<6;i++){
          int x=38+i%3*365,y=115+i/3*245;
          Size outer=new Size(310,172);
          var style=new RenderStyle(colors[i],catalog.Find(patterns[i]),6,patterns[i]=="rib");
          using(var b=KnitRenderer.RenderStrip(new Rectangle(Point.Empty,outer),outer,24,8,style))g.DrawImageUnscaled(b,x,y);
          g.DrawString(catalog.Title(patterns[i]),label,Brushes.DarkSlateGray,x+5,y+190);
        }
        image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","docs","新版针织样张.png"),ImageFormat.Png);
      }
    }
    finally{
      Directory.Delete(folder,true);
    }
  }
}
