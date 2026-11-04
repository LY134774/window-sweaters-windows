using System;
using System.Drawing;
using System.Collections.Generic;
using WindowSweaters;
internal static class OcclusionTests{
  [STAThread] static int Main(){
    try{
      using(var r=WindowTracker.VisibleRegion(new Rectangle(10,10,100,10),new[]{
        new Rectangle(40,0,20,50)
      })){
        if(!r.IsVisible(5,5)||r.IsVisible(35,5)||!r.IsVisible(80,5))throw new Exception("foreground rectangle not subtracted in local coordinates");
      }
      using(var r=WindowTracker.VisibleRegion(new Rectangle(-100,0,80,10),new[]{
        new Rectangle(-200,-20,300,100)
      })){
        using(var m=new System.Drawing.Drawing2D.Matrix())if(r.GetRegionScans(m).Length!=0)throw new Exception("fully covered border remains visible");
      }
      var screen=System.Windows.Forms.SystemInformation.VirtualScreen;
      using(var outside=WindowTracker.VisibleRegion(new Rectangle(screen.Right+10,screen.Top,50,10),new Rectangle[0]))using(var m=new System.Drawing.Drawing2D.Matrix())if(outside.GetRegionScans(m).Length!=0)throw new Exception("offscreen strips remain native-visible");
      using(var border=new BorderWindow()){
        var style=new RenderStyle(Color.Coral,null,6,false);
        var rect=new Rectangle(screen.Left-10,screen.Top+100,40,12);
        border.Present(rect,new Rectangle(0,0,40,12),new Size(200,120),12,0,style,new Rectangle[0]);
        if(border.Region.IsVisible(5,5))throw new Exception("offscreen area not clipped");
        rect.X=screen.Left+30;
        border.Present(rect,new Rectangle(0,0,40,12),new Size(200,120),12,0,style,new Rectangle[0]);
        if(!border.Region.IsVisible(5,5))throw new Exception("moved border retains stale screen clipping");
      }
      Console.WriteLine("4 passed, 0 failed");
      return 0;
    }
    catch(Exception e){
      Console.Error.WriteLine(e);
      return 1;
    }
  }
}
