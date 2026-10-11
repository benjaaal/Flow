# Third-party source and visual references

`MacFlow/Native.cs` adapts WPF background clearing from [WPF UI WindowBackdrop.cs](https://github.com/lepoco/wpfui/blob/main/src/Wpf.Ui/Controls/Window/WindowBackdrop.cs). `MacFlow/WelcomeWindow.cs` adapts four letter shapes from the MIT project credited below. MacFlow's native controls and integration are authored for this project. No Apple images, icons or binaries are bundled.

Copyright (C) Leszek Pomianowski and WPF UI Contributors.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Additional references reviewed: [WPF Liquid Glass Effect](https://github.com/dragosniamtu/WPF-Liquid-Glass-Effect), [Windows Duo](https://github.com/kinhoy/windows-duo), [WPF TitleBar Menu](https://github.com/manfromarce/Wpf-TitleBar-Menu), and [Apple's macOS Tahoe design announcement](https://www.apple.com/newsroom/2025/06/macos-tahoe-26-makes-the-mac-more-capable-productive-and-intelligent-than-ever/). No code from these reference projects or Apple assets is redistributed. Lid projection and smoothing are authored for MacFlow using Windows power notifications and the documented HingeAngleSensor API. The current glass uses the Windows compositor with an optional own-window accent policy (undocumented), falling back to public DWM backdrop APIs. It does not exclude windows from capture or capture desktop images for the glass. Contrast checks read five pixels from the visible, unoccupied margins of owned windows every 400 ms; those pixels are never saved or used as backdrop textures. The lid effect captures one desktop frame in memory per transition and releases it afterwards; it does not persist or upload it. No third-party window hooks or Explorer injection are bundled.

## Handwritten greeting

The h, o, l and a cubic Bézier letter shapes in `MacFlow/WelcomeWindow.cs` are adapted from [kumailnanji/letters, src/custom-letters.ts](https://github.com/kumailnanji/letters/blob/main/src/custom-letters.ts). The reveal renderer and WPF integration are native implementations for MacFlow; no React or browser runtime is included.

MIT License

Copyright (c) 2026 Kumail Nanji

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Material and motion references (1.0.2)

Reviewed [sohumsuthar/liquid-glass](https://github.com/sohumsuthar/liquid-glass), [OverShifted/LiquidGlass](https://github.com/OverShifted/LiquidGlass), [jtrivedi/Wave](https://github.com/jtrivedi/Wave) and [flocked/Anima](https://github.com/flocked/Anima) for directional highlights, damping and interaction behavior. No code or assets from these projects are redistributed. CrystalLight.hlsl and the analytical spring are original WPF implementations. The optical shader supplies lighting only: the Windows compositor supplies backdrop blur, without real optical refraction. The apple-shaped menu mark is a locally authored vector fallback for the user-requested private-use character; no Apple font or image is bundled.

## References and original assets (1.0.3)

Reviewed [XamlFlair](https://github.com/XamlFlair/XamlFlair) and [QuickLook](https://github.com/QL-Win/QuickLook) as feature references. Neither project's code, binaries or assets are bundled. QuickLook is not installed by MacFlow. Media controls use Windows.Media.Control APIs in original application code. The three new wallpapers were generated with OpenAI image generation, rather than downloaded from Apple or GitHub. Explorer, Settings and MacFlow display locally authored, cached vector icons inside MacFlow; system icons are not overwritten.
