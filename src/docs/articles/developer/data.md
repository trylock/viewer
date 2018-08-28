# Viewer.Data

The application works with attributes stored in JPEG files. This namespace contains classes responsible for loading and storing these attributes in various places and formats.

## Data representation in the program

The namespace works with entities (see [IEntity](xref:Viewer.Data.IEntity)). Entities are collections of [Attribute](xref:Viewer.Data.Attribute)s. The application uses [FileEntity](xref:Viewer.Data.FileEntity) to represent a file in a file system and [DirectoryEntity](xref:Viewer.Data.DirectoryEntity) to represent a directory in a file system. Directory entities don't have attributes (i.e., it is always an empty collection). 

Attributes have a name, a value and a source and they are immutable. Value is any type derived from [BaseValue](xref:Viewer.Data.BaseValue). Source of an attribute designates from where an attribute has been loaded:

- [Metadata](xref:Viewer.Data.AttributeSource.Metadata) for attributes loaded from file metadata such as file system or Exif JPEG segment
- [Custom](xref:Viewer.Data.AttributeSource.Custom) for attributes added by the user (stored in a custom JPEG segment)

Values are immutable classes. They can be `null` regardless of their type. This property is only used in query evaluation. An entity will never have an attribute whose value is null. Instead, if you try to [SetAttribute](xref:Viewer.Data.IEntity#Viewer_Data_IEntity_SetAttribute_Viewer_Data_Attribute_) whose value is null, an attribute with the same name will be removed from the entity.

## Storage

[IAttributeStorage](xref:Viewer.Data.Storage.IAttributeStorage) represents a place where an entity can be stored. Currently, entities can be stored in their files, SQLite database or in memory. 

### FileSystemAttributeStorage

[FileSystemAttributeStorage](xref:Viewer.Data.Storage.FileSystemAttributeStorage) can store entities to their files and load them. The data is stored in custom APP1 [JPEG segments](https://en.wikipedia.org/wiki/JPEG#Syntax_and_structure). Attributes are first serialized to a binary format and then split into APP1 JPEG segments. Structure of the JPEG segment is following:

- APP1 JPEG segment header (2B): `0xFF` `0xE1`
- Size of the JPEG segment (2B, big endian, the size includes even the two bytes for the size)
- Segment name (5B, ASCII string `"Attr"` with the 0 byte at the end): `0x41` `0x74` `0x74` `0x72` `0x00`
- Segment data (size depends on the size of the JPEG segment, binary format is described below)

> [!NOTE]
> Serialized attributes might not fit into 1 JPEG segment. In this case, the binary data is split into multiple segments at the boundary of the maximal segment size. Segment name (the `"Attr"` ASCII string) is written to all segments so that they can be identified.

#### Binary format of attributes

Attributes are stored consecutively as type, name and value. We will use these types in the format definition:
- `uint16`: unsigned 2 byte integer, little endian
- `int32`: signed 4 byte integer, two's complement, little endian
- `Double`: 8 bytes, IEEE 745, (binary64)
- `String`: UTF8 string with 0 byte at the end
- `DateTime`: String in the W3C DTF format: "YYYY-MM-DDThh:mm:ss.sTZD"

An attribute is serialized to:
- type (`uint16`, (numbers are defined by [AttributeType](xref:Viewer.Data.Formats.Attributes.AttributeType))
- name (`string`)
- value (format depends on the type, it can be `int32`, `Double`, `String` or `DateTime`)

#### Read algorithm

The sotrage reads entities as follows:

1. read all JPEG segments to memory
2. parse attributes from the segments (currently, Exif and Attr segments are parsed)

This means that only image metadata will be read (usually around ten 4 KiB blocks).

#### Write algorithm

1. create a temporary file on the same disk as the original image
2. read all JPEG segments
3. write all but attribute segments to the temporary file
4. serialize entity to JPEG segments and write them to the temporary file
5. write the SOS (Start of Scan) JPEG segment header and copy all remaining data to the temporary file
6. replace the original image with the temporary file

In the first step, it is important that the temporary file is on the same disk as the original. It makes the 6th step as simple as possible which minimizes the probability of corrupting data in the original file.

### SqliteAttributeStorage

[SqliteAttributeStorage](xref:Viewer.Data.Storage.SqliteAttributeStorage) is a special [IDeferredAttributeStorage](xref:Viewer.Data.Storage.IDeferredAttributeStorage). This means that write operations can be deferred until the [ApplyChanges](xref:Viewer.Data.Storage.IDeferredAttributeStorage#Viewer_Data_Storage_IDeferredAttributeStorage_ApplyChanges) method is called. This is the case for a couple of reasons: (1) it is non-blocking (this storage is used as a cache), (2) batching multiple write operations will speed things up as the overhead won't be included with each operation.

The database has a simple schema:

```SQL
CREATE TABLE IF NOT EXISTS `files` (
	`id`	INTEGER NOT NULL,
	`path`	TEXT NOT NULL UNIQUE COLLATE CURRENT_CULTURE_IGNORE_CASE,
	`lastWriteTime`	TEXT NOT NULL,
	`lastAccessTime`	TEXT NOT NULL,
	PRIMARY KEY(`id`)
);
CREATE TABLE IF NOT EXISTS `attributes` (
	`id`	INTEGER NOT NULL,
	`name`	TEXT NOT NULL,
	`source`	INTEGER NOT NULL DEFAULT 0,
	`type`	INTEGER NOT NULL DEFAULT 0,
	`value`	BLOB NOT NULL,
	`owner`	INTEGER NOT NULL,
	PRIMARY KEY(`id`),
	FOREIGN KEY(`owner`) REFERENCES `files`(`id`) on update cascade on delete cascade
);
CREATE UNIQUE INDEX IF NOT EXISTS `files_path_index` ON `files` (
	`path`	ASC
);
CREATE UNIQUE INDEX IF NOT EXISTS `attributes_owner_name_index` ON `attributes` (
	`owner`	ASC,
	`name`	ASC
);
```

For each file it stores the last write time of the file and the last access time of the record in the database. Notice that a special collation is used to the path. This collation is implemented in C# so that paths are compares using a unicode aware comparer.

## CachedAttributeStorage 

[CachedAttributeStorage](xref:Viewer.Data.Storage.CachedAttributeStorage) combines 2 attribute storages where one of the storage is deferred. The deferred storage is used as a cache. The load operation first checks if it can find the entity in the cache storage. If it can't, it will load it from the main storage and store the result in the cache storage. All modifying operations are done to both storages. These operations are applied to the cache storage on a background thread after a set ammount of changes have been made or after a set ammount of time (so that all changes will be applied within a time limit).