using System;
using System.Diagnostics;
using System.IO;
internal static class IntegrationRunner{
  static int Main(){
    string root=AppDomain.CurrentDomain.BaseDirectory;
    string folder=Path.Combine(root,"runtime-"+Guid.NewGuid());
    Directory.CreateDirectory(folder);
    Process host=null,scenario=null;
    try{
      host=Process.Start(new ProcessStartInfo{
        FileName=Path.Combine(root,"TestHost.exe"),Arguments="\""+folder+"\"",UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden
      });
      System.Threading.Thread.Sleep(300);
      scenario=Process.Start(new ProcessStartInfo{
        FileName=Path.Combine(root,"MultiWindowScenario.exe"),Arguments=host.Id.ToString(),UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Normal
      });
      if(!scenario.WaitForExit(18000)){
        scenario.Kill();
        throw new Exception("window scenario timed out");
      }
      Console.Write(File.ReadAllText(Path.Combine(root,"多窗口验收.txt")));
      return scenario.ExitCode;
    }
    catch(Exception e){
      Console.Error.WriteLine(e.Message);
      return 1;
    }
    finally{
      if(host!=null){
        if(!host.HasExited){
          File.WriteAllText(Path.Combine(folder,"stop"),"stop");
          if(!host.WaitForExit(5000))host.Kill();
        }
        host.Dispose();
      }
      if(scenario!=null)scenario.Dispose();
      if(Path.GetFullPath(folder).StartsWith(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase)&&Directory.Exists(folder))Directory.Delete(folder,true);
    }
  }
}
