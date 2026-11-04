using System;
using System.Drawing;
using System.IO;
using WindowSweaters;
internal static class RenderingTests {
  static int pass;
  static void Check(bool c,string s){
    if(!c)throw new Exception(s);
    pass++;
    Console.WriteLine("PASS "+s);
  }
  static int Main(){
    string folder=Path.Combine(Path.GetTempPath(),"SweatersRender-"+Guid.NewGuid());
    try{
      var catalog=new PatternCatalog(folder);
      var style=new RenderStyle(Color.FromArgb(213,133,97),catalog.Find("atelier-claude"),6,false);
      using(var b=KnitRenderer.RenderStrip(new Rectangle(0,0,260,20),new Size(260,170),12,8,style)){
        Check(b.GetPixel(0,0).A==0,"outer corner transparent");
        Check(b.GetPixel(130,5).A>240,"band opaque");
        Check(b.GetPixel(130,19).A==0,"window center remains transparent");
      }
      using(var a=KnitRenderer.RenderStrip(new Rectangle(0,0,240,12),new Size(240,180),12,0,style))using(var b=KnitRenderer.RenderStrip(new Rectangle(0,0,240,28),new Size(240,180),28,0,new RenderStyle(style.Color,style.Chart,6,false))){
        Check(a.Height!=b.Height && a.GetPixel(100,5)!=b.GetPixel(100,5),"width changes stitch geometry");
      }
      using(var a=KnitRenderer.Render(new Size(120,20),96,Point.Empty))using(var b=KnitRenderer.RenderStrip(new Rectangle(0,0,120,20),new Size(120,100),20,0,new RenderStyle(Color.Coral,null,3,true))){
        Check(a.GetPixel(40,3)!=b.GetPixel(40,3),"rib differs from stockinette");
      }
      foreach(int band in new[]{2,4,12}){
      var frame=new Rectangle(50,40,200,160);
      var strips=BorderGeometry.Calculate(frame,band,false,8);
      var outer=frame;
      outer.Inflate(band,band);
      using(var combined=new Bitmap(outer.Width,outer.Height))using(var full=KnitRenderer.RenderStrip(new Rectangle(Point.Empty,outer.Size),outer.Size,band,8,style)){
        using(var g=Graphics.FromImage(combined)){
          g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
          foreach(var strip in strips){
            var local=strip;
            local.Offset(-outer.X,-outer.Y);
            using(var b=KnitRenderer.RenderStrip(local,outer.Size,band,8,style))g.DrawImageUnscaled(b,local.Location);
          }
        }
        bool equal=true;
        for(int y=0;y<outer.Height;y++)for(int x=0;x<outer.Width;x++)if(combined.GetPixel(x,y)!=full.GetPixel(x,y))equal=false;
        Check(equal,"rounded ring stays complete at width "+band);
      }
      }
      foreach(int thin in new[]{2,4}){
        Check(BorderGeometry.Thickness(96,thin)==thin&&BorderGeometry.Thickness(144,thin)==thin*3/2&&BorderGeometry.Thickness(192,thin)==thin*2,"thin width "+thin+" scales correctly at 100/150/200 percent");
        using(var strip=KnitRenderer.RenderStrip(new Rectangle(0,0,220,thin),new Size(220,160),thin,0,new RenderStyle(Color.Coral,null,6,false)))
          Check(strip.GetPixel(110,thin-1).A==255,"thin width "+thin+" remains visible without covering window center");
      }
      Console.WriteLine(pass+" passed, 0 failed");
      return 0;
    }
    catch(Exception ex){
      Console.Error.WriteLine(ex);
      return 1;
    }
    finally { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
  }
}
