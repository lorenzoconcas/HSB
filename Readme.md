# HttpServerBoxed

![alt text](banner.png)
### 🇬🇧🇺🇸 Description

This is a project written to study how a web server works

It is written in C-Sharp, compiles and runs on any platform where the dotnet tool is available

[Project Webpage](https://lorenzoconcas.github.io/HSB)

-----

Please note that this is a work-in-progress project so API can change between single commits! Until a v1.0.0 is reached this is an expected behaviour, so it's better use the libraries provided in the "release" section

-----

Repository organization


| Project Name                     | Type         | Description                                                                                                                                                    |
|----------------------------------|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| HSB                              | Library      | Is the core library that contains the entire server                                                                                                            |
| Runner                           | DevExtension | Contains the code used to test HSB in action                                                                                                                   |
| Manager                          | Tool         | (WIP) Project that aims to create a instances manager                                                                                                          |
| MultiplatformLauncher            | Tool         | Standalone, multiplatform launcher for HSB projects                                                                                                            |
| Standalone                       | Extension    | Utility to make the HSB core library directly runnable, provides additional functionaly                                                                        |
| LaunchableExample                | Example/Test | Provides an example for the Multiplatform launcher, just build it as library then copy the two DLLs to the executable path of the launcher to see it in action |
| Template                         | Template     | Provides a template for Visual Studio                                                                                                                          |
| Examples                         | Folder| Contains example projects that illustrates the API                                                                                                                    |
| Experiments | Folder | This folder contains temporary projects used to create complex new features that will be merged into the main library |


More info can be found in the Documentation folder
[](./Documentation/)


#### Roadmap
Small roadmap planned


| Version | Feature Planned             | Status                     |
|---------|-----------------------------|----------------------------|
| 0.0.5R2 | Better debugging            | ✅ Ready                   |
| 0.0.6   | Http Session implementation | ✅ Ready                   |
| 0.0.7   | Refactor/Clean-up           | ✅ Ready 1*                |
| 0.0.8   | Http Authentication         | ✅ Ready                   |
| 0.0.9   | FormData/File upload        | ✅ Ready                   |
| 0.0.10  | WebSocket support           | ✅ Ready (current stable)  | 
| 0.0.11  | SSL/TLS                     | ✅ Ready for test 2*         |

1* Version 0.0.7 will be skipped
2* Custom TLS implementation is still WIP
Note: "Experiments/WebSocket" project is going to be deleted when current version (0.0.11) will reach stable status