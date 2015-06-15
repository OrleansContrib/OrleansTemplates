function ZipFiles( $zipfilepath, $sourcedir )
{
   Add-Type -Assembly System.IO.Compression.FileSystem
   [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcedir, $zipfilepath)
}

$projectTemplates = "ETG.Orleans.Templates.Interfaces", "ETG.Orleans.Templates.Grains", "ETG.Orleans.Templates.Host", "ETG.Orleans.Templates.WebApis"
$currentPath = $args[0]
$solZipDest = $currentPath + "\ETG.Orleans.Templates.VSIX\ProjectTemplates\ETG.Orleans.Solution.zip"

[System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')

$tmpDir = $currentPath + "\.tmp"
if (Test-Path $tmpDir)
{	
	Remove-Item -Recurse -Force $tmpDir
} 
New-Item $tmpDir -type directory

foreach ($projectTemplate in $projectTemplates)
{
	$sourceFile =  $currentPath + "\" + $projectTemplate + "\bin\Release\ProjectTemplates\CSharp\1033\" + $projectTemplate + ".zip"
	$sourceFile
	[System.IO.Compression.ZipFile]::ExtractToDirectory($sourceFile, $tmpDir + "\" + $projectTemplate)
}

Copy-Item ($currentPath + "\ETG.Orleans.Solution\ETG.Orleans.Solution.vstemplate") ($tmpDir + "\ETG.Orleans.Solution.vstemplate")  

$solZipDest
$tmpDir
if (Test-Path $solZipDest)
{
	Remove-Item $solZipDest	
}
ZipFiles $solZipDest $tmpDir

Remove-Item -Recurse -Force $tmpDir 



