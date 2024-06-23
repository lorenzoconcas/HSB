//run this program with the --listFiles argument
//example : $ ./FileListing --listFiles
//you can compile it to a single file with command :
//dotnet publish -r  {platform-target} -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
//see https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file
//for more information

using HSB;



Configuration c = new();
c.GetRawArguments().ForEach(Console.WriteLine);
//we manually force the file listing mode
//adding the --listFiles argument
c.GetRawArguments().Add("--listFiles");

new Server(c).Start();