# Thread Safety

Registry uses `ConcurrentBag`/`ConcurrentDictionary` with a cache-replacement invalidation pattern (replace entire dictionary reference rather than clearing) for thread-safe converter and blacklist cache management.
