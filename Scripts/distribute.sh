#!/bin/bash
echo "HSB Distribution Script"

#if first arg is -h or --help, print help
if [ "$1" == "-h" ] || [ "$1" == "--help" ]; then
  echo "Usage: ./distribute.sh -v versionName -o otherValues"
  echo "versionName: The version name of the build. If not passed, the version name will be detected from ./HSB/Properties/AssemblyInfo.cs"
  echo "otherValues: Other values to be appended to the zip file name. If not passed, the zip file name will be HSB_v{versionName}_DEBUG.zip and HSB_v{versionName}_RELEASE.zip"
  exit 0
fi

versionName=""
otherValues=""

#check if version name is passed, if not detect from ./HSB/Properties/AssemblyInfo.cs
#loop arguments and check if the current argument is -v
#then the next argument is the version name
for var in "$@"
do
  if [ "$var" == "-v" ]; then
    versionName=$2
  fi
done
#if version name is not passed, detect from ./HSB/Properties/AssemblyInfo.cs
if [ "$versionName" == "" ]; then
  echo "Version name not passed. Using version name from ./HSB/Properties/AssemblyInfo.cs"
  #check if AssemblyInfo.cs exists
  if [ ! -f "../HSB/Properties/AssemblyInfo.cs" ]; then
    echo "AssemblyInfo.cs not found. Make sure that the HSB source is the root path of the folder this script is in"
    exit 1
  fi
  #get version name from AssemblyInfo.cs
  versionName=$(grep -oE '\[assembly: AssemblyVersion\(\"(\d+.\d+.\d+)' ../HSB/Properties/AssemblyInfo.cs)
  #cut after [assembly: AssemblyVersion(" 
  versionName=${versionName:28}
fi


#check if there is an argument with value -o
#if is present then "otherValues" is the next argument
for var in "$@"
do
  if [ "$var" == "-o" ]; then
    otherValues=$2
  fi
done

#if otherValues != "" then prepend _ to it
if [ "$otherValues" != "" ]; then
  otherValues="_$otherValues"
fi

#check if dotnet is installed
if ! [ -x "$(command -v dotnet)" ]; then
  echo 'Error: dotnet is not installed.' >&2
  exit 2
fi

#move to the folder this script is in
cd "$(dirname "$0")" #this is to fix run from Apple Shortcuts or when you cannot set the working directory

#check if HSB source exists (../HSB) relative to the directory this script is in

if [ ! -d "../HSB" ]; then
  echo "HSB source not found. Make sure that the HSB source is the root path of the folder this script is in"
  exit 3
fi

#check if Releases directory exists (../Releases/HSB), if not create it
if [ ! -d "../Releases/HSB" ]; then
  echo "Releases directory not found. Creating Releases directory"
  mkdir ../Releases
  mkdir ../Releases/HSB
fi

#clear zip files
echo "Clearing previous archives files"
rm -f ../Releases/HSB/*.zip 2> /dev/null
######DEBUG MODE BUILD######

#compile HSB in debug mode
echo "Compiling HSB in debug mode"
dotnet publish ../HSB/HSB.csproj -c Debug -o ../Releases/HSB/Debug

#archive HSB in debug mode to a zip file called HSB_v{versionName}{otherValues}_DEBUG.zip in the ./Releases directory
echo "Archiving HSB in debug mode"
cd ../Releases/HSB/Debug
zip -r HSB_v${versionName}${otherValues}_DEBUG.zip *
mv HSB_v${versionName}${otherValues}_DEBUG.zip ../../HSB_v${versionName}${otherValues}_DEBUG.zip

#delete build folder
echo "Deleting build folder"
cd ..
rm -rf Debug
cd ..

#print current directory
echo "Current directory: "
pwd

######RELEASE MODE BUILD######

#compile HSB in release mode
echo "Compiling HSB in release mode"
dotnet publish ../HSB/HSB.csproj -c Release -o ../Releases/HSB/Release

#archive HSB in release mode to a zip file called HSB_v{versionName}{otherValues}_RELEASE.zip in the ./Releases directory
#but delete the archive first if it already exists
echo "Archiving HSB in release mode"
cd ../Releases/HSB/Release
zip -r HSB_v${versionName}${otherValues}_RELEASE.zip *
mv HSB_v${versionName}${otherValues}_RELEASE.zip ../../HSB_v${versionName}${otherValues}_RELEASE.zip


#delete build folder
echo "Deleting build folder"
cd ..
rm -rf Release


#clear Releases/HSB directory
echo "Clearing Releases/HSB directory"
cd ..
rm -rf HSB
#exit
echo "Done"
exit 0
