# Application structure overview

The application is separated into several namespaces. Some namespaces (like `Viewer.Data`) are in a custom assembly to further separate them from the rest of the project. There are 6 main namespaces:

- `Viewer.Core` contains common types and interfaces used by all other components (i.e., every other assembly depends on this)
- `Viewer.Data` contains classes which read, modify and write attributes in files and in memory. It also contains comparison functions used to compare attributes and their values.
- `Viewer.Query` contains classes which compile and execute VQL queries. There are also types used at query runtime (e.g. for adding and calling functions, operators etc.).
- `Viewer.QueryRuntime` contains implementation of functions and operators used in VQL queries.
- `Viewer.IO` contains helper classes and wrappers which work with the file system.
- `Viewer` is the actual application. It produces executable which has a GUI.

Other projects: `ViewerTests`, `ViewereSetup` and `ViewerInstaller` contain tests, application setup and installer respectively. 

## Components

This application uses the standard [Managed Extensibility Framework (MEF)](https://docs.microsoft.com/en-us/dotnet/framework/mef/). It is a composition of multiple components (i.e., classes which implement the [IComponent](xref:Viewer.Core.IComponent) interface). Components can have UI elements (such as their own window) but it is not required.

## Queries

## Entities