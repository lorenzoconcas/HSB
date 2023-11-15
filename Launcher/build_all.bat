#linux
dotnet publish --self-contained -r linux-arm64 -c release -o ./build/linux_arm64 -p:PublishSingleFile=true
dotnet publish --self-contained -r linux-x64 -c release -o ./build/linux_x64 -p:PublishSingleFile=true
#macOS
dotnet publish --self-contained -r osx-arm64 -c release -o ./build/apple_silicon -p:PublishSingleFile=true
dotnet publish --self-contained -r osx-x64 -c release -o ./build/apple_intel -p:PublishSingleFile=true
#windows
dotnet publish --self-contained -r win-arm64 -c release -o ./build/win_arm64 -p:PublishSingleFile=true
dotnet publish --self-contained -r win-x64 -c release -o ./build/win_x64 -p:PublishSingleFile=true
dotnet publish --self-contained -r win-xx86 -c release -o ./build/win_x86 -p:PublishSingleFile=true
