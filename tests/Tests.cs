using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using WindowSweaters;

internal static class Tests
{
 static int passed;
 static void Check(bool value,string name) { if(!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
 [STAThread] static int Main(string[] args) {
  if(args.Length>0 && args[0]=="--fixture") { Fixture(); return 0; }
  try {
   Rectangle target=new Rectangle(100,80,500,300);
   Rectangle[] parts=BorderGeometry.Calculate(target,18,false);
   Check(parts.Length==4,"four strips");
   Check(parts[0]==new Rectangle(82,62,536,18),"normal border stays outside target");
   Check(parts[1]==new Rectangle(82,380,536,18),"bottom matches frame");
   Check(parts[2]==new Rectangle(82,80,18,300),"left avoids overlapping corners");
   Check(parts[3]==new Rectangle(600,80,18,300),"right matches frame");
   parts=BorderGeometry.Calculate(new Rectangle(-1920,0,1920,1080),18,true);
   Check(parts[0]==new Rectangle(-1920,0,1920,18),"maximized stays on negative-coordinate monitor");
   Check(parts[1].Bottom==1080 && parts[2].Height==1044,"maximized inside screen");
   Check(BorderGeometry.Thickness(96)==18 && BorderGeometry.Thickness(144)==27 && BorderGeometry.Thickness(192)==36,"DPI scales border");
   Check(BorderGeometry.Calculate(Rectangle.Empty,18,false).Length==0,"empty target hides border");
   Check(BorderGeometry.Calculate(new Rectangle(0,0,10,10),18,true).Length==0,"tiny target hides border");
   Check(!BorderGeometry.ShouldDecorate(false,false,false,false,true,true,false),"invisible window rejected");
   Check(!BorderGeometry.ShouldDecorate(true,true,false,false,true,true,false),"minimized window rejected");
   Check(!BorderGeometry.ShouldDecorate(true,false,true,false,true,true,false),"cloaked window rejected");
   Check(!BorderGeometry.ShouldDecorate(true,false,false,true,true,true,false),"own window rejected");
   Check(!BorderGeometry.ShouldDecorate(true,false,false,false,false,true,false),"desktop/taskbar rejected");
   Check(!BorderGeometry.ShouldDecorate(true,false,false,false,true,false,false),"frameless popup rejected");
   Check(!BorderGeometry.ShouldDecorate(true,false,false,false,true,true,true),"fullscreen hidden");
   Check(BorderGeometry.ShouldDecorate(true,false,false,false,true,true,false),"regular window accepted");
   using(Bitmap b=KnitRenderer.Render(new Size(360,18),96,Point.Empty)) {
    Check(b.PixelFormat==PixelFormat.Format32bppPArgb,"alpha bitmap for layered window");
    int opaque=0; var colors=new System.Collections.Generic.HashSet<int>();
    for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y); if(c.A>180)opaque++; colors.Add(c.ToArgb());}
    Check(opaque>360*12,"border visibly filled with yarn");
    Check(colors.Count>50,"textured yarn highlights and shading");
   }
   using(Bitmap a=KnitRenderer.Render(new Size(40,18),96,Point.Empty))
   using(Bitmap b=KnitRenderer.Render(new Size(20,18),96,new Point(20,0))) {
    bool match=true; for(int y=2;y<16;y++)for(int x=3;x<17;x++) if(a.GetPixel(x+20,y)!=b.GetPixel(x,y))match=false;
    Check(match,"adjacent strips share a continuous stitch grid");
   }
   Console.WriteLine(passed+" passed, 0 failed"); return 0;
  } catch(Exception ex) { Console.Error.WriteLine("FAIL "+ex.Message); return 1; }
 }
 internal static void Fixture() {
  Application.EnableVisualStyles();
  var f=new Form { Text="针织边框 · 验收窗口", StartPosition=FormStartPosition.CenterScreen, Size=new Size(720,470), BackColor=Color.FromArgb(247,244,235) };
  var title=new Label { Text="给窗口穿上一件毛衣", Font=new Font("Microsoft YaHei UI",23,FontStyle.Bold), Location=new Point(38,48),AutoSize=true, ForeColor=Color.FromArgb(39,65,53) };
  var note=new Label { Text="试着拖动、缩放这个窗口，再点击下面的按钮。\n边框应跟着窗口走，点击和输入都能正常使用。",Font=new Font("Microsoft YaHei UI",11),Location=new Point(42,117),Size=new Size(610,60), ForeColor=Color.FromArgb(83,91,81) };
  var button=new Button {Text="点击测试 · 0", Location=new Point(43,208),Size=new Size(190,46),Font=new Font("Microsoft YaHei UI",11)}; int count=0; button.Click+=delegate{count++;button.Text="点击测试 · "+count;};
  var input=new TextBox { Location=new Point(43,284), Size=new Size(570,32),Font=new Font("Microsoft YaHei UI",12),Text="这里可以正常输入" };
  f.Controls.AddRange(new Control[]{title,note,button,input}); Application.Run(f);
 }
}
