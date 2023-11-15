#!/usr/bin/env bash

mkdir ./build/ 2> /dev/null
dotnet publish --self-contained -r linux-arm64 -c release -o ./build/ -p:PublishSingleFile=true   
