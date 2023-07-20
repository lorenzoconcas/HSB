#!/usr/bin/bash

mkdir ./build/ 2> /dev/null

if [ "$(uname)" == "Darwin" ]; then
   dotnet publish --self-contained -r osx-x64 -c release -o ./build/ -p:PublishSingleFile=true     
elif [ "$(expr substr $(uname -s) 1 5)" == "Linux" ]; then
    echo "Sorry this is for Apple macOS, run the 'build_linux.sh' script instead";   
fi
