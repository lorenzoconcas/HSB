#!/usr/bin/bash

mkdir ./build/ 2> /dev/null
dotnet publish --self-contained -r osx-x64 -c release -o ./build/ -p:PublishSingleFile=true     

