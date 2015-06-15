$version = "1.1.0"
$orleansVersion = "1.0.8"

msbuild.exe src\ETG.Orleans.sln /t:Clean /p:Configuration=Release
msbuild.exe src\ETG.Orleans.sln /t:Build /p:Configuration=Release

msbuild.exe test\ETG.Orleans.CodeGenTest.sln /t:Clean /p:Configuration=Release
msbuild.exe test\ETG.Orleans.CodeGenTest.sln /t:Build /p:Configuration=Release

### mstest is failing to resolve dependencies; run tests inside visual studio for now.
#cd test\
#mstest /testcontainer:ETG.Orleans.CodeGenTest\bin\Release\ETG.Orleans.CodeGenTest.dll
#cd ..

# Create Nugets
cd Nuget
.\NugetPackAll.ps1 $version $orleansVersion
cd ..

# Create VSIX
msbuild.exe "VSTemplates\VSTemplates.sln" /t:Clean /p:Configuration=Release
msbuild.exe "VSTemplates\VSTemplates.sln" /t:Build /p:Configuration=Release

Copy-Item "VSTemplates\ETG.Orleans.Templates.VSIX\bin\Release\ETG.Orleans.Templates.VSIX.vsix" .