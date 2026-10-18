using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MacFlow
{
    public sealed class SearchAction
    {
        public string Name,Detail;
        public string FilePath;
        public AppEntry App;
        public Action Execute;
    }
    public static class LocalFiles
    {
        public static List<string> Scan()
        {
            var result=new List<string>();var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots=new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")};
            foreach(var root in roots)
            {
                if(!Directory.Exists(root))continue;var queue=new Queue<string>();queue.Enqueue(root);
                while(queue.Count>0 && result.Count<15000)
                {
                    var dir=queue.Dequeue();if(!visited.Add(dir))continue;
                    try
                    {
                        foreach(var file in Directory.EnumerateFiles(dir)){if((File.GetAttributes(file)&FileAttributes.Hidden)==0)result.Add(file);if(result.Count>=15000)break;}
                        foreach(var sub in Directory.EnumerateDirectories(dir)){if((File.GetAttributes(sub)&(FileAttributes.ReparsePoint|FileAttributes.Hidden))==0 && Path.GetFileName(sub)!="node_modules" && Path.GetFileName(sub)!=".git")queue.Enqueue(sub);}
                    }
                    catch(UnauthorizedAccessException){}catch(IOException){}catch(System.Security.SecurityException){}
                }
            }
            return result;
        }
    }
    public sealed class Calculator
    {
        private string text;private int at;
        public static bool TryEvaluate(string input,out string answer)
        {
            answer=null;input=input.Replace('×','*').Replace('÷','/').Trim();if(input.Length<3||input.Length>160||!System.Text.RegularExpressions.Regex.IsMatch(input,"^[0-9+*/^()., -]+$")||input.IndexOfAny(new[]{'+','-','*','/','^'})<0)return false;
            try{var parser=new Calculator{text=input.Replace(',','.')};double value=parser.Sum();parser.Space();if(parser.at!=parser.text.Length||Double.IsNaN(value)||Double.IsInfinity(value))return false;answer=value.ToString("G12",CultureInfo.CurrentCulture);return true;}catch{return false;}
        }
        private void Space(){while(at<text.Length&&text[at]==' ')at++;}
        private bool Eat(char c){Space();if(at<text.Length&&text[at]==c){at++;return true;}return false;}
        private double Sum(){double v=Product();while(true){if(Eat('+'))v+=Product();else if(Eat('-'))v-=Product();else return v;}}
        private double Product(){double v=Unary();while(true){if(Eat('*'))v*=Unary();else if(Eat('/'))v/=Unary();else return v;}}
        private double Unary(){if(Eat('+'))return Unary();if(Eat('-'))return -Unary();return Power();}
        private double Power(){double v=Atom();return Eat('^')?Math.Pow(v,Unary()):v;}
        private double Atom(){if(Eat('(')){var v=Sum();if(!Eat(')'))throw new FormatException();return v;}Space();int start=at;while(at<text.Length&&(Char.IsDigit(text[at])||text[at]=='.'))at++;if(start==at)throw new FormatException();return Double.Parse(text.Substring(start,at-start),CultureInfo.InvariantCulture);}
    }
}
