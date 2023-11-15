#!/usr/bin/bash

mkdir ./build/ 2> /dev/null
dotnet publish --self-contained -r osx-arm64 -c release -o ./build/ -p:PublishSingleFile=true
