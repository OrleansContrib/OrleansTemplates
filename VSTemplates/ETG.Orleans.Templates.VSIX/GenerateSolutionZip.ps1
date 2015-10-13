# When the solution is compiled, a zip file is created for each project template. This script will 
# - extract all the zips inside a temporary folder
# - copy the solution .vstemplate and put it at the root of that folder
# - create a zip of the resulting folder and put it in the ProjectTemplates folder of the VSIX project.
# The resulting solution template zip will be embedded inside the VSIX.
 
# This script is called after all individual project templates have been generated but before the VSIX is compiled. In fact, this script is called as a PreBuildEvent
# of the VSIX project and we rely on the fact that the dependencies (individual project templates) will be built before the PreBuildEvent.

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



