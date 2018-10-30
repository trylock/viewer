/** Attribute cache database structure.
 * Scripts are separated with 4 dashes and run separately.
 *
 * We assume following things:
 * (1) file paths as normalized and they use / (forward slash) as directory separator
 * (2) file paths as case INsensitive
 */

/** Tables */

-- files
create table if not exists `files`(
    `id` integer not null,
    `path` text not null unique, -- COLLATE INVARIANT_CULTURE_IGNORE_CASE -- it is assumed to be normalized
    `parent_id` integer,
    -- the following 2 columns are only used for leaf nodes (i.e., files, not directories)
    `last_file_write_time` text, -- the last write time of this file in the file system
    `last_row_access_time` text, -- the last access time of this row entry in the database

    primary key(`id`),
    foreign key(`parent_id`)
        references `files`(`id`)
            on update cascade
            on delete cascade
);

----

-- attributes of files
create table if not exists `attributes`(
    `id` integer not null,
    `name` text not null, -- COLLATE INVARIANT_CULTURE
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

/** Closure of the tree order relation (the relation: "is ancestor of")
 * We need this table for efficient subtree queries used for query optimizations. While it could 
 * be quite large, its blocking factor is also big and it takes far less space than thumbnails.
 */
create table if not exists `files_closure`(
    `parent_id` integer not null,
    `child_id` integer not null,

	primary key(`parent_id`, `child_id`),
    foreign key(`parent_id`)
        references `files`(`id`)
            on update cascade
            on delete cascade,
    foreign key(`child_id`)
        references `files`(`id`)
            on update cascade
            on delete cascade
) without rowid; -- we want the (parent_id, child_id) index to be the clustered index of this table

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

----

-- access to the closure table from the other side (e.g., during the `files_closure_after_insert` trigger)
create index if not exists `files_closure_child_id_index` on `files_closure`(
    `child_id` asc
);

----

/** Triggers */

-- this trigger will fix the closure of the tree order relation for every inserted file
create trigger if not exists `files_closure_after_insert`
after insert on `files` 
for each row 
begin 
    -- ensure the relation is reflexive
    insert into files_closure (parent_id, child_id) values (NEW.id, NEW.id); 
    -- ensure the relation is transitive
    insert into files_closure (parent_id, child_id) 
        select parent_id, NEW.id 
        from files_closure 
        where child_id = NEW.parent_id; 
end;

----

/** This will update the closure for all elements on the path to the root 
 * (i.e., every explicit parent_id update has to be done on leaf nodes)
 */
create trigger if not exists `files_closure_after_update`
after update on `files`
for each row when NEW.parent_id is not null and (OLD.parent_id is null or NEW.parent_id <> OLD.parent_id)
begin
    -- remove all nodes above it
    delete from files_closure where child_id = NEW.id;
    -- add new nodes above it
    insert into files_closure (parent_id, child_id) 
        select parent_id, NEW.id 
        from files_closure 
        where child_id = NEW.parent_id; 
end;

----

-- this trigger will insert file parent directory to the database if it doesn't exist
-- NOTE: this is a recursive trigger
create trigger if not exists `files_insert_parent_trigger`
after insert on `files`
for each row when (NEW.parent_id is null) and (getParentPath(NEW.path) is not null) 
begin
    insert or ignore into `files`(`path`, `parent_id`) values(getParentPath(NEW.path), null);
    update `files` 
    set `parent_id` = (
        select `id` 
        from `files` 
        where `path` = getParentPath(NEW.path)
    )
    where `id` = NEW.id;
end;