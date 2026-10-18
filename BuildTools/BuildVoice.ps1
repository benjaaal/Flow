param([string]$Bin,[string]$Compiler)
$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot '../build/voice-deps'
New-Item -ItemType Directory -Force $cache | Out-Null
$specs=@(@('voice-en','https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip','30F26242C4EB449F948E42CB302DD7A686CB29A3423A8367F99FF41780942498'),@('vosk','https://api.nuget.org/v3-flatcontainer/vosk/0.3.38/vosk.0.3.38.nupkg','D01F96E306F3B6ABD68BD0EEF6D8AACB15C870B7873A74074C775B13DD50397F'),@('naudio','https://api.nuget.org/v3-flatcontainer/naudio/1.10.0/naudio.1.10.0.nupkg','E4A80EED41DAD794695F9C114432DE4667D20BDFA80825AA94D159708CF29217'),@('voice-es','https://alphacephei.com/vosk/models/vosk-model-small-es-0.42.zip','09B239888F633EF2F0B4E09736E3D9936ACFD810BC65D53FAD45261762C6511F'))
foreach($spec in $specs){$zip=Join-Path $cache ($spec[0]+'.zip');if(!(Test-Path $zip)){Invoke-WebRequest -UseBasicParsing $spec[1] -OutFile $zip};if((Get-FileHash $zip -Algorithm SHA256).Hash -ne $spec[2]){throw 'Integridad incorrecta del reconocimiento local'};if($spec[0] -like 'voice-*'){Copy-Item -LiteralPath $zip -Destination $Bin -Force}else{$folder=Join-Path $cache $spec[0];if(!(Test-Path $folder)){Expand-Archive -LiteralPath $zip -DestinationPath $folder}}}
Copy-Item -LiteralPath (Join-Path $cache 'vosk/lib/netstandard2.0/Vosk.dll') -Destination $Bin -Force
Copy-Item -LiteralPath (Join-Path $cache 'naudio/lib/net35/NAudio.dll') -Destination $Bin -Force
Get-ChildItem -LiteralPath (Join-Path $cache 'vosk/build/lib/win-x64') -Filter '*.dll' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $Bin -Force}
$facade=Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/netstandard') -Filter netstandard.dll -Recurse | Select-Object -First 1 -ExpandProperty FullName
$refs=@(('/reference:'+(Join-Path $Bin 'Vosk.dll')),('/reference:'+(Join-Path $Bin 'NAudio.dll')),('/reference:'+$facade),'/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll','/reference:System.Web.Extensions.dll')
& $Compiler /nologo /target:exe /platform:x64 /optimize+ /langversion:5 @refs ('/out:'+(Join-Path $Bin 'MacFlow.Voice.exe')) (Join-Path $PSScriptRoot 'VoiceWorker.cs')
if($LASTEXITCODE -ne 0){throw 'No se pudo compilar el reconocimiento de voz'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../MacFlow/App.config') -Destination (Join-Path $Bin 'MacFlow.Voice.exe.config') -Force
