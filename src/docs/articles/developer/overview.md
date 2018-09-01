# Application structure overview

The application is separated into several namespaces. Some namespaces (like `Viewer.Data`) are in a custom assembly to further separate them from the rest of the project. There are 6 main namespaces:

- `Viewer.Core` contains common types and interfaces used by all other components
- `Viewer.Data` contains classes which read, modify and write attributes in files and in memory. It also contains comparison functions used to compare attributes and their values.
- `Viewer.Query` contains classes which compile and execute VQL queries. There are also types used at query runtime (e.g. for adding and calling functions, operators etc.).
- `Viewer.QueryRuntime` contains implementation of functions and operators used in VQL queries.
- `Viewer.IO` contains helper classes and wrappers for working with the file system.
- `Viewer` Windows Forms application which provides a GUI for classes in other namespaces.

Other projects: `ViewerTests`, `ViewereSetup` and `ViewerInstaller` contain tests, application setup and installer respectively. 

## Components

This application uses the standard [Managed Extensibility Framework (MEF)](https://docs.microsoft.com/en-us/dotnet/framework/mef/).  See a more detailed description of some other namespaces: 

- [Viewer.Data](data.md)
- [Viewer.Query](query.md)
- [Viewer](gui.md)