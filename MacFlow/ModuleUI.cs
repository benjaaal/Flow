using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    // Shared module controls reuse MacFlow's material and existing interaction
    // clocks. Keep readable content outside the shader and remove opaque faces.
    internal static class ModuleUI
    {
        internal static Button Button(string text,Action action,bool prominent=false)
        {
            var b=Design.Button(text,action,prominent);System.Windows.Automation.AutomationProperties.SetName(b,text);if(!prominent){b.Background=Brushes.Transparent;b.Padding=new Thickness(0);var label=Design.Text(text,12,true);label.TextWrapping=TextWrapping.Wrap;b.Content=new GlassPiece(label,12,new Thickness(11,7,11,7));}return b;
        }
        internal static TextBox Input(string text,int max=4000,bool multiline=false)
        {
            var field=new TextBox{Text=text??"",MaxLength=max,AcceptsReturn=multiline,TextWrapping=multiline?TextWrapping.Wrap:TextWrapping.NoWrap,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(10,8,10,8),FontSize=13,FontFamily=new FontFamily("Segoe UI Variable Text, Segoe UI")};field.SetResourceReference(TextBox.ForegroundProperty,"GlassInk");field.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");return field;
        }
        internal static GlassPiece Field(UIElement field){return new GlassPiece(field,12,new Thickness(0)){Margin=new Thickness(0,6,0,10)};}
        internal static TextBlock Caption(string text){var label=Design.Text(text,11,false);label.TextWrapping=TextWrapping.Wrap;label.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");label.Margin=new Thickness(0,5,0,7);return label;}
    }
}
