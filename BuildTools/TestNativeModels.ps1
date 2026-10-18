param([string]$Bin,[string]$Output)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force $Output | Out-Null
$cache=Join-Path $PSScriptRoot '../build/model-tests'
New-Item -ItemType Directory -Force $cache | Out-Null
$checks=@(@('test-face.jpg','https://raw.githubusercontent.com/opencv/opencv/master/samples/data/lena.jpg','7DE7ED51A1594FFF247F4CAE2301ECEACF5313D6011E37B4A4C8733F7BB72C07'),@('image_T1.jpg','https://raw.githubusercontent.com/minivision-ai/Silent-Face-Anti-Spoofing/master/images/sample/image_T1.jpg','F4455149F488F76205FDEE5499EC5261D08EF6279A1CFF7B778EA85405331E94'),@('image_F1.jpg','https://raw.githubusercontent.com/minivision-ai/Silent-Face-Anti-Spoofing/master/images/sample/image_F1.jpg','4B11B5D7A8A8E4A88F5F16A5426A0A7692E39E5BB45BB03B4EBE5E1606336860'),@('image_F2.jpg','https://raw.githubusercontent.com/minivision-ai/Silent-Face-Anti-Spoofing/master/images/sample/image_F2.jpg','FBBEA73450AE9D9BB555C8CCAC77BF39D234261FE3BE4190E3ED2999690C485F'))
foreach($check in $checks){$file=Join-Path $cache $check[0];if(!(Test-Path $file)){Invoke-WebRequest -UseBasicParsing $check[1] -OutFile $file};if((Get-FileHash $file -Algorithm SHA256).Hash -ne $check[2]){throw 'Imagen de prueba alterada'}}
$report=@()
$result=& (Join-Path $Bin 'MacFlow.Face.exe') --self-test (Join-Path $cache 'test-face.jpg')
if($LASTEXITCODE -ne 0 -or $result -notmatch '^PASS'){throw ('Falló reconocimiento de modelo: '+$result)}
$report+=$result
foreach($name in @('image_T1.jpg','image_F1.jpg','image_F2.jpg')){$result=& (Join-Path $Bin 'MacFlow.Face.exe') --spoof-test (Join-Path $cache $name);if($LASTEXITCODE -ne 0 -or $result -notmatch 'LIVE=([0-9.]+)'){throw ('No se pudo probar presencia: '+$result)};$score=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);if(($name -eq 'image_T1.jpg' -and $score -lt .95) -or ($name -ne 'image_T1.jpg' -and $score -ge .95)){throw ('Clasificación incorrecta: '+$name+' '+$score)};$report+=('PASS presencia '+$name+' '+$score.ToString('F5',[Globalization.CultureInfo]::InvariantCulture))}
$wav=Join-Path $cache 'hey-siri.wav';[IO.File]::WriteAllBytes($wav,[Convert]::FromBase64String([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'TestVoice.wav.base64'))))
$result=& (Join-Path $Bin 'MacFlow.Voice.exe') $cache $wav wake
if($LASTEXITCODE -ne 0 -or !($result -contains 'MATCH:True')){throw ('El audio de Hey Siri no activa el modelo: '+($result -join ' '))}
$report+='PASS audio sintético Hey Siri reconocido por el modelo local, sin micrófono ni SAPI'
Add-Type -Path (Join-Path $Bin 'NAudio.dll')
$silence=Join-Path $cache 'silence.wav';$writer=[NAudio.Wave.WaveFileWriter]::new($silence,[NAudio.Wave.WaveFormat]::new(16000,16,1));try{$bytes=New-Object byte[] 64000;$writer.Write($bytes,0,$bytes.Length)}finally{$writer.Dispose()}
$result=& (Join-Path $Bin 'MacFlow.Voice.exe') $cache $silence wake
if($LASTEXITCODE -ne 0 -or $result -contains 'MATCH:True'){throw 'El silencio activó la frase'}
$report+='PASS silencio no activa la voz'
$negative=Join-Path $cache 'not-wake.wav';[IO.File]::WriteAllBytes($negative,[Convert]::FromBase64String([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'TestVoiceNegative.wav.base64'))))
$result=& (Join-Path $Bin 'MacFlow.Voice.exe') $cache $negative wake
if($LASTEXITCODE -ne 0 -or $result -contains 'MATCH:True'){throw 'Una frase corriente activó la voz'}
$report+='PASS frase corriente no activa Hey Siri'
$report+='INFO Las fotos de prueba son ejemplos públicos del proyecto; no validan ataques en todas las webcams. Cámara, micrófono físico, fotos impresas y pantallas reales requieren validación en el equipo.'
$report | Set-Content -LiteralPath (Join-Path $Output 'native-model-tests.txt')
$report
