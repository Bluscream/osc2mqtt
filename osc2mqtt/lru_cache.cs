namespace Namespace {
    
    using functools;
    
    using namedtuple = collections.namedtuple;
    
    using update_wrapper = functools.update_wrapper;
    
    using RLock = threading.RLock;
    
    using System.Collections.Generic;
    
    using System.Collections;
    
    public static class Module {
        
        static Module() {
            @"Backport of functools.lru_cache decorator from Python 3.3.

Source:

    http://code.activestate.com/recipes/578078-py26-and-py30-backport-of-python-33s-lru-cache/

";
        }
        
        public static object _CacheInfo = namedtuple("CacheInfo", new List<object> {
            "hits",
            "misses",
            "maxsize",
            "currsize"
        });
        
        // Patch two bugs in functools.update_wrapper.
        [functools.wraps(functools.update_wrapper)]
        public static object update_wrapper(object wrapper, object wrapped, object assigned = functools.WRAPPER_ASSIGNMENTS, object updated = functools.WRAPPER_UPDATES) {
            // workaround for http://bugs.python.org/issue3445
            assigned = tuple(from attr in assigned
                where hasattr(wrapped, attr)
                select attr);
            wrapper = functools.update_wrapper(wrapper, wrapped, assigned, updated);
            // workaround for https://bugs.python.org/issue17482
            wrapper.@__wrapped__ = wrapped;
            return wrapper;
        }
        
        public class _HashedSeq
            : list {
            
            public _HashedSeq(object tup, object hash = hash) {
                this[":"] = tup;
                this.hashvalue = hash(tup);
            }
            
            public virtual object @__hash__() {
                return this.hashvalue;
            }
        }
        
        // Make a cache key from optionally typed positional and keyword arguments
        public static object _make_key(
            object args,
            object kwds,
            object typed,
            object kwd_mark = ValueTuple.Create(object()),
            object fasttypes = new HashSet({
                @int}, {
                str}, {
                frozenset}, {
                type(null)}),
            object sorted = sorted,
            object tuple = tuple,
            object type = type,
            object len = len) {
            var key = args;
            if (kwds) {
                var sorted_items = kwds.items().OrderBy(_p_1 => _p_1).ToList();
                key += kwd_mark;
                foreach (var item in sorted_items) {
                    key += item;
                }
            }
            if (typed) {
                key += tuple(from v in args
                    select type(v));
                if (kwds) {
                    key += tuple(from _tup_2 in sorted_items.Chop((k,v) => (k, v))
                        let k = _tup_2.Item1
                        let v = _tup_2.Item2
                        select type(v));
                }
            } else if (key.Count == 1 && fasttypes.Contains(type(key[0]))) {
                return key[0];
            }
            return _HashedSeq(key);
        }
        
        // Least-recently-used cache decorator.
        // 
        //     If *maxsize* is set to None, the LRU features are disabled and the cache
        //     can grow without bound.
        // 
        //     If *typed* is True, arguments of different types will be cached separately.
        //     For example, f(3.0) and f(3) will be treated as distinct calls with
        //     distinct results.
        // 
        //     Arguments to the cached function must be hashable.
        // 
        //     View the cache statistics named tuple (hits, misses, maxsize, currsize) with
        //     f.cache_info().  Clear the cache and statistics with f.cache_clear().
        //     Access the underlying function with f.__wrapped__.
        // 
        //     See:  http://en.wikipedia.org/wiki/Cache_algorithms#Least_Recently_Used
        // 
        //     
        public static object lru_cache(object maxsize = 128, object typed = false) {
            // Users should only access the lru_cache through its public API:
            //       cache_info, cache_clear, and f.__wrapped__
            // The internals of the lru_cache are encapsulated for thread safety and
            // to allow the implementation to change (including a possible C version).
            Func<object, object> decorating_function = user_function => {
                var cache = new Dictionary<object, object>();
                var stats = new List<object> {
                    0,
                    0
                };
                var HITS = 0;
                var MISSES = 1;
                var make_key = _make_key;
                var cache_get = cache.get;
                var _len = len;
                var @lock = RLock();
                var root = new List<object>();
                root[":"] = new List<object> {
                    root,
                    root,
                    null,
                    null
                };
                var nonlocal_root = new List<object> {
                    root
                };
                var PREV = 0;
                var NEXT = 1;
                var KEY = 2;
                var RESULT = 3;
                if (maxsize == 0) {
                } else if (maxsize == null) {
                }
                Func<object, object, object> wrapper = (kwds,args) => {
                    // no caching, just do a statistics update after a successful call
                    var result = user_function(args, kwds);
                    stats[MISSES] += 1;
                    return result;
                };
                Func<object, object, object> wrapper = (kwds,args) => {
                    // simple caching without ordering or size limit
                    var key = make_key(args, kwds, typed);
                    var result = cache_get(key, root);
                    if (result != root) {
                        stats[HITS] += 1;
                        return result;
                    }
                    result = user_function(args, kwds);
                    cache[key] = result;
                    stats[MISSES] += 1;
                    return result;
                };
                Func<object, object, object> wrapper = (kwds,args) => {
                    // size limited caching that tracks accesses by recency
                    var key = kwds || typed ? make_key(args, kwds, typed) : args;
                    using (var @lock) {
                        link = cache_get(key);
                        if (link != null) {
                            // record recent use of the key by moving it to the front of the list
                            _tup_1 = nonlocal_root;
                            root = _tup_1.Item1;
                            _tup_2 = link;
                            link_prev = _tup_2.Item1;
                            link_next = _tup_2.Item2;
                            key = _tup_2.Item3;
                            result = _tup_2.Item4;
                            link_prev[NEXT] = link_next;
                            link_next[PREV] = link_prev;
                            last = root[PREV];
                            last[NEXT] = link;
                            link[PREV] = last;
                            link[NEXT] = root;
                            stats[HITS] += 1;
                            return result;
                        }
                    }
                    var result = user_function(args, kwds);
                    using (var @lock) {
                        _tup_3 = nonlocal_root;
                        root = _tup_3.Item1;
                        if (cache.Contains(key)) {
                            // getting here means that this same key was added to the
                            // cache while the lock was released.  since the link
                            // update is already done, we need only return the
                            // computed result and update the count of misses.
                        } else if (_len(cache) >= maxsize) {
                            // use the old root to store the new key and result
                            oldroot = root;
                            oldroot[KEY] = key;
                            oldroot[RESULT] = result;
                            // empty the oldest link and make it the new root
                            root = oldroot[NEXT];
                            oldkey = root[KEY];
                            oldvalue = root[RESULT];
                            root[KEY] = null;
                            // now update the cache dictionary for the new links
                            cache.Remove(oldkey);
                            cache[key] = oldroot;
                        } else {
                            // put result in a new link at the front of the list
                            last = root[PREV];
                            link = new List<object> {
                                last,
                                root,
                                key,
                                result
                            };
                            last[NEXT] = link;
                        }
                        stats[MISSES] += 1;
                    }
                    return result;
                };
                Func<object> cache_info = () => {
                    using (var @lock) {
                        return _CacheInfo(stats[HITS], stats[MISSES], maxsize, cache.Count);
                    }
                };
                Func<object> cache_clear = () => {
                    using (var @lock) {
                        cache.clear();
                        root = nonlocal_root[0];
                        root[":"] = new List<object> {
                            root,
                            root,
                            null,
                            null
                        };
                        stats[":"] = new List<object> {
                            0,
                            0
                        };
                    }
                };
                wrapper.@__wrapped__ = user_function;
                wrapper.cache_info = cache_info;
                wrapper.cache_clear = cache_clear;
                return update_wrapper(wrapper, user_function);
            };
            return decorating_function;
        }
    }
}
