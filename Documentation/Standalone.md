# Standalone
HSB can run without being embedded (if compiled) thanks to the "Standalone" project.

When you run this project without any arguments or configuration an empty, default, failsafe configuration, will be written to the disk, as "config.json" file. 
All valid arguments are printing passing the "--info" argument like this: 

```bash
HSB_Standalone --info
```

Here is the list of all valid arguments:


| Argument         | Description                                                                                                                                                                                                                                                               | Usage example                                                             |
|------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------|
| --config-path    | Specifies a path for the json configuration of the server                                                                                                                                                                                                                 | HSB_Standalone --config-path="./path_to_disk"                             |
| --create-default | Creates a default configuration, same as not running arguments                                                                                                                                                                                                            | HSB_Standalone --create-default                                           |
| --info           | Prints the info screen with the valid arguments                                                                                                                                                                                                                           | HSB_Standalone --info                                                     |
| --no-verbose     | Disable verbose writing (WIP)                                                                                                                                                                                                                                             | HSB_Standalone --no-verbose                                               |
| --port           | Manually specifies the listening port                                                                                                                                                                                                                                     | HSB_Standalone --port=8080                                                |
| --address        | Manually specifies the listening address                                                                                                                                                                                                                                  | HSB_Standalone --address="127.0.0.1"                                      |
| --assembly       | Specifies additional assemblies that will be loaded, this is the only argument that can be used multiple times. HSB MUST NOT be embedded in your code. Also your code must be compiled with the same library used to compile the standalone or the version will conflict! | HSB_Standalone --assembly ="./assembly1.dll" --assembly="./assembly2.dll" |

