# # HSB-#
![alt text](banner.png)
## 🇬🇧🇺🇸 Description

This is a toy project written to study how a web server works

It is written in C-Sharp, compiles and runs on any platform where the dotnet tool is available

-----

Please note that this is a work-in-progress project so API can change between single commits! Until a v1.0.0 is reached this is an expected behaviour, so it's better use the libraries provided in the "release" section

-----

Repository organization


| Project Name                     | Type         | Description                                                                                                                                                    |
|----------------------------------|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| HSB                              | Library      | Is the core library that contains the entire server                                                                                                            |
| Manager                          | Tool         | (WIP) Project that aims to create a instances manager                                                                                                          |
| MultiplatformLauncher            | Tool         | Standalone, multiplatform launcher for HSB projects                                                                                                            |
| Standalone                       | Extension    | Utility to make the HSB core library directly runnable, provides additional functionaly                                                                        |
| StandaloneLauncherExampleAndTest | Example/Test | Provides an example for the Multiplatform launcher, just build it as library then copy the two DLLs to the executable path of the launcher to see it in action |
| Template                         | Template     | Provides a template for Visual Studio                                                                                                                          |
| TestRunner                       | Test         | Contains all test created to build new HSB features                                                                                                            |


More info can be found in the Documentation folder
[](./Documentation/)


#### Roadmap
Small roadmap planned


| Version | Feature Planned             |
|---------|-----------------------------|
| 0.0.5R2 | Better debugging            |
| 0.0.6   | Http Session implementation |
| 0.0.7   | Http Authentication         |
| 0.0.8   | Refactor/Clean-up           |
| 0.0.9   | To be decided               |
