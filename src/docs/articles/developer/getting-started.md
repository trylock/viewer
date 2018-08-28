# Getting started

## Download

The project uses [git](https://git-scm.com/) version control system and it is hosted on [github](https://github.com/trylock/viewer). Download it to current directory using [git](https://git-scm.com/) command: `git clone https://github.com/trylock/viewer.git` or use your favourite GUI git client to clone the project. Alternatively, you can [download zipped project](https://github.com/trylock/viewer/archive/master.zip) directly from github.

## Installation and build

1. Download and install [Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=vs-2017) or newer with the .NET Framework 4.7
2. Download Java (version 1.6 or higher, required in order to run ANTLR4)
3. Download the `antlr-4.7.1-complete.jar` file from the [ANTLR4 website](http://www.antlr.org/download.html) 
4. Place the `antlr-4.7.1-complete.jar` file into the `tools` directory located in the project root directory.
5. Open the project solution in Visual Studio `Viewer.sln`. It is located in the project root directory.
6. Build the solution (this should also install all necessary packages from NuGet)

### Building the installer

Optionally, if you want to build the project setup and installer, you'll need to follow these steps:

1. Download and install the [WiX v3 toolset](http://wixtoolset.org/releases/) 
2. Download and install the [WiX Toolset extension for Visual Studio 2017](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension) 
3. Build the `ViewerSetup` project in Visual Studio
4. Build the `ViewerInstaller` project in Visual Studio

### Building documentation

## NuGet packages

This is a list of NuGet packages used by the project with a short explanation of why the package is used.

- `Antlr4.Runtime.Standard` for query compilation
- `DockPanelSuite` for UI customization
- `MetadataExtractor` for parsing Exif metadata in JPEG files
- `Moq` for mocking clases in the test project
- `MSTest` for running tests
- `NLog` for logging errors
- `Scintilla.NET` for the query editor component
- `SkiaSharp` for loading and resizing images (since GDI+'s `DrawImage` is basically useless in a multithreaded environment)
- `System.Data.SQLite.Core` for caching thumbnails and attributes
- Some standard library extensions installed through NuGet: `System.Collections.Immutable`, `System.ValueTuple`

See [Application structure overview](overview.md) next.