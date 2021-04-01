using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData
{
 
    public enum FeatureEnum : short
    {
        none,
        partitioned_replica_topology,
        partitioned_topology,
        replicated_topology,
        mirror_topology,
        local_outproc,
        local_inproc,
        client_cache,

        //events
        //cache level events
        cache_event,
        cache_clear_event_feature,
        cache_stopped_event_feature,
        //cluster level events
        cluster_change_events,
        node_join_event,
        node_leave_event,


        //data sharing 
        data_sharing,
        pubsub,
        durable_subscription,
        non_durable_subscription,
        message_expiration,
        delivery_failure_notificatiion,

        continuous_query,
        selective_events,
        general_events,
        collection_events,

        //security

        security,
        node_level_security,
        cache_security,
        encryption_aes,
        encryption_intel_aes,
        encryption_fips_aes,
        encryption_3des,


        //locking
        locking,

        //item versioning
        item_versioning,

        //streamining api
        streaming,

        //search cache
        //distributedlucene
        sql_queries,
        query_indexes,
        //linqqueries
        tags,
        named_tags,
        groups,

        //expiration
        //expirationhint
        absolute_expiration,
        sliding_expiration,
        default_expiration,

        //eviction
        eviction,
        priority_eviction,
        lru,
        lru_legacy,
        lfu,
        donot_evict,

        //cache-dependencies
        //expirationhint
        key_dependency,
        file_dependency,
        aggregate_dependency,
        custom_dependency,
        cache_sync_dependency,

        //database dependency
        //expirationhint
        db_dependency,
        sql_dependency,
        oracle_dependency,
        oledb_dependency,
        clr_db_procedures,

        //cache loader
        cache_loader,
        cache_loader_with_hints,

        //backing source
        backing_source,
        read_thru,
        write_thru,
        write_behind_batching,
        write_behind_nonbatching,

        //datatypes

        distributed_datatypes,
        dist_list,
        dist_hashset,
        dist_dictionary,
        dist_queue,
        dist_counter,

        //mapreduce
        map_reduce,
        aggregator,

        //entry processor
        entry_processor,

        //location affinity
        location_affinity,

        //compression
        compression,

        //compact serialization
        compact_serialization,

        //pipelining
        pipelining,

        //email notification
        email_notification,

        //lucene(text searching)
        lucene,

        //serialization

        binary_serialization,

        json_serialization,

        //split brain
        split_brain,

        split_brain_detection,

        split_brain_recovery,

        //bulk operations
        bulk_operations,

        //client conectivity notifications 
        client_connectivity_notification,

        //tls support
        tls_support,

        //graceful shutdown
        graceful_shutdown,

        //maintenance mode
        maintenance_mode,

        cache_auto_start,

        //bridge
        bridge,
        conflict_resolver,

        //data portability
        data_portability,
        auto_load_balancing,

        expiration,
        cache_dependency,
        view_state,
        aspnetcore_signalr,
        outputcache_provider,
        hibernate,
        efcore61,
        aspnet_session,
        aspnetcore_session,
        efcore,
    }

}
