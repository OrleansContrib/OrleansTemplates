$version = $args[0]
$orleansVersion = $args[1]

$dest = "..\..\VSTemplates\ETG.Orleans.Templates.VSIX\Packages"
	
cd ETG.Orleans
nuget pack ETG.Orleans.nuspec -Version $version -Properties orleansVersion=$orleansVersion
Move-Item -Path "ETG.Orleans.${version}.nupkg" -Destination $dest -force

cd ../ETG.Orleans.Templates.Interfaces
nuget pack ETG.Orleans.Templates.Interfaces.nuspec -Version $version -Properties orleansVersion=$orleansVersion
Move-Item -Path "ETG.Orleans.Templates.Interfaces.${version}.nupkg" -Destination $dest -force

cd ../ETG.Orleans.Templates.Grains
nuget pack ETG.Orleans.Templates.Grains.nuspec -Version $version -Properties orleansVersion=$orleansVersion
Move-Item -Path "ETG.Orleans.Templates.Grains.${version}.nupkg" -Destination $dest -force

cd..
