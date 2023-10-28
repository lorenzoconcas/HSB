#!/bin/bash
echo "HSB Distribution Script"

#if first arg is -h or --help, print help
if [ "$1" == "-h" ] || [ "$1" == "--help" ]; then
  echo "Usage: "
  echo "./distribute.sh -h or --help"
  echo "./distribute.sh -v versionName -o otherValues -p hsbPath"
  echo "./distribute.sh -v versionName"
  echo "./distribute.sh -o otherValues"
  echo "./distribute.sh -p hsbPath"
  echo "./distribute.sh"
  echo "versionName: The version name (string) of the build. If not passed, the version name will be detected from ./HSB/Properties/AssemblyInfo.cs"
  echo "otherValues: Other string values to be appended to the zip file name. If not passed, the zip file name will be HSB_v{versionName}_DEBUG.zip and HSB_v{versionName}_RELEASE.zip"
  echo "hsbPath: The path to the HSB source. If not passed, the default path is ../HSB"
  echo "If no arguments are passed, the script will use the default values"
  exit 0
fi

cd "$(dirname "$0")" #this is to fix run from Apple Shortcuts or when you cannot set the working directory

function pause(){
 read -s -n 1 -p "Press any key to continue . . ."
 echo ""
}

function cleanup(){
  echo "Cleaning up"
  cd ..
  rm -rf HSB
  exit 0
}

versionName=""
otherValues=""
hsbPath="../HSB"
baseWorkingDir=""

#function that builds a C# project (csproj)
function buildProject(){
  projPath=$1
  mode=$2
  destPath=$3
  archivePath=$4
  echo "Building project $projPath in $mode mode to $destPath"
  dotnet publish $projPath -c $mode -o $destPath
  echo "Archiving project"
  cd $destPath
  #print file name
  fileName="HSB_v${versionName}${otherValues}_${mode}.zip"
  echo "File name: $fileName"
  zip -r $fileName *
  mv HSB_v${versionName}${otherValues}_${mode}.zip ../${archivePath}/HSB_v${versionName}${otherValues}_${mode}.zip
  cd $baseWorkingDir
}


#this get the value of a flag (the argument after the flag)
function getFlagValue(){ 
  flag=$1
  for (( i=2; i <= $#; i++ ))
  do
    arg=${!i}
    #if arg is the flag, get the next argument
    if [ "$arg" == "$flag" ]; then
      v=$((i+1))
      echo ${!v}
      break
    fi
  done
}

#check if there is an argument with value -o
otherValues=$(getFlagValue "-o" "$@")
#if otherValues != "" then prepend _ to it
if [ "$otherValues" != "" ]; then
  otherValues="_$otherValues"
fi

#check if a custom HSB path is passed
hsbPath=$(getFlagValue "-p" "$@")

#check if dotnet is installed
if ! [ -x "$(command -v dotnet)" ]; then
  echo 'Error: dotnet is not installed.' >&2
  exit 2
fi


if [ "$hsbPath" != "" ]; then
  echo Custom hsbPath set to $hsbPath
  if [ ! -d "$hsbPath" ]; then
    echo "Custom HSB folder not found, make sure it exists and is a valid path"
    exit 1
  fi
  cd $hsbPath
  echo Current working directory is: $(pwd)
else
  cd "$(dirname "$0")" #if no custom path is passed, set the working directory to the script directory
  cd .. #go to parent directory
fi
baseWorkingDir=$(pwd)
echo Current working directory is: $(pwd)
if [ ! -d "HSB" ]; then
  echo "HSB source not found. Make sure that the HSB source is the root path of the folder this script is in"
  exit 1
fi


#check if version name is passed, if not detect from ./HSB/Properties/AssemblyInfo.cs
versionName=$(getFlagValue "-v" "$@")

#if version name is not passed, detect from {hsbPath}/HSB/Properties/AssemblyInfo.cs
if [ "$versionName" == "" ]; then
  echo Version name not passed. Using version name from HSB/Properties/AssemblyInfo.cs
  #check if AssemblyInfo.cs exists
  if [ ! -f "HSB/Properties/AssemblyInfo.cs" ]; then
    echo "AssemblyInfo.cs not found. Make sure that the HSB source is the root path of the folder this script is in"
    exit 1
  fi
  #get version name from AssemblyInfo.cs
  versionName=$(grep -oE '\[assembly: AssemblyVersion\(\"(\d+.\d+.\d+)' HSB/Properties/AssemblyInfo.cs)
  #cut after [assembly: AssemblyVersion(" 
  versionName=${versionName:28}
fi
echo Version name: $versionName
echo Other values: $otherValues
echo HSB path: $hsbPath


#check if Releases directory exists (../Releases/HSB), if not create it
if [ ! -d "Releases/HSB" ]; then
  echo "Releases directory not found. Creating Releases directory"
  mkdir Releases
  mkdir Releases/HSB
fi


######CLEANUP######
#clear zip files
echo "Clearing previous archives files"
rm -f Releases/HSB/*.zip 2> /dev/null

######DEBUG MODE BUILD######

#compile HSB in debug mode
echo "Compiling HSB in debug mode"

buildProject HSB/HSB.csproj Debug Releases/HSB/Debug ../../Releases

######RELEASE MODE BUILD######

#compile HSB in release mode
echo "Compiling HSB in release mode"

buildProject HSB/HSB.csproj Release Releases/HSB/Release ../../Releases

#final cleanup

#clear Releases/HSB directory
clearPath="${baseWorkingDir}/Releases/HSB"
echo "Clearing $clearPath"
cd ..
rm -rf $clearPath
#exit
echo "Done"
exit 0




