using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
[assembly: AssemblyTitle("窗口针织边框 Windows 完整设置版")]
[assembly: AssemblyVersion("0.2.1.0")]
[assembly: AssemblyFileVersion("0.2.1.0")]
namespace WindowSweaters
{
  internal static class Program
  {
    [STAThread] static void Main()
    {
      bool created;
      using(Mutex mutex=new Mutex(true,@"Local\WindowSweaters.Windows.Basic",out created)) {
        if(!created) {
          MessageBox.Show("针织边框已经在运行，请到右下角托盘查找毛线图标。","针织边框",MessageBoxButtons.OK,MessageBoxIcon.Information);
          return;
        }
        try {
          Application.EnableVisualStyles();
          Application.SetCompatibleTextRenderingDefault(false);
          Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
          using(SweaterContext context=new SweaterContext()) Application.Run(context);
        }
        catch(Exception ex) {
          MessageBox.Show("针织边框无法继续运行，已关闭。\n\n"+ex.Message,"针织边框",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        finally {
          mutex.ReleaseMutex();
        }
      }
    }
  }
}
