/** Attribute cache database structure.
 * Scripts are separated with 4 dashes and run separately.
 *
 * We assume following things:
 * (1) file paths are normalized 
 * (2) file paths are case INsensitive
 */

/** Tables */

-- files
create table if not exists `files`(
    `id` integer not null,
    `path` text not null unique COLLATE INVARIANT_CULTURE_IGNORE_CASE, -- it is assumed to be normalized
    `last_file_write_time` text, -- the last write time of this file in the file system
    `last_row_access_time` text, -- the last access time of this row entry in the database

    primary key(`id`)
);

----

-- attributes of files
create table if not exists `attributes`(
    `id` integer not null,
    `name` text not null COLLATE INVARIANT_CULTURE,
    `source` integer not null default 0,
    `type` integer not null default 0, -- type as defined in the AttributeType enum
    `value` blob not null,
    `file_id` integer not null,

    primary key(`id`),
    foreign key(`file_id`) 
        references `files`(`id`)
            on update cascade
            on delete cascade
);

----

/** Indexes */

-- index used for random access to files
create unique index if not exists `files_path_index` on `files`(
    `path` asc
);

----

-- index used for access to attributes of given file
create index if not exists `attributes_file_id_index` on `attributes`(
    `file_id` asc
);

----

-- index used for random access to custom attribute names (in attribute name suggestion)
create index if not exists `attributes_source_name_index` on `attributes`(
    `source` asc,
    `name` asc
);
