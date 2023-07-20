#!/usr/bin/env bash

mkdir ./build/ 2> /dev/null

if [ "$(uname)" == "Darwin" ]; then
     echo "Sorry this is for Linux, run the 'build_macOS_*.sh' scripts instead";   
elif [ "$(expr substr $(uname -s) 1 5)" == "Linux" ]; then
   dotnet publish --self-contained -r linux-arm64 -c release -o ./build/ -p:PublishSingleFile=true   
fi
