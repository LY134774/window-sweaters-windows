using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WindowSweaters;
internal static class RenderPreview
{
 static void Main() {
  string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","docs"); Directory.CreateDirectory(directory);
  using(Bitmap image=new Bitmap(880,540)) using(Graphics g=Graphics.FromImage(image)) {
   g.Clear(Color.FromArgb(238,235,226));
   Rectangle target=new Rectangle(70,70,740,380);
   using(Brush paper=new SolidBrush(Color.FromArgb(250,248,242))) g.FillRectangle(paper,target);
   foreach(Rectangle part in BorderGeometry.Calculate(target,18,false))
    using(Bitmap strip=KnitRenderer.Render(part.Size,96,new Point(part.X-52,part.Y-52))) g.DrawImageUnscaled(strip,part.Location);
   using(Font title=new Font("Microsoft YaHei UI",24,FontStyle.Bold))
   using(Font body=new Font("Microsoft YaHei UI",13))
   using(Font small=new Font("Microsoft YaHei UI",10))
   using(Brush ink=new SolidBrush(Color.FromArgb(45,75,58))) {
    g.DrawString("给窗口穿上一件毛衣",title,ink,112,125);
    g.DrawString("Windows 针织边框 · 基础版",body,ink,116,200);
    g.DrawString("装饰当前窗口  /  跟随移动缩放  /  托盘暂停与退出",body,ink,116,260);
    g.DrawString("纹理示意图，非桌面截图；实际效果请运行程序查看。",small,ink,113,375);
   }
   image.Save(Path.Combine(directory,"针织纹理预览.png"),ImageFormat.Png);
  }
 }
}
