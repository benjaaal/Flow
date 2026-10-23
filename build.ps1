param([switch]$RunTests,[switch]$CoreBridge,[switch]$CoreOnly,[string]$OutputDirectory,[string]$UpdateFeedUrl,[switch]$Offline,[string]$DependencyCacheRoot,[string]$NativeRuntimeDirectory)
$ErrorActionPreference = 'Stop'
if($CoreBridge){throw 'La publicación 27.0.0 requiere el paquete completo.'}
$ProgressPreference = 'SilentlyContinue'
if($CoreOnly -and ($RunTests -or $CoreBridge)){throw 'CoreOnly compila solo el núcleo; ejecutá sus pruebas por separado.'}
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
$references = @('System.dll','System.Security.dll','System.Core.dll','System.Numerics.dll','System.Management.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Runtime.Serialization.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','Microsoft.CSharp.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','WPF\System.Speech.dll','WPF\UIAutomationClient.dll','WPF\UIAutomationTypes.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$common = @('/nologo','/target:winexe','/platform:x64','/optimize+','/langversion:5','/utf8output',('/resource:' + (Join-Path $root 'MacFlow\Theme.xaml') + ',MacFlow.Theme.xaml'),('/win32manifest:' + (Join-Path $root 'MacFlow\app.manifest')),('/win32icon:' + $iconPath)) + $references
$common+=('/resource:'+$channelFile+',MacFlow.Channel.json')
$radioRuntime = Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Runtime') -Filter System.Runtime.dll -Recurse | Select-Object -First 1
if (!$radioRuntime) { throw 'No se encontró la proyección WinRT de .NET Framework.' }
$common += @(('/reference:'+$radioRuntime.FullName),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Devices.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Foundation.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Media.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Storage.winmd')))
$common += @(('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.UI.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.Graphics.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.System.winmd')),('/reference:'+(Join-Path $env:WINDIR 'System32/WinMetadata/Windows.ApplicationModel.winmd'))); $vectorRuntime=Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Numerics.Vectors') -Filter System.Numerics.Vectors.dll -Recurse | Select-Object -First 1; $common += ('/reference:'+$vectorRuntime.FullName)
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
& $shaderCompiler (Join-Path $root 'MacFlow/AssistantLight.hlsl') (Join-Path $build 'AssistantLight.ps')
if ($LASTEXITCODE -ne 0) { throw 'Falló la luz del asistente.' }
& $shaderCompiler (Join-Path $root 'MacFlow/LockGlass.hlsl') (Join-Path $build 'LockGlass.ps')
if ($LASTEXITCODE -ne 0) { throw 'Falló el shader del reloj.' }
& $shaderCompiler (Join-Path $root 'MacFlow/WelcomeApple.hlsl') (Join-Path $build 'WelcomeApple.ps')
if ($LASTEXITCODE -ne 0) { throw 'Fallo el shader del saludo.' }
$assemblySource=Join-Path $root 'MacFlow/AssemblyInfo.cs'
if($CoreBridge){
    $assemblySource=Join-Path $build 'CoreAssemblyInfo.cs'
    [IO.File]::WriteAllText($assemblySource,([IO.File]::ReadAllText((Join-Path $root 'MacFlow/AssemblyInfo.cs')) -replace '26\.5\.1\.0','26.5.0.0'))
    $common+='/define:CORE_BRIDGE'
}
$appSources = @(Get-ChildItem -LiteralPath (Join-Path $root 'MacFlow') -Filter '*.cs' | Where-Object Name -ne 'AssemblyInfo.cs' | ForEach-Object FullName)+@($assemblySource)
$wallpaperResources=Get-ChildItem -LiteralPath (Join-Path $root 'MacFlow/Assets/Wallpapers') -File | Where-Object Extension -In '.png','.jpg' | ForEach-Object { '/resource:'+$_.FullName+',MacFlow.Wallpaper.'+$_.Name }
& $csc @common @wallpaperResources ('/resource:' + (Join-Path $build 'AssistantLight.ps') + ',MacFlow.AssistantLight.ps') ('/resource:' + (Join-Path $build 'LockGlass.ps') + ',MacFlow.LockGlass.ps') ('/resource:' + (Join-Path $build 'WelcomeApple.ps') + ',MacFlow.WelcomeApple.ps') ('/resource:' + (Join-Path $build 'LiquidGlass.ps') + ',MacFlow.LiquidGlass.ps') ('/resource:' + (Join-Path $build 'LidFold.ps') + ',MacFlow.LidFold.ps') ('/resource:' + (Join-Path $build 'CrystalLight.ps') + ',MacFlow.CrystalLight.ps') ('/out:' + (Join-Path $bin 'MacFlow.exe')) '/main:MacFlow.Program' @appSources
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de MacFlow.' }
Copy-Item -LiteralPath (Join-Path $root 'MacFlow\App.config') -Destination (Join-Path $bin 'MacFlow.exe.config') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.txt') -Destination (Join-Path $bin 'README.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE.txt') -Destination (Join-Path $bin 'LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $bin 'THIRD-PARTY-NOTICES.md') -Force
if($CoreOnly){Get-Item -LiteralPath (Join-Path $bin 'MacFlow.exe') | Select-Object FullName,Length;return}
if($NativeRuntimeDirectory){
    $runtime=[IO.Path]::GetFullPath($NativeRuntimeDirectory)
    $previousSource=Join-Path (Split-Path $runtime -Parent) 'source/BuildTools'
    foreach($worker in @('FaceWorker.cs','VoiceWorker.cs')){
        $original=Join-Path $previousSource $worker
        if(!(Test-Path -LiteralPath $original) -or (Get-FileHash -LiteralPath $original).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root ('BuildTools/'+$worker))).Hash){throw 'Los workers cambiaron: recompilar los motores nativos con la cache verificada.'}
    }
    $required=@('MacFlow.Face.exe','MacFlow.Voice.exe','OpenCvSharpExtern.dll','OpenCvSharp.dll','Vosk.dll','NAudio.dll','libvosk.dll','voice-en.zip','voice-es.zip','yunet.onnx','sface.onnx','antispoof.onnx')
    foreach($name in $required){if(!(Test-Path -LiteralPath (Join-Path $runtime $name))){throw ('Runtime incompleto: '+$name)}}
    $models=@{ 'yunet.onnx'='8F2383E4DD3CFBB4553EA8718107FC0423210DC964F9F4280604804ED2552FA4'; 'sface.onnx'='0BA9FBFA01B5270C96627C4EF784DA859931E02F04419C829E83484087C34E79'; 'antispoof.onnx'='D7B3CD9BA8A7CEB13BAA8C4720902E27CA3112EFF52F926C08804AF6B6EECC7B'; 'voice-en.zip'='30F26242C4EB449F948E42CB302DD7A686CB29A3423A8367F99FF41780942498'; 'voice-es.zip'='09B239888F633EF2F0B4E09736E3D9936ACFD810BC65D53FAD45261762C6511F' }
    foreach($name in $models.Keys){if((Get-FileHash -LiteralPath (Join-Path $runtime $name)).Hash -ne $models[$name]){throw ('Integridad incorrecta: '+$name)}}
    $core=@('MacFlow.exe','MacFlow.exe.config','MacFlow.ico','README.txt','LICENSE.txt','THIRD-PARTY-NOTICES.md','NATIVE-THIRD-PARTY.txt')
    $records=@()
    foreach($file in (Get-ChildItem -LiteralPath $runtime -File | Where-Object Name -NotIn $core)){
        $target=Join-Path $bin $file.Name;Copy-Item -LiteralPath $file.FullName -Destination $target -Force
        $hash=(Get-FileHash -LiteralPath $file.FullName).Hash
        if((Get-FileHash -LiteralPath $target).Hash -ne $hash){throw ('Copia incorrecta: '+$file.Name)}
        $records+=@{File=$file.Name;SHA256=$hash;Bytes=$file.Length}
    }
    [IO.File]::WriteAllText((Join-Path $deliverables 'native-runtime-integrity.json'),($records|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
}elseif(!$CoreBridge){
    & (Join-Path $root 'BuildTools/BuildFace.ps1') -Bin $bin -Compiler $csc -Offline:$Offline -CacheRoot $DependencyCacheRoot
    & (Join-Path $root 'BuildTools/BuildVoice.ps1') -Bin $bin -Compiler $csc -Offline:$Offline -CacheRoot $DependencyCacheRoot
}
Copy-Item -LiteralPath (Join-Path $root 'NATIVE-THIRD-PARTY.txt') -Destination $bin -Force
$payload = Join-Path $build 'payload.zip'
if (Test-Path -LiteralPath $payload) { Remove-Item -LiteralPath $payload }
$coreFiles=@('MacFlow.exe','MacFlow.exe.config','MacFlow.ico','README.txt','LICENSE.txt','THIRD-PARTY-NOTICES.md','NATIVE-THIRD-PARTY.txt')
$payloadFiles=@(Get-ChildItem -LiteralPath $bin -File | Where-Object { !$CoreBridge -or $_.Name -in $coreFiles } | ForEach-Object FullName)
Compress-Archive -LiteralPath $payloadFiles -DestinationPath $payload
$shared = @('Models.cs','WindowLife.cs','CompositorGlass.cs','NativeBlurEffect.cs','Native.cs','Services.cs','Startup.cs','TaskbarSurfaces.cs','Design.cs','Motion.cs','DisplaySpace.cs','GenieTransition.cs','SystemSymbols.cs','FeatureIcons.cs','IconThemes.cs','PanelMotion.cs','GlassPiece.cs','LiquidMaterial.cs','CrystalLight.cs','WallpaperLibrary.cs','SystemFeatures.cs','UpdateService.cs','AssemblyInfo.cs','WindowsMenuKey.cs') | ForEach-Object { Join-Path $root ('MacFlow\' + $_) }
$shared=@($shared | Where-Object { [IO.Path]::GetFileName($_) -ne 'AssemblyInfo.cs' })+@($assemblySource)
$versionSource=[System.IO.File]::ReadAllText($assemblySource)
$versionMatch=[regex]::Match($versionSource,'AssemblyFileVersion\("(\d+\.\d+\.\d+)\.0"\)')
if(!$versionMatch.Success){throw 'La versión del producto no es válida.'}
$packageVersion=$versionMatch.Groups[1].Value
$setup = Join-Path $deliverables ('MacFlow-Setup-'+$packageVersion+'.exe')
& $csc @common ('/resource:' + (Join-Path $build 'LiquidGlass.ps') + ',MacFlow.LiquidGlass.ps') ('/resource:' + (Join-Path $build 'CrystalLight.ps') + ',MacFlow.CrystalLight.ps') ('/out:' + $setup) '/main:MacFlowSetup.Program' ('/resource:' + $payload + ',MacFlow.Payload.zip') @shared (Join-Path $root 'Installer\Setup.cs') (Join-Path $root 'Installer\UpdatePresentation.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del instalador.' }
Copy-Item -LiteralPath (Join-Path $root 'MacFlow\App.config') -Destination ($setup + '.config') -Force
$packageInfo=Get-Item -LiteralPath $setup
if($CoreBridge -and $packageInfo.Length -gt 64MB){throw 'La actualización base supera el límite de 26.4.1.'}
$packageJson=@{Product='MacFlow';Architecture='x64';Version=$packageVersion;Installer=$packageInfo.Name;Sha256=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash;Size=$packageInfo.Length;FeedUrl=$UpdateFeedUrl;Notes='27.2.2: isla dinámica flotante, física compartida, ventanas proporcionales, barra y Dock adaptativos, interfaces y Hola renovados.'} | ConvertTo-Json
[System.IO.File]::WriteAllText((Join-Path $deliverables 'MacFlow-update.json'),$packageJson,[System.Text.UTF8Encoding]::new($false))
if ($RunTests) {
    if(!$CoreBridge){ & (Join-Path $root 'BuildTools/TestNativeModels.ps1') -Bin $bin -Output (Join-Path $deliverables 'verification') }
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
