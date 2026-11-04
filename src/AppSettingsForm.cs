using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace WindowSweaters {
  internal sealed class AppSettingsForm:Form {
    readonly SweaterContext context;
    readonly ListBox apps=new ListBox{
      Dock=DockStyle.Fill
    };
    readonly ComboBox patterns=new ComboBox{
      DropDownStyle=ComboBoxStyle.DropDownList,Width=290
    };
    readonly Panel swatch=new Panel{
      Width=70,Height=30
    };
    Color color;
    internal AppSettingsForm(SweaterContext c){
      context=c;
      Text="软件专属颜色和图案";
      ClientSize=new Size(650,430);
      MinimumSize=new Size(580,400);
      StartPosition=FormStartPosition.CenterScreen;
      Font=new Font("Microsoft YaHei UI",10);
      var layout=new TableLayoutPanel{
        Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(18)
      };
      layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,43));
      layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,57));
      layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
      layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
      layout.Controls.Add(new Label{
        Text="选择软件，然后调整颜色与图案",AutoSize=true
      },0,0);
      layout.SetColumnSpan(layout.GetControlFromPosition(0,0),2);
      layout.Controls.Add(apps,0,1);
      var controls=new FlowLayoutPanel{
        Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(14,0,0,0)
      };
      controls.Controls.Add(new Label{
        Text="主毛线颜色",AutoSize=true
      });
      controls.Controls.Add(swatch);
      var choose=new Button{
        Text="选择颜色…",AutoSize=true
      };
      choose.Click+=delegate{
        using(var dialog=new ColorDialog{
          Color=color,FullOpen=true
        })if(dialog.ShowDialog(this)==DialogResult.OK){
          color=dialog.Color;
          swatch.BackColor=color;
        }
      };
      controls.Controls.Add(choose);
      controls.Controls.Add(new Label{
        Text="软件专属图案（全局选按软件时使用）",AutoSize=true,Margin=new Padding(0,15,0,8)
      });
      patterns.Items.Add("平针");
      foreach(var p in context.Catalog.All)patterns.Items.Add(p.Name);
      controls.Controls.Add(patterns);
      var save=new Button{
        Text="保存并生效",AutoSize=true,Margin=new Padding(0,18,0,5)
      };
      save.Click+=delegate{
        var a=apps.SelectedItem as AppInfo;
        if(a==null)return;
        try{
          context.Profiles.SetCustom(a.Name,color,patterns.SelectedIndex<=0?"":(string)patterns.SelectedItem);
          context.Changed();
          MessageBox.Show(this,"已保存。颜色会立即生效；软件专属图案在“按软件自动选择”模式下生效。","针织边框");
        }
        catch(Exception ex){
          MessageBox.Show(this,"保存失败："+ex.Message,"针织边框");
        }
      };
      controls.Controls.Add(save);
      var reset=new Button{
        Text="恢复该软件原配色",AutoSize=true
      };
      reset.Click+=delegate{
        var a=apps.SelectedItem as AppInfo;
        if(a==null)return;
        try{
          context.Profiles.RemoveCustom(a.Name);
          context.Changed();
          SelectApp();
        }
        catch(Exception ex){
          MessageBox.Show(this,ex.Message,"恢复失败");
        }
      };
      controls.Controls.Add(reset);
      layout.Controls.Add(controls,1,1);
      Controls.Add(layout);
      foreach(var a in context.RunningApps())apps.Items.Add(a);
      apps.SelectedIndexChanged+=delegate{
        SelectApp();
      };
      if(apps.Items.Count>0)apps.SelectedIndex=0;
    }
    void SelectApp(){
      var a=apps.SelectedItem as AppInfo;
      if(a==null)return;
      var style=context.StyleFor(a);
      var profile=context.Profiles.Find(a.Name);
      color=profile==null?style.Color:profile.Color;
      swatch.BackColor=color;
      string pattern=profile==null?"":profile.Pattern;
      patterns.SelectedIndex=patterns.Items.IndexOf(pattern);
      if(patterns.SelectedIndex<0)patterns.SelectedIndex=0;
    }
  }
}
