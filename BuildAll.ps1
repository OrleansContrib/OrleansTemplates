$version = "1.1.0"
$orleansVersion = "1.0.8"

msbuild.exe src\ETG.Orleans.sln /t:Clean /p:Configuration=Release
msbuild.exe src\ETG.Orleans.sln /t:Build /p:Configuration=Release

msbuild.exe test\ETG.Orleans.CodeGenTest.sln /t:Clean /p:Configuration=Release
msbuild.exe test\ETG.Orleans.CodeGenTest.sln /t:Build /p:Configuration=Release

#cd test\
#mstest /testcontainer:ETG.Orleans.CodeGenTest\bin\Release\ETG.Orleans.CodeGenTest.dll
#cd ..

cd Nuget
.\NugetPackAll.ps1 $version $orleansVersion
cd ..

msbuild.exe VSTemplates\ETG.Orleans.Templates.VSIX\ETG.Orleans.Templates.VSIX.csproj /t:Clean /p:Configuration=Release
msbuild.exe VSTemplates\ETG.Orleans.Templates.VSIX\ETG.Orleans.Templates.VSIX.csproj /t:Build /p:Configuration=Release

Copy-Item VSTemplates\ETG.Orleans.Templates.VSIX\bin\Release\ETG.Orleans.Templates.VSIX.vsix .
