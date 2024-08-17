# VXInstaller
The official GUI installer for [VX](https://github.com/doggybootsy/vx) on Windows

# Usage
Eventually it'll get uploaded to the Microsft Store

Currently you must publish your own local copy
### Visual Studio
* Follow the contributing section
* Right click VXInstaller
* Then select `Package or Publish` -> `Create App Package`
* Select `sideloading`
* Create a self signed certificate (or use a certificate you own)
* Then press `create`
* Then open the directory it gives, then open the directory inside that
* Open the `VXInstaller_[version]_x64.msix`
* CLick `install` then open the application

# Contributing
* First you need all the prequisites for Windows App SDK
* Then you clone the repo `git clone https://github.com/doggybootsy/VXInstaller`
* Open the solution in Visual Studio (or editor of choice)
* Then compile for your platform / architicture 