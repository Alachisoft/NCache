CREATE TABLE ncache_db_sync(
cache_key VARCHAR(256),
cache_id VARCHAR(256),
modified bit DEFAULT(0),
work_in_progress bit Default(0),
PRIMARY KEY(cache_key, cache_id) );
Go