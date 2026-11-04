// Procedural knit material ported from window-sweaters v1.5.0 knit.c; GPL-3.0. See NOTICE.md.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
namespace WindowSweaters {
  internal sealed class RenderStyle {
    internal readonly Color Color;
    internal readonly Chart Chart;
    internal readonly double Rows;
    internal readonly bool Rib;
    internal RenderStyle(Color c,Chart chart,double rows,bool rib){
      Color=c;
      Chart=chart;
      Rows=rows;
      Rib=rib;
    }
    internal string Key{
      get{
        return Color.ToArgb()+":"+(Chart==null?0:Chart.Token)+":"+Rows.ToString(CultureInfo.InvariantCulture)+":"+Rib;
      }
    }
  }
  internal static class KnitRenderer {
    sealed class Tile{
      internal int W,H;
      internal double Width,Height;
      internal int[] Pixels;
    }
    static readonly Dictionary<string,Tile> cache=new Dictionary<string,Tile>();
    static readonly Queue<string> order=new Queue<string>();
    static int cachedPixels;
    internal static void ClearCache(){
      cache.Clear();
      order.Clear();
      cachedPixels=0;
    }
    internal static Bitmap Render(Size size,uint dpi,Point origin){
      var style=new RenderStyle(Color.FromArgb(105,139,109),null,6,false);
      return Fill(size,origin,GetTile(18*Math.Max(96u,dpi)/96.0,style));
    }
    static Bitmap Fill(Size size,Point origin,Tile tile){
      int[] data=new int[size.Width*size.Height];
      for(int y=0;y<size.Height;y++)for(int x=0;x<size.Width;x++)data[y*size.Width+x]=Sample(tile,x+origin.X,y+origin.Y);
      return BitmapOf(size,data);
    }
    internal static Bitmap RenderStrip(Rectangle strip,Size outer,int band,int radius,RenderStyle style){
      if(strip.Width<1||strip.Height<1||strip.Width>20200||strip.Height>20200)throw new ArgumentOutOfRangeException("strip");
      Tile tile=GetTile(band,style);
      Tile cuff=style.Chart!=null&&!style.Chart.Cuff.IsEmpty?GetTile(band,new RenderStyle(style.Chart.Cuff,null,style.Rows,false)):null;
      Tile corner=style.Chart!=null&&style.Chart.SolidCorners?GetTile(band,new RenderStyle(style.Color,null,style.Rows,false)):null;
      int[] data=new int[strip.Width*strip.Height];
      double outerRadius=radius+band;
      for(int y=0;y<strip.Height;y++)for(int x=0;x<strip.Width;x++){
        double gx=strip.X+x+.5,gy=strip.Y+y+.5;
        int hits=0;
        for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++){
          double px=gx+(sx==0?-.25:.25),py=gy+(sy==0?-.25:.25);
          if(InRound(px,py,outer.Width,outer.Height,outerRadius)&&!InRound(px-band,py-band,outer.Width-2*band,outer.Height-2*band,radius))hits++;
        }
        if(hits==0)continue;
        double d=gy,u=gx,v=gy,L=outer.Width;
        if(outer.Width-gx<d){
          d=outer.Width-gx;
          u=gy;
          v=d;
          L=outer.Height;
        }
        if(outer.Height-gy<d){
          d=outer.Height-gy;
          u=outer.Width-gx;
          v=d;
          L=outer.Width;
        }
        if(gx<d){
          u=outer.Height-gy;
          v=gx;
          L=outer.Height;
        }
        Tile chosen=cuff!=null&&v<band/3.0?cuff:corner!=null&&(u<outerRadius||u>L-outerRadius)?corner:tile;
        double tx=u;
        if(style.Chart!=null && style.Chart.Fitted){
          double count=Math.Max(1,Math.Round(L/tile.Width));
          tx=u*tile.Width/(L/count);
        }
        else if(corner!=null&&chosen==tile&&L>2*outerRadius){
          double available=L-2*outerRadius;
          tx=(u-outerRadius)*tile.Width/(available/Math.Max(1,Math.Round(available/tile.Width)));
        }
        double ty=style.Chart!=null&&style.Chart.Name.StartsWith("atelier-")?v:v-band*.5+chosen.Height*.5;
        int c=Sample(chosen,tx,ty);
        int alpha=hits*255/4;
        data[y*strip.Width+x]=(alpha<<24)|(((c>>16)&255)*alpha/255<<16)|(((c>>8)&255)*alpha/255<<8)|((c&255)*alpha/255);
      }
      return BitmapOf(strip.Size,data);
    }
    static bool InRound(double x,double y,double w,double h,double r){
      if(x<0||y<0||x>=w||y>=h)return false;
      r=Math.Min(r,Math.Min(w,h)/2);
      double dx=Math.Max(Math.Max(r-x,x-(w-r)),0),dy=Math.Max(Math.Max(r-y,y-(h-r)),0);
      return dx*dx+dy*dy<=r*r;
    }
    static int Sample(Tile t,double x,double y){
      double ax=(x/t.Width-Math.Floor(x/t.Width))*t.W,ay=(y/t.Height-Math.Floor(y/t.Height))*t.H;
      return t.Pixels[Math.Min(t.H-1,(int)ay)*t.W+Math.Min(t.W-1,(int)ax)];
    }
    static Bitmap BitmapOf(Size size,int[] data){
      Bitmap b=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
      BitmapData locked=b.LockBits(new Rectangle(Point.Empty,size),ImageLockMode.WriteOnly,b.PixelFormat);
      try{
        Marshal.Copy(data,0,locked.Scan0,data.Length);
      }
      finally{
        b.UnlockBits(locked);
      }
      return b;
    }
    static Tile GetTile(double band,RenderStyle style){
      string key=band.ToString(CultureInfo.InvariantCulture)+":"+style.Key;
      Tile tile;
      if(cache.TryGetValue(key,out tile))return tile;
      tile=MakeTile(band,style);
      while(order.Count>0&&(order.Count>=64||cachedPixels+tile.Pixels.Length>8000000)){
        string old=order.Dequeue();
        cachedPixels-=cache[old].Pixels.Length;
        cache.Remove(old);
      }
      cache[key]=tile;
      order.Enqueue(key);
      cachedPixels+=tile.Pixels.Length;
      return tile;
    }
    static Tile MakeTile(double band,RenderStyle style){
      Chart chart=style.Chart;
      double rows=Math.Max(1.5,style.Rows),step=band/rows,sw=step*1.35;
      int cols=chart==null?8:chart.Width,nrows=chart==null?8:chart.Height;
      double logicalW=Math.Max(8,sw*cols),logicalH=Math.Max(8,step*nrows);
      double S=Math.Min(3,Math.Sqrt(1500000/(logicalW*logicalH)));
      int w=Math.Max(8,(int)Math.Round(logicalW*S)),h=Math.Max(8,(int)Math.Round(logicalH*S));
      double scaleX=w/logicalW,scaleY=h/logicalH;
      sw=logicalW/cols;
      step=logicalH/nrows;
      double sh=step/.95,yarn=Math.Max(.8,sw*.4);
      bool sculpted=chart!=null;
      int[] color,height;
      using(var cb=new Bitmap(w,h,PixelFormat.Format32bppArgb))using(var hb=new Bitmap(w,h,PixelFormat.Format32bppArgb))using(Graphics cg=Graphics.FromImage(cb))using(Graphics hg=Graphics.FromImage(hb)){
        cg.Clear(Multiply(style.Color,sculpted?.94:.98));
        hg.Clear(Color.Black);
        cg.ScaleTransform((float)scaleX,(float)scaleY);
        hg.ScaleTransform((float)scaleX,(float)scaleY);
        cg.SmoothingMode=hg.SmoothingMode=SmoothingMode.AntiAlias;
        for(int j=-2;j<=nrows+1;j++)for(int i=-2;i<=cols+1;i++){
          Color c=chart==null?style.Color:chart.Pixel(i,j);
          if(c.A<128||(!chartSafeCuff(chart).IsEmpty&&c.ToArgb()==chartSafeCuff(chart).ToArgb()))c=style.Color;
          if(chart!=null&&!chart.Custom&&chart.Name=="zigzag")c=chart.Pixel(i,j).A<128?style.Color:Contrast(style.Color);
          using(var p=new GraphicsPath()){
            float x=(float)(i*sw),y=(float)(j*step);
            if(style.Rib&&chart==null){
              float rx=x+(i%2==0?0:(float)(sw*.16));
              p.AddLine(rx,y,rx,y+(float)(step*1.1));
            }
            else{
              double bx=sw*.14;
              Quad(p,x,y,x+(float)bx,y+(float)(sh*.55),x+(float)(sw*.5),y+(float)sh);
              Quad(p,x+(float)(sw*.5),y+(float)sh,x+(float)(sw-bx),y+(float)(sh*.55),x+(float)sw,y);
            }
            using(var pen=new Pen(c,(float)yarn)){
              pen.StartCap=pen.EndCap=LineCap.Round;
              cg.DrawPath(pen,p);
            }
            double[] widths={
              1,.8,.6,.4,.2
            },values={
              .22,.48,.70,.88,1
            };
            for(int k=0;k<5;k++)using(var pen=new Pen(Color.FromArgb((int)(values[k]*255),(int)(values[k]*255),(int)(values[k]*255)),(float)(yarn*widths[k]))){
              pen.StartCap=pen.EndCap=LineCap.Round;
              hg.DrawPath(pen,p);
            }
          }
        }
        color=Read(cb);
        height=Read(hb);
      }
      float[] hf=new float[w*h],tmp=new float[w*h];
      for(int i=0;i<hf.Length;i++)hf[i]=(height[i]&255)/255f;
      for(int n=0;n<2;n++)Blur(hf,tmp,w,h);
      int[] output=new int[w*h];
      double relief=S*(sculpted?1.65:.70);
      for(int y=0;y<h;y++)for(int x=0;x<w;x++){
        int i=y*w+x;
        double dx=(hf[y*w+(x+1)%w]-hf[y*w+(x+w-1)%w])*relief,dy=(hf[((y+1)%h)*w+x]-hf[((y+h-1)%h)*w+x])*relief;
        double ndl=Math.Max(0,(dx*.45+dy*.55+.70)/Math.Sqrt(dx*dx+dy*dy+1));
        double shade=(sculpted?.84:.94)+(sculpted?.28:.10)*ndl;
        shade*=sculpted?.74+.26*hf[i]:.86+.14*hf[i];
        Color c=Color.FromArgb(color[i]);
        double peak=Math.Max(c.R,Math.Max(c.G,c.B));
        double lift=sculpted?Math.Max(0,1-peak/48)*hf[i]*10:0;
        output[i]=unchecked((int)0xff000000)|((int)Math.Min(255,c.R*shade+lift)<<16)|((int)Math.Min(255,c.G*shade+lift)<<8)|(int)Math.Min(255,c.B*shade+lift);
      }
      return new Tile{
        W=w,H=h,Width=logicalW,Height=logicalH,Pixels=output
      };
    }
    static Color chartSafeCuff(Chart chart){
      return chart==null?Color.Empty:chart.Cuff;
    }
    static int[] Read(Bitmap b){
      int[] data=new int[b.Width*b.Height];
      var l=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      try{
        Marshal.Copy(l.Scan0,data,0,data.Length);
      }
      finally{
        b.UnlockBits(l);
      }
      return data;
    }
    static void Blur(float[] a,float[] tmp,int w,int h){
      for(int y=0;y<h;y++)for(int x=0;x<w;x++)tmp[y*w+x]=(a[y*w+(x+w-1)%w]+2*a[y*w+x]+a[y*w+(x+1)%w])*.25f;
      for(int y=0;y<h;y++)for(int x=0;x<w;x++)a[y*w+x]=(tmp[((y+h-1)%h)*w+x]+2*tmp[y*w+x]+tmp[((y+1)%h)*w+x])*.25f;
    }
    static void Quad(GraphicsPath p,float x,float y,float cx,float cy,float ex,float ey){
      p.AddBezier(x,y,x+2*(cx-x)/3,y+2*(cy-y)/3,ex+2*(cx-ex)/3,ey+2*(cy-ey)/3,ex,ey);
    }
    static Color Multiply(Color c,double f){
      return Color.FromArgb((int)(c.R*f),(int)(c.G*f),(int)(c.B*f));
    }
    internal static Color Contrast(Color c){
      if(c.GetBrightness()<.62)return Color.FromArgb(246,240,222);
      return Multiply(c,.62);
    }
  }
}
