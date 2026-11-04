using System;
using System.IO;
using System.Windows.Forms;
using WindowSweaters;
internal static class TestHost{
  [STAThread]static void Main(string[] args){
    Application.EnableVisualStyles();
    using(var c=new SweaterContext(args[0]))using(var timer=new Timer{
      Interval=100
    }){
      timer.Tick+=delegate{
        if(File.Exists(Path.Combine(args[0],"stop")))c.ExitThread();
      };
      timer.Start();
      Application.Run(c);
      File.WriteAllText(Path.Combine(args[0],"exit.txt"),"NORMAL_EXIT");
    }
  }
}
