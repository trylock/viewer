# Multifilter Image Viewer

Viewer is a program that lets you store key/value pairs directly into JPEG files. Tagged files can then be searched using a custom SQLike query language. Since all tags are stored directly in the JPEG files, you can freely move files (even between 2 computers) without losing any tags.

**Important**: this application is in active development. There has not been a stable version release yet.

## UI overview

This is a screenshot of the application. 

![Overview](docs/images/overview.png)

It contains many windows. All windows are resizable and movable. Lets have a look at some of the components.

### Thumbnail Grid

The window labeled `Images` contains a grid of thumbnails from the query result set. It works very simillar to Windows File Explorer. You can select images using mouse pointer. Copy, cut, rename and delete files using standard shortcuts or a context menu opened by right clicking anywhere in the window. Size of thumbnails can be dynamically changed using the thumbnail size slide shown in the bottom right corner of the application (in the status bar).

### Attributes

When you select photos in the application, all their attributes are shown in windows labeled `Exif` and `Attributes`. `Exif` contains read-only tags parsed from the Exif segment. `Attributes` contains editable attributes saved in a custom attributes segment. You can edit these attributes for all selected photos and save all modifications using the save button shown in the bottom right corner of the `Attributes` window.

### Explorer

You can easily navigate throuh local files using the window labeled `Explorer`. It contains tree view with all local folders. Clicking on a folder in this tree view will execute query `SELECT "path_to_the_folder"` which effectively opens content of this directory in the thumbnail grid window.

### Presentation 

You can view all your photos in the `Presentation` window. It shows photos one by one scaled down to the window size. Pointing your cursor to the bottom of this window will open a control panel. You can start presentation, set presentation speed and zoom in and out using this control panel. Additionaly, a fullscreen presentation can be opened in this panel. 

### Query Editor

The window labeled `Query` contains a viewer query. Queries can be executed, opened or saved using the application toolbar (shown in the top left). See [Query Language](#query-language) for information about query syntax.

## Query Language

Viewer Query Language can be used to query files in the file system. All keywords and function identifiers are case insensitive (i.e., `select` and `SelECt` is the same keywrod).

### `select`

Each query starts with the `select` keyword after which can be a directory path pattern. This pattern determines which directories will be searched. It can contain some special characters (for API, see [FileFinder](xref:Viewer.IO.FileFinder)):

1) `*`: matches a sequence of characters except for a directory separator (`/` or `\`). For example, `x` `xa`, `xab` are all matched by `x*`. On the other hand, it does not match `ya` since that does not start with `x`, nor does it match `xa/b` since that contains a directory separator.
2) `?`: matches any character except for a directory separator. For example, the pattern `a?b` matches `axb`, `ayb` but it does not match `ab` or `axyb` since `?` has to be replace with exactly one character.
3) `**`: matches a sequence of characters including a directory separator. For example, `x/y`, `x/a/y` and `x/a/b/y` are all matched by `x/**/y`. The pattern does not match `x/a` since that does not end with `y`. 

#### Examples of `select` query

TBD