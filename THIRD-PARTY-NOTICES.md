# Third-party source and visual references

`MacFlow/Native.cs` adapts WPF background clearing from [WPF UI WindowBackdrop.cs](https://github.com/lepoco/wpfui/blob/main/src/Wpf.Ui/Controls/Window/WindowBackdrop.cs). `MacFlow/WelcomeWindow.cs` adapts letter shapes from the MIT project credited below. MacFlow's native controls and integration are authored for this project. No Apple fonts, wallpaper images or binaries are bundled. The welcome brand silhouette is attributed below.

Copyright (C) Leszek Pomianowski and WPF UI Contributors.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Additional references reviewed: [WPF Liquid Glass Effect](https://github.com/dragosniamtu/WPF-Liquid-Glass-Effect), [Windows Duo](https://github.com/kinhoy/windows-duo), [WPF TitleBar Menu](https://github.com/manfromarce/Wpf-TitleBar-Menu), and [Apple's macOS Tahoe design announcement](https://www.apple.com/newsroom/2025/06/macos-tahoe-26-makes-the-mac-more-capable-productive-and-intelligent-than-ever/). No code from these reference projects or Apple assets is redistributed. Lid projection and smoothing are authored for MacFlow using Windows power notifications and the documented HingeAngleSensor API. The current glass uses the Windows compositor with an optional own-window accent policy (undocumented), falling back to public DWM backdrop APIs. It does not exclude windows from capture or capture desktop images for the glass. Contrast checks read five pixels from the visible, unoccupied margins of owned windows every 400 ms; those pixels are never saved or used as backdrop textures. The lid effect captures one desktop frame in memory per transition and releases it afterwards; it does not persist or upload it. No third-party window hooks or Explorer injection are bundled.

## Handwritten greeting

The a-z cubic Bézier letter shapes in `MacFlow/WelcomeWindow.cs` and `MacFlow/WelcomeScriptGlyphs.cs` are adapted from [kumailnanji/letters, src/custom-letters.ts](https://github.com/kumailnanji/letters/blob/main/src/custom-letters.ts). The reveal renderer and WPF integration are native implementations for MacFlow; no React or browser runtime is included.

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

## Welcome references (26.0.1)

Reviewed [AppleHello](https://github.com/mtynior/AppleHello) and [open-swiftui-animations](https://github.com/amosgyamfi/open-swiftui-animations/tree/master/Gists_To_Try/HandwrittenAnimation) as motion references; no code or assets from those two projects are redistributed. Latin lettering paths are adapted from kumailnanji/letters under the MIT license above. The 30 greeting strings and their WPF transitions are native local code.

The Apple silhouette in the welcome, menu and updater is adapted from [Simple Icons, icons/apple.svg](https://github.com/simple-icons/simple-icons/blob/develop/icons/apple.svg), available under CC0 1.0 Universal. This does not imply affiliation with Apple. WelcomeApple.hlsl is original mask lighting; it samples the vector alpha mask only. The desktop blur comes from the Windows compositor, and no optical refraction is claimed.

# CC0 1.0 Universal

## Statement of Purpose

The laws of most jurisdictions throughout the world automatically confer exclusive Copyright and Related Rights (defined below) upon the creator and subsequent owner(s) (each and all, an “owner”) of an original work of authorship and/or a database (each, a “Work”).

Certain owners wish to permanently relinquish those rights to a Work for the purpose of contributing to a commons of creative, cultural and scientific works (“Commons”) that the public can reliably and without fear of later claims of infringement build upon, modify, incorporate in other works, reuse and redistribute as freely as possible in any form whatsoever and for any purposes, including without limitation commercial purposes. These owners may contribute to the Commons to promote the ideal of a free culture and the further production of creative, cultural and scientific works, or to gain reputation or greater distribution for their Work in part through the use and efforts of others.

For these and/or other purposes and motivations, and without any expectation of additional consideration or compensation, the person associating CC0 with a Work (the “Affirmer”), to the extent that he or she is an owner of Copyright and Related Rights in the Work, voluntarily elects to apply CC0 to the Work and publicly distribute the Work under its terms, with knowledge of his or her Copyright and Related Rights in the Work and the meaning and intended legal effect of CC0 on those rights.

1. Copyright and Related Rights. A Work made available under CC0 may be protected by copyright and related or neighboring rights (“Copyright and Related Rights”). Copyright and Related Rights include, but are not limited to, the following:
    1. the right to reproduce, adapt, distribute, perform, display, communicate, and translate a Work;
    2. moral rights retained by the original author(s) and/or performer(s);
    3. publicity and privacy rights pertaining to a person’s image or likeness depicted in a Work;
    4. rights protecting against unfair competition in regards to a Work, subject to the limitations in paragraph 4(i), below;
    5. rights protecting the extraction, dissemination, use and reuse of data in a Work;
    6. database rights (such as those arising under Directive 96/9/EC of the European Parliament and of the Council of 11 March 1996 on the legal protection of databases, and under any national implementation thereof, including any amended or successor version of such directive); and
    7. other similar, equivalent or corresponding rights throughout the world based on applicable law or treaty, and any national implementations thereof.

2. Waiver. To the greatest extent permitted by, but not in contravention of, applicable law, Affirmer hereby overtly, fully, permanently, irrevocably and unconditionally waives, abandons, and surrenders all of Affirmer’s Copyright and Related Rights and associated claims and causes of action, whether now known or unknown (including existing as well as future claims and causes of action), in the Work (i) in all territories worldwide, (ii) for the maximum duration provided by applicable law or treaty (including future time extensions), (iii) in any current or future medium and for any number of copies, and (iv) for any purpose whatsoever, including without limitation commercial, advertising or promotional purposes (the “Waiver”). Affirmer makes the Waiver for the benefit of each member of the public at large and to the detriment of Affirmer’s heirs and successors, fully intending that such Waiver shall not be subject to revocation, rescission, cancellation, termination, or any other legal or equitable action to disrupt the quiet enjoyment of the Work by the public as contemplated by Affirmer’s express Statement of Purpose.

3. Public License Fallback. Should any part of the Waiver for any reason be judged legally invalid or ineffective under applicable law, then the Waiver shall be preserved to the maximum extent permitted taking into account Affirmer’s express Statement of Purpose. In addition, to the extent the Waiver is so judged Affirmer hereby grants to each affected person a royalty-free, non transferable, non sublicensable, non exclusive, irrevocable and unconditional license to exercise Affirmer’s Copyright and Related Rights in the Work (i) in all territories worldwide, (ii) for the maximum duration provided by applicable law or treaty (including future time extensions), (iii) in any current or future medium and for any number of copies, and (iv) for any purpose whatsoever, including without limitation commercial, advertising or promotional purposes (the “License”). The License shall be deemed effective as of the date CC0 was applied by Affirmer to the Work. Should any part of the License for any reason be judged legally invalid or ineffective under applicable law, such partial invalidity or ineffectiveness shall not invalidate the remainder of the License, and in such case Affirmer hereby affirms that he or she will not (i) exercise any of his or her remaining Copyright and Related Rights in the Work or (ii) assert any associated claims and causes of action with respect to the Work, in either case contrary to Affirmer’s express Statement of Purpose.

4. Limitations and Disclaimers.
    1. No trademark or patent rights held by Affirmer are waived, abandoned, surrendered, licensed or otherwise affected by this document.
    2. Affirmer offers the Work as-is and makes no representations or warranties of any kind concerning the Work, express, implied, statutory or otherwise, including without limitation warranties of title, merchantability, fitness for a particular purpose, non infringement, or the absence of latent or other defects, accuracy, or the present or absence of errors, whether or not discoverable, all to the greatest extent permissible under applicable law.
    3. Affirmer disclaims responsibility for clearing rights of other persons that may apply to the Work or any use thereof, including without limitation any person’s Copyright and Related Rights in the Work. Further, Affirmer disclaims responsibility for obtaining any necessary consents, permissions or other rights required for any use of the Work.
    4. Affirmer understands and acknowledges that Creative Commons is not a party to this document and has no duty or obligation with respect to this CC0 or use of the Work.

For more information, please see <https://creativecommons.org/publicdomain/zero/1.0>.


## App icon variants (26.1.0)
Seven brand geometries from Simple Icons, CC0-1.0. Pinned commit: dc7b10662a0a2cfdfc9305e2a2b06c023c30724a.
Source: https://github.com/simple-icons/simple-icons/tree/dc7b10662a0a2cfdfc9305e2a2b06c023c30724a/icons
Included: Google Chrome, WhatsApp, Roblox, Spotify, Firefox Browser, VLC media player, Discord. The CC0 text is included above; brand marks remain the property of their owners. Artwork identifies installed applications and does not imply endorsement.

## Weather data
Weather: https://open-meteo.com/ — CC BY 4.0 data. Geocoding: GeoNames, CC BY 4.0.
The bundled free endpoint is for non-commercial use, is contacted only after selecting a city, and can be unavailable. A commercial distribution must configure an appropriate licensed service. No location is inferred automatically.
