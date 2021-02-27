# clear old release
Remove-Item "ReleaseArtifacts" -Recurse -ErrorAction SilentlyContinue
Remove-Item "*\bin\Release\" -Recurse -ErrorAction SilentlyContinue
Remove-Item "BurntSushi\bin\Release\" -Recurse -ErrorAction SilentlyContinue

# build all but installer
dotnet publish --configuration Release

# build installer
$msbuildPath = & "$(${Env:ProgramFiles(x86)})\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
& $msbuildPath -target:build Installer /property:Configuration=Release

# copy to artifacts folder
New-Item -Path "ReleaseArtifacts" -ItemType "directory"
New-Item -Path "ReleaseArtifacts\.gitignore" -ItemType "file" -Value "*.*"
Copy-Item -Path "Installer\bin\Release\*.msi" -Destination "ReleaseArtifacts"
Remove-Item -Path "BurntSushi\bin\Release\net4.8\publish\*.pdb"
Compress-Archive -Path "BurntSushi\bin\Release\net4.8\publish\*.*" -DestinationPath "ReleaseArtifacts\BurntSushiPortable.zip"

