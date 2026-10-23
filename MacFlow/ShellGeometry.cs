using System;
namespace MacFlow
{
    // Read the work area for this display rather than assuming a 58-DIP
    // Windows taskbar. Handles auto-hide, taskbars on another edge and DPI.
    internal static class ShellGeometry
    {
        internal static double BottomInset(DisplayArea area,bool replaced)
        {return replaced?8:Math.Max(8,(area.Pixels.Bottom-area.WorkingPixels.Bottom)/area.Scale+8);}
        internal static double SideInset(DisplayArea area,bool left,bool replaced)
        {return replaced?10:Math.Max(10,(left?area.WorkingPixels.Left-area.Pixels.Left:area.Pixels.Right-area.WorkingPixels.Right)/area.Scale+10);}
    }
}
