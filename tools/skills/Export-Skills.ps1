param([string]$CodexDirectory=$(if($env:CODEX_HOME){$env:CODEX_HOME}else{Join-Path $env:USERPROFILE '.codex'}),[string]$AgentsDirectory=(Join-Path $env:USERPROFILE '.agents'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$manifestPath=Join-Path $PSScriptRoot 'manifest.json'
$previous=if(Test-Path $manifestPath){Get-Content $manifestPath -Raw|ConvertFrom-Json}else{$null}
$items=@()
foreach($location in @(@{kind='codex';root=(Join-Path $CodexDirectory 'skills')},@{kind='agents';root=(Join-Path $AgentsDirectory 'skills')})){
 if(-not(Test-Path $location.root)){continue}
 foreach($directory in Get-ChildItem -LiteralPath $location.root -Directory | Where-Object {$_.Name -ne '.system'}){
  if(-not(Test-Path (Join-Path $directory.FullName 'SKILL.md'))){continue}
  if(@(Get-ChildItem -LiteralPath $directory.FullName -Directory -Recurse -Force|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Refuse linked skill directory'}
  if($directory.Name -notmatch '^[a-z0-9-]+$'){throw 'Invalid skill directory name'}
  $files=@(Get-ChildItem -LiteralPath $directory.FullName -Recurse -File -Force | Sort-Object FullName)
  $records=@()
  foreach($file in $files){
   $relative=$file.FullName.Substring($directory.FullName.Length+1).Replace('\','/')
   if($file.Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Refuse linked file: $relative"}
   if($relative -match '(^|/)(\.env[^/]*|\.git|node_modules|__pycache__|.*\.(pem|key|log))($|/)'){throw "Review private/generated file before export: $relative"}
   $records+=@{path=$relative;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
  }
  $archive="snapshots/$($location.kind)-$($directory.Name).zip"
  $destination=Join-Path $PSScriptRoot $archive
  New-Item -ItemType Directory -Force (Split-Path $destination)|Out-Null
  $temporary="$destination.$([guid]::NewGuid().ToString('N')).tmp"
  [IO.Compression.ZipFile]::CreateFromDirectory($directory.FullName,$temporary)
  Move-Item -LiteralPath $temporary -Destination $destination -Force
  $items+=@{name=$directory.Name;location=$location.kind;archive=$archive;archiveSha256=(Get-FileHash $destination).Hash.ToLowerInvariant();files=$records;source='Installed skill snapshot; original bytes preserved'}
 }
}
$identities=@($items|ForEach-Object {"$($_.location)/$($_.name)"})
if($previous){foreach($old in $previous.skills){if("$($old.location)/$($old.name)" -notin $identities){$items+= $old}}}
$managed=@('computer-use','google-drive','pages','plugin-creator','plugin-management','sites','work-pets','visualize','documents','pdf','presentations','spreadsheets','template-creator')
if($previous -and $previous.managedPlugins){$managed=@($previous.managedPlugins)}
$manifest=@{schemaVersion=1;updatedAt=(Get-Date).ToUniversalTime().ToString('o');skills=$items;cli=@(@{package='skillevaluator';version='1.0.1';command='skillevaluator'},@{package='cli-anything-hub';version='0.4.1';command='cli-hub'});managedPlugins=$managed;systemSkills='Codex-managed .system skills: install/update Codex; never copy cache or credentials'}
$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Output "Recorded $($items.Count) skills with original file and archive SHA-256 hashes."
