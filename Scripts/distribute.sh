#!/bin/bash
echo "HSB Distribution Script"
#if no args are passed, print usage (like ./distribute.sh "0.0.10" "ALPHA")
if [ $# -eq 0 ]
  then
    echo "Usage: ./distribute.sh \"versionName\" \"otherValues\""
    echo "Example: ./distribute.sh 0.0.10 ALPHA"
    exit 0
fi

#check if version name is passed
if [ -z "$1" ]
  then
    echo "No version name supplied"
    exit 1
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
######DEBUG MODE BUILD######

#compile HSB in debug mode
echo "Compiling HSB in debug mode"
dotnet publish ../HSB/HSB.csproj -c Debug -o ../Releases/HSB/Debug

#archive HSB in debug mode to a zip file called HSB_v{versionName}_{otherValues}_DEBUG.zip in the ./Releases directory
echo "Archiving HSB in debug mode"
cd ../Releases/HSB/Debug
rm -f HSB_v$1_$2_DEBUG.zip 2> /dev/null
zip -r HSB_v$1_$2_DEBUG.zip *
mv HSB_v$1_$2_DEBUG.zip ../../HSB_v$1_$2_DEBUG.zip

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

#archive HSB in release mode to a zip file called HSB_v{versionName}_{otherValues}_RELEASE.zip in the ./Releases directory
#but delete the archive first if it already exists
echo "Archiving HSB in release mode"
cd ../Releases/HSB/Release
rm -f HSB_v$1_$2_RELEASE.zip 2> /dev/null
zip -r HSB_v$1_$2_RELEASE.zip *
mv HSB_v$1_$2_RELEASE.zip ../../HSB_v$1_$2_RELEASE.zip


#delete build folder
echo "Deleting build folder"
cd ..
rm -rf Release

#copy HSB_v{versionName}_{otherValues}_DEBUG.zip and HSB_v{versionName}_{otherValues}_RELEASE.zip to ../Releases
echo "Copying HSB_v$1_$2_DEBUG.zip and HSB_v$1_$2_RELEASE.zip to ../Releases"
cp HSB_v$1_$2_DEBUG.zip ../../HSB_v$1_$2_DEBUG.zip
cp HSB_v$1_$2_RELEASE.zip ../../HSB_v$1_$2_RELEASE.zip

#clear Releases/HSB directory
echo "Clearing Releases/HSB directory"
cd ..
rm -rf HSB
#exit
echo "Done"
exit 0
