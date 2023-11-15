#!/bin/bash
echo "This scripts compile *.cs files in the current directory and outputs the result in ./output"
echo "It also copies the launcher and the json files in ./output"
echo "It requires dotnet 6.0 to be installed and in the PATH"
echo "It's used to compile quick, standalone programs that use the HSB library"
echo "To provide a custom HSB.dll, place it in the same directory as this script"
echo "Otherwhise the included one will be used"
echo "Don't forget to include the HSBMain(string[] args) method in one of the files"
echo "It's recommended to use the HSB template to create a new project"
echo "You can pass additional libraries to the compiler by placing them in the same directory as this script"
echo "if these are dotnet libraries, you must pass them to the compiler with the -r: flag"
echo "Compilation will now start if requirements are met"


platform='unknown'
dotnetFolder=''
unamestr=$(uname)

if [[ "$unamestr" == 'Linux' ]]; then
   platform='linux'
   dotnetFolder='/usr/share/dotnet'
elif [[ "$unamestr" == 'Darwin' ]]; then
   platform='macOS'
   dotnetFolder='/usr/local/share/dotnet'
else 
    echo "unknown platform"
    exit 1
fi

echo "System: $platform"

#check if dotnet is installed
if ! command -v dotnet &> /dev/null
then
    echo "dotnet could not be found"
    exit 1
fi

#check if dotnet sdk version is atleast 6.0
dotnetVersion=$(dotnet --version)
echo "Dotnet version: $dotnetVersion"
#grep first number of the version
dotnetVersionFirstNumber=$(echo $dotnetVersion | grep -oE '^[0-9]+')

if [ $dotnetVersionFirstNumber -lt 6 ]
then
    echo "dotnet version is not atleast 6.0"
    exit 1
fi
#get dotnet sdk version
dotnetVersion=$(dotnet --list-sdks | grep -oE '^[0-9]+.[0-9]+.[0-9]+')
#split content of dotnetVersion by ' ' and get last string
dotnetVersion=$(echo $dotnetVersion | awk '{print $NF}')

baseDotnetFolder="${dotnetFolder}/sdk/${dotnetVersion}"
baseDotnetFolder="/usr/local/share/dotnet/shared/Microsoft.NETCore.App/7.0.13";

#collect all *.cs files in the current directory and concatenate them in a single string
files=$(find . -name "*.cs" -type f -print0 | xargs -0 echo)
echo "Source files: $files"
#search for the file that inside contains the HSBMain(string[] args) method, we need is name
mainFile=$(grep -l "HSBMain(string\[\] args)" $files)
echo "MainFile (first found): $mainFile"
#from mainfile we take the name, so remove path and extension
mainFileName=$(basename -- "$mainFile")
mainFileName="${mainFileName%.*}.dll"

#collect all additional arguments just getting all arguments except the first one
additionalArgs="${@:2}"

references=" -r:${baseDotnetFolder}/System.Private.CoreLib.dll"
references="${references} -r:${baseDotnetFolder}/System.Console.dll"
references="${references} -r:${baseDotnetFolder}/System.Runtime.dll"
references="${references} -r:./HSB.dll"
references="${references} -r:${baseDotnetFolder}/System.dll"
#references="${references} -r:${baseDotnetFolder}/System.Text.dll"
references="${references} -r:${baseDotnetFolder}/System.Text.Json.dll"
references="${references} -r:${baseDotnetFolder}/mscorlib.dll"
references="${references} ${additionalArgs}"

echo "Beginning compilation..."
csc -out:${mainFileName} ${files} -t:library  ${references}
#lets move required files to an ouput folder
echo "Compilation finished"
#check if output folder exists
if [ ! -d "./output" ]
then
    mkdir output
fi

cp ./*.dll ./output
cp ./launcher ./output
cp ./*.json ./output

echo "Done, view results in ./output"