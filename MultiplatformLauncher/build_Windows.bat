mkdir .\build
dotnet publish --self-contained -r win-x64 -c release -o ./build/ -p:PublishSingleFile=true   
