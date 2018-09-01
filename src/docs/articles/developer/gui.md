# Viewer

The `Viewer` namespace contains a GUI application written in Windows Forms. It uses services from all other namespaces and provides a GUI for them.

## Components

The GUI code uses the MVP pattern. Each window has a [Presenter](xref:Viewer.Core.UI.Presenter) of some [IWindowView](xref:Viewer.Core.UI.IWindowView). Views are more or less passive. They have a set of events and methods which change the view. Typically, a presenter gets its view in constructor, registers its event handlers and calls view methods to change it in response to the events.

## Images 

The Images component is responsible for evaluating a query and showing its result in a thumbnail grid. Since query returns entities in a random order, it has to sort the result incrementally. This is basically what the [QueryEvaluator](xref:Viewer.UI.Images.QueryEvaluator) class does. Additionally, it watches entity changes using [IEntityManager](xref:Viewer.Data.IEntityManager) and file changes using [IFileWatcher](xref:Viewer.IO.IFileWatcher).

### Thumbnails

Thumbnail grid primarily shows thumbnails of entities. Thumbnails are loaded lazily using [ILazyThumbnail](xref:Viewer.UI.Images.ILazyThumbnail). Main purpose of [ILazyThumbnail](xref:Viewer.UI.Images.ILazyThumbnail) is to provide a non-intrusive, non-blocking and easy to use interface for the thumbnail grid view to get a thumbnail. 

Embedded thumbnail (e.g. in the Exif segment) is loaded as an entity attribute with all other attributes. The program tries to decode this thumbnail first. If it is missing or its dimensions are too small for current thumbnail size, it will try to generate a new thumbnail from the original image. The genrated thumbnail is stored as an attribute of the entity and in the SQLite storage.

### Generating thumbnails

Thumbnails are generated using the [IThumbnailLoader](xref:Viewer.UI.Images.IThumbnailLoader) class. Embedded thumbnails are decoded on a thread pool thread. 

Native thumbnails (i.e., thumbnails generated from the original image) use task continuations to define a task graph which synchronizes I/O operations (reading the file from a disk) but it can execute some operations in parallel (decoding the image, generating the thumbnail). Basically, the task graph is very simple:

```
load → decode → generate
 ↓
load → decode → generate
 ↓
...
```

In this diagram, `load` is a task which loads the whole encoded file into memory. `decode` and `generate` are tasks which will decode the loaded JPEG file and generate a thumbnail. Since there could be a lot of waiting `decode` tasks (which have a rather large memory buffers), they are syncrhonized using a semaphore. Only a set ammount of tasks are allowed to be running at once (at the moment, they are limmited to the number of logical cores).

## Explorer

Explorer is a component which displays a directory tree. It's pretty straightforward. Other components can use its [IExplorer](xref:Viewer.UI.Explorer.IExplorer) service. It copies or moves a list of files and creates all necessary UI including a progress bar.