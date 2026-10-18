param([string]$Bin,[string]$Compiler)
$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot '../build/face-deps'
New-Item -ItemType Directory -Force $cache | Out-Null
$specs=@(
@('core','opencvsharp4','4.13.0.20260627','8ACEE778364E5EEE6495D923732CACD8D895C7F683D2144F622B54418623D12C'),
@('runtime','opencvsharp4.runtime.win','4.13.0.20260627','907FC3F682B1D430EF47F6782FB2C78A8337AC9BED2E5E7F8F01B9585C42C830'),
@('memory','system.memory','4.6.3','26078AEB758C9AE985E8BF851F973026061DA6A5EB4837204D0C2D2204C72955'),
@('unsafe','system.runtime.compilerservices.unsafe','6.1.2','5F6A7F53AF3465F92BEB6DA873EBE0E496206C313313B98BADEE4355A6B25937'),
@('buffers','system.buffers','4.6.1','B00451E91D016FBEC091AD1E361F3A7015E1D91D4047F7E48A74455B2A673D79'),
@('vectors','system.numerics.vectors','4.6.1','2BC500A86DCB02F2032D6D877F9E2D6E9E4A79080E57239B4198679D4031F2C7'))
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
foreach($spec in $specs){$zip=Join-Path $cache ($spec[0]+'.zip');if(!(Test-Path $zip)){Invoke-WebRequest -UseBasicParsing ('https://api.nuget.org/v3-flatcontainer/'+$spec[1]+'/'+$spec[2]+'/'+$spec[1]+'.'+$spec[2]+'.nupkg') -OutFile $zip};if((Get-FileHash $zip -Algorithm SHA256).Hash -ne $spec[3]){throw ('Integridad incorrecta: '+$spec[0])};$dest=Join-Path $cache $spec[0];if(!(Test-Path $dest)){Expand-Archive -LiteralPath $zip -DestinationPath $dest}}
foreach($spec in @(@('yunet','face_detection_yunet','face_detection_yunet_2023mar.onnx','8F2383E4DD3CFBB4553EA8718107FC0423210DC964F9F4280604804ED2552FA4'),@('sface','face_recognition_sface','face_recognition_sface_2021dec.onnx','0BA9FBFA01B5270C96627C4EF784DA859931E02F04419C829E83484087C34E79'))){$model=Join-Path $cache ($spec[0]+'.onnx');if(!(Test-Path $model)){Invoke-WebRequest -UseBasicParsing ('https://media.githubusercontent.com/media/opencv/opencv_zoo/main/models/'+$spec[1]+'/'+$spec[2]) -OutFile $model};if((Get-FileHash $model -Algorithm SHA256).Hash -ne $spec[3]){throw 'Integridad incorrecta del modelo facial'};Copy-Item -LiteralPath $model -Destination $Bin -Force}
$anti=Join-Path $cache 'antispoof.onnx'
if(!(Test-Path $anti)){Invoke-WebRequest -UseBasicParsing 'https://huggingface.co/garciafido/minifasnet-v2-anti-spoofing-onnx/resolve/d29c87568ca9b5662da803b10f217c4db20b142b/minifasnet_v2.onnx' -OutFile $anti}
if((Get-FileHash $anti -Algorithm SHA256).Hash -ne 'D7B3CD9BA8A7CEB13BAA8C4720902E27CA3112EFF52F926C08804AF6B6EECC7B'){throw 'Integridad incorrecta del modelo de presencia'}
Copy-Item -LiteralPath $anti -Destination $Bin -Force
$files=@('core/lib/netstandard2.0/OpenCvSharp.dll','runtime/runtimes/win-x64/native/OpenCvSharpExtern.dll','memory/lib/net462/System.Memory.dll','unsafe/lib/net462/System.Runtime.CompilerServices.Unsafe.dll','buffers/lib/net462/System.Buffers.dll','vectors/lib/net462/System.Numerics.Vectors.dll')
foreach($file in $files){Copy-Item -LiteralPath (Join-Path $cache $file) -Destination $Bin -Force}
$refs=@(('/reference:'+(Join-Path $Bin 'OpenCvSharp.dll')),('/reference:'+(Join-Path $Bin 'System.Memory.dll')),('/reference:'+(Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/netstandard') -Filter netstandard.dll -Recurse | Select-Object -First 1 -ExpandProperty FullName)))
& $Compiler /nologo /target:exe /platform:x64 /optimize+ /langversion:5 @refs ('/out:'+(Join-Path $Bin 'MacFlow.Face.exe')) (Join-Path $PSScriptRoot 'FaceWorker.cs')
if($LASTEXITCODE -ne 0){throw 'No se pudo compilar el motor facial'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../MacFlow/App.config') -Destination (Join-Path $Bin 'MacFlow.Face.exe.config') -Force
