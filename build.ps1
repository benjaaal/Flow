param([switch]$RunTests,[string]$OutputDirectory,[string]$UpdateFeedUrl)
$ErrorActionPreference = 'Stop'
if ($UpdateFeedUrl) { $channelUri=$null; if (![Uri]::TryCreate($UpdateFeedUrl,[UriKind]::Absolute,[ref]$channelUri) -or $channelUri.Scheme -ne 'https' -or $channelUri.UserInfo -or $channelUri.Fragment) {throw 'El canal publicado debe ser un enlace HTTPS al manifiesto.'} }
$root = $PSScriptRoot
$deliverables = if ($OutputDirectory) { [System.IO.Path]::GetFullPath($OutputDirectory) } else { Split-Path -Parent $root }
$bin = Join-Path $deliverables 'portable'
$build = Join-Path $root 'build'
New-Item -ItemType Directory -Force -Path $bin,$build | Out-Null
$channelFile=Join-Path $build 'Channel.json'
[System.IO.File]::WriteAllText($channelFile,(@{FeedUrl=$UpdateFeedUrl} | ConvertTo-Json),[System.Text.UTF8Encoding]::new($false))
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $csc)) { throw 'Se requiere Windows de 64 bits con .NET Framework 4.8.' }
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap(64,64)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::FromArgb(255,23,32,48))
$points = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new(32,6),[System.Drawing.PointF]::new(58,32),[System.Drawing.PointF]::new(32,58),[System.Drawing.PointF]::new(6,32))
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,141,174,255),5)
$pen.StartCap=$pen.EndCap=[Drawing.Drawing2D.LineCap]::Round
$graphics.DrawBezier($pen,13,44,20,13,27,10,33,32)
$graphics.DrawBezier($pen,33,32,40,55,47,49,52,19)
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$iconPath = Join-Path $bin 'MacFlow.ico'
$stream = [System.IO.File]::Create($iconPath)
$icon.Save($stream); $stream.Dispose(); $icon.Dispose(); $pen.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
$references = @('System.dll','System.Core.dll','System.Numerics.dll','System.Management.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Runtime.Serialization.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','Microsoft.CSharp.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$common = @('/nologo','/target:winexe','/platform:x64','/optimize+','/langversion:5','/utf8output',('/resource:' + (Join-Path $root 'MacFlow\Theme.xaml') + ',MacFlow.Theme.xaml'),('/win32manifest:' + (Join-Path $root 'MacFlow\app.manifest')),('/win32icon:' + $iconPath)) + $references
$common+=('/resource:'+$channelFile+',MacFlow.Channel.json')
$radioRuntime = Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Runtime') -Filter System.Runtime.dll -Recurse | Select-Object -First 1
if (!$radioRuntime) { throw 'No se encontró la proyección WinRT de .NET Framework.' }
$common += @(('/reference:'+$radioRuntime.FullName),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Devices.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Foundation.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Media.winmd')))
$common += @(('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.UI.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.System.winmd'))); $vectorRuntime=Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Numerics.Vectors') -Filter System.Numerics.Vectors.dll -Recurse | Select-Object -First 1; $common += ('/reference:'+$vectorRuntime.FullName)
$winrtInterop = Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Runtime.InteropServices.WindowsRuntime') -Filter System.Runtime.InteropServices.WindowsRuntime.dll -Recurse | Select-Object -First 1
$common += ('/reference:'+$winrtInterop.FullName)
$shaderCompiler = Join-Path $build 'CompileShader.exe'
& $csc /nologo /target:exe ('/out:' + $shaderCompiler) (Join-Path $root 'BuildTools/CompileShader.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la herramienta de shaders.' }
& $shaderCompiler (Join-Path $root 'MacFlow/LiquidGlass.hlsl') (Join-Path $build 'LiquidGlass.ps')
if ($LASTEXITCODE -ne 0) { throw 'Falló el shader de cristal.' }
& $shaderCompiler (Join-Path $root 'MacFlow/LidFold.hlsl') (Join-Path $build 'LidFold.ps')
if ($LASTEXITCODE -ne 0) { throw 'Falló el shader de tapa.' }
& $shaderCompiler (Join-Path $root 'MacFlow/CrystalLight.hlsl') (Join-Path $build 'CrystalLight.ps')
if ($LASTEXITCODE -ne 0) { throw 'Falló el shader de iluminación.' }
& $shaderCompiler (Join-Path $root 'MacFlow/WelcomeApple.hlsl') (Join-Path $build 'WelcomeApple.ps')
if ($LASTEXITCODE -ne 0) { throw 'Fallo el shader del saludo.' }
$appSources = Get-ChildItem -LiteralPath (Join-Path $root 'MacFlow') -Filter '*.cs' | ForEach-Object FullName
$wallpaperResources=Get-ChildItem -LiteralPath (Join-Path $root 'MacFlow/Assets/Wallpapers') -Filter '*.png' | ForEach-Object { '/resource:'+$_.FullName+',MacFlow.Wallpaper.'+$_.Name }
& $csc @common @wallpaperResources ('/resource:' + (Join-Path $build 'WelcomeApple.ps') + ',MacFlow.WelcomeApple.ps') ('/resource:' + (Join-Path $build 'LiquidGlass.ps') + ',MacFlow.LiquidGlass.ps') ('/resource:' + (Join-Path $build 'LidFold.ps') + ',MacFlow.LidFold.ps') ('/resource:' + (Join-Path $build 'CrystalLight.ps') + ',MacFlow.CrystalLight.ps') ('/out:' + (Join-Path $bin 'MacFlow.exe')) '/main:MacFlow.Program' @appSources
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de MacFlow.' }
Copy-Item -LiteralPath (Join-Path $root 'MacFlow\App.config') -Destination (Join-Path $bin 'MacFlow.exe.config') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.txt') -Destination (Join-Path $bin 'README.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE.txt') -Destination (Join-Path $bin 'LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $bin 'THIRD-PARTY-NOTICES.md') -Force
$payload = Join-Path $build 'payload.zip'
if (Test-Path -LiteralPath $payload) { Remove-Item -LiteralPath $payload }
Compress-Archive -LiteralPath @((Join-Path $bin 'MacFlow.exe'),(Join-Path $bin 'MacFlow.exe.config'),$iconPath,(Join-Path $bin 'README.txt'),(Join-Path $bin 'LICENSE.txt'),(Join-Path $bin 'THIRD-PARTY-NOTICES.md')) -DestinationPath $payload
$shared = @('Models.cs','CompositorGlass.cs','Native.cs','Services.cs','Startup.cs','TaskbarSurfaces.cs','Design.cs','Motion.cs','SystemSymbols.cs','FeatureIcons.cs','IconThemes.cs','PanelMotion.cs','GlassPiece.cs','LiquidMaterial.cs','CrystalLight.cs','WallpaperLibrary.cs','SystemFeatures.cs','UpdateService.cs','AssemblyInfo.cs','WindowsMenuKey.cs') | ForEach-Object { Join-Path $root ('MacFlow\' + $_) }
$versionSource=[System.IO.File]::ReadAllText((Join-Path $root 'MacFlow/AssemblyInfo.cs'))
$versionMatch=[regex]::Match($versionSource,'AssemblyFileVersion\("(\d+\.\d+\.\d+)\.0"\)')
if(!$versionMatch.Success){throw 'La versión del producto no es válida.'}
$packageVersion=$versionMatch.Groups[1].Value
$setup = Join-Path $deliverables ('MacFlow-Setup-'+$packageVersion+'.exe')
& $csc @common ('/resource:' + (Join-Path $build 'LiquidGlass.ps') + ',MacFlow.LiquidGlass.ps') ('/resource:' + (Join-Path $build 'CrystalLight.ps') + ',MacFlow.CrystalLight.ps') ('/out:' + $setup) '/main:MacFlowSetup.Program' ('/resource:' + $payload + ',MacFlow.Payload.zip') @shared (Join-Path $root 'Installer\Setup.cs') (Join-Path $root 'Installer\UpdatePresentation.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del instalador.' }
Copy-Item -LiteralPath (Join-Path $root 'MacFlow\App.config') -Destination ($setup + '.config') -Force
$packageInfo=Get-Item -LiteralPath $setup
$packageJson=@{Product='MacFlow';Architecture='x64';Version=$packageVersion;Installer=$packageInfo.Name;Sha256=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash;Size=$packageInfo.Length;FeedUrl=$UpdateFeedUrl;Notes='26.1.2: presentacion completa con fondo, Hola transparente sin tinte celeste y avance manual. Cristal recortado sin placa ni borde extra. Nuevo simbolo Wi-Fi. Paneles hacia el Dock y cierre de programas desde MacFlow con transicion propia cuando Windows permite obtener la vista; respeta guardar o cancelar.'} | ConvertTo-Json
[System.IO.File]::WriteAllText((Join-Path $deliverables 'MacFlow-update.json'),$packageJson,[System.Text.UTF8Encoding]::new($false))
if ($RunTests) {
    $tests = Join-Path $deliverables 'verification'
    New-Item -ItemType Directory -Force -Path $tests | Out-Null
    $app = Start-Process -FilePath (Join-Path $bin 'MacFlow.exe') -ArgumentList @('--self-test',('"' + $tests + '"')) -PassThru -WindowStyle Hidden
    $app.WaitForExit()
    if ($app.ExitCode -ne 0) { Get-Content -LiteralPath (Join-Path $tests 'self-tests.txt') -Tail 12; throw 'Fallaron los tests de aplicación. Ver verification/self-tests.txt.' }
    $installer = Start-Process -FilePath $setup -ArgumentList @('--setup-test',('"' + (Join-Path $tests 'installer') + '"')) -PassThru -WindowStyle Hidden
    $installer.WaitForExit()
    if ($installer.ExitCode -ne 0) { Get-Content -LiteralPath (Join-Path $tests 'installer/installer-tests.txt') -Tail 12; throw 'Fallaron los tests del instalador. Ver verification/installer/installer-tests.txt.' }
}
Get-Item -LiteralPath $setup,(Join-Path $bin 'MacFlow.exe') | Select-Object FullName,Length
