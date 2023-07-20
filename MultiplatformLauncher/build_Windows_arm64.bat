mkdir .\build
dotnet publish --self-contained -r win-arm64 -c release -o ./build/ -p:PublishSingleFile=true   
