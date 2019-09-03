// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==
/*============================================================
**
** Class:  HashVector
**
** <OWNER>[....]</OWNER>
**
**
** Purpose: Clustered Hash table implementation
**
** 
===========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Threading;
using System.Runtime.ConstrainedExecution;
using System.Collections;
using Alachisoft.NCache.Common.Transactions;
#if DEBUG
using System.Diagnostics.Contracts;
#endif



namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    // The Hashtable class represents a dictionary of associated keys and values
    // with constant lookup time.
    // 
    // Objects used as keys in a hashtable must implement the GetHashCode
    // and Equals methods (or they can rely on the default implementations
    // inherited from Object if key equality is simply reference
    // equality). Furthermore, the GetHashCode and Equals methods of
    // a key object must produce the same results given the same parameters for the
    // entire time the key is present in the hashtable. In practical terms, this
    // means that key objects should be immutable, at least for the time they are
    // used as keys in a hashtable.
    // 
    // When entries are added to a hashtable, they are placed into
    // buckets based on the hashcode of their keys. Subsequent lookups of
    // keys will use the hashcode of the keys to only search a particular bucket,
    // thus substantially reducing the number of key comparisons required to find
    // an entry. A hashtable's maximum load factor, which can be specified
    // when the hashtable is instantiated, determines the maximum ratio of
    // hashtable entries to hashtable buckets. Smaller load factors cause faster
    // average lookup times at the cost of increased memory consumption. The
    // default maximum load factor of 1.0 generally provides the best balance
    // between speed and size. As entries are added to a hashtable, the hashtable's
    // actual load factor increases, and when the actual load factor reaches the
    // maximum load factor value, the number of buckets in the hashtable is
    // automatically increased by approximately a factor of two (to be precise, the
    // number of hashtable buckets is increased to the smallest prime number that
    // is larger than twice the current number of hashtable buckets).
    // 
    // Each object provides their own hash function, accessed by calling
    // GetHashCode().  However, one can write their own object 
    // implementing IEqualityComparer and pass it to a constructor on
    // the Hashtable.  That hash function (and the equals method on the 
    // IEqualityComparer) would be used for all objects in the table.
    //
    // Changes since V1 during Whidbey:
    // *) Deprecated IHashCodeProvider, use IEqualityComparer instead.  This will
    //    allow better performance for objects where equality checking can be
    //    done much faster than establishing an ordering between two objects,
    //    such as an ordinal string equality check.
    // 
    [DebuggerDisplay("Count = {Count}")]
    [System.Runtime.InteropServices.ComVisible(true)]
#if DEBUG
    [DebuggerTypeProxy(typeof(VectorDebugView))]
#endif
    [Serializable]
    public class HashVector : ISizableDictionary, ISizableIndex, ISerializable, IDeserializationCallback, ICloneable, IDictionary
    {
        /*
          Implementation Notes:
          The generic Dictionary was copied from Hashtable's source - any bug 
          fixes here probably need to be made to the generic Dictionary as well.
    
          This Hashtable uses double hashing.  There are hashsize buckets in the 
          table, and each bucket can contain 0 or 1 element.  We a bit to mark
          whether there's been a collision when we inserted multiple elements
          (ie, an inserted item was hashed at least a second time and we probed 
          this bucket, but it was already in use).  Using the collision bit, we
          can terminate lookups & removes for elements that aren't in the hash
          table more quickly.  We steal the most significant bit from the hash code
          to store the collision bit.

          Our hash function is of the following form:
    
          h(key, n) = h1(key) + n*h2(key)
    
          where n is the number of times we've hit a collided bucket and rehashed
          (on this particular lookup).  Here are our hash functions:
    
          h1(key) = GetHash(key);  // default implementation calls key.GetHashCode();
          h2(key) = 1 + (((h1(key) >> 5) + 1) % (hashsize - 1));
    
          The h1 can return any number.  h2 must return a number between 1 and
          hashsize - 1 that is relatively prime to hashsize (not a problem if 
          hashsize is prime).  (Knuth's Art of Computer Programming, Vol. 3, p. 528-9)
          If this is true, then we are guaranteed to visit every bucket in exactly
          hashsize probes, since the least common multiple of hashsize and h2(key)
          will be hashsize * h2(key).  (This is the first number where adding h2 to
          h1 mod hashsize will be 0 and we will search the same bucket twice).
          
          We previously used a different h2(key, n) that was not constant.  That is a 
          horrifically bad idea, unless you can prove that series will never produce
          any identical numbers that overlap when you mod them by hashsize, for all
          subranges from i to i+hashsize, for all i.  It's not worth investigating,
          since there was no clear benefit from using that hash function, and it was
          broken.
    
          For efficiency reasons, we've implemented this by storing h1 and h2 in a 
          temporary, and setting a variable called seed equal to h1.  We do a probe,
          and if we collided, we simply add h2 to seed each time through the loop.
    
          A good test for h2() is to subclass Hashtable, provide your own implementation
          of GetHash() that returns a constant, then add many items to the hash table.
          Make sure Count equals the number of items you inserted.

          Note that when we remove an item from the hash table, we set the key
          equal to buckets, if there was a collision in this bucket.  Otherwise
          we'd either wipe out the collision bit, or we'd still have an item in
          the hash table.

           -- 
        */

        internal const Int32 HashPrime = 101;
        private const Int32 InitialSize = 3;
        private const String LoadFactorName = "LoadFactor";
        private const String VersionName = "Version";
        private const String ComparerName = "Comparer";
        private const String HashCodeProviderName = "HashCodeProvider";
        private const String HashSizeName = "HashSize";  // Must save buckets.Length
        private const String KeysName = "Keys";
        private const String ValuesName = "Values";
        private const String KeyComparerName = "KeyComparer";
        // Deleted entries have their key set to buckets

        // The hash table data.
        // This cannot be serialised
        private struct bucket
        {
            public Object key;
            public Object val;
            public int hash_coll;   // Store hash code; sign bit means there was a collision.
        }
#if !NETCORE
        private static int sizeOfReference = System.Runtime.InteropServices.Marshal.SizeOf(typeof(bucket));
#elif NETCORE
        private static int sizeOfReference = 24; 
#endif
        private static int lengthThreshold = (81920 / sizeOfReference);
        private int bucketCount;
        private bucket[][] buckets;

        // The total number of entries in the hash table.
        private int count;

        // The total number of collision bits set in the hashtable
        private int occupancy;

        private int loadsize;
        private float loadFactor;

        private volatile int version;
        private volatile bool isWriterInProgress;

        private ICollection keys;
        private ICollection values;

        private IEqualityComparer _keycomparer;
        private Object _syncRoot;
#if NETCORE
        private SerializationInfo _serializationInfo = null;
#endif

        [Obsolete("Please use EqualityComparer property.")]
        protected IHashCodeProvider hcp
        {
            get
            {
                if (_keycomparer is CompatibleComparer)
                {
                    return ((CompatibleComparer)_keycomparer).HashCodeProvider;
                }
                else if (_keycomparer == null)
                {
                    return null;
                }
                else
                {
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
                }
            }
            set
            {
                if (_keycomparer is CompatibleComparer)
                {
                    CompatibleComparer keyComparer = (CompatibleComparer)_keycomparer;
                    _keycomparer = new CompatibleComparer(keyComparer.Comparer, value);
                }
                else if (_keycomparer == null)
                {
                    _keycomparer = new CompatibleComparer((IComparer)null, value);
                }
                else
                {
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
                }
            }
        }


        [Obsolete("Please use KeyComparer properties.")]
        protected IComparer comparer
        {
            get
            {
                if (_keycomparer is CompatibleComparer)
                {
                    return ((CompatibleComparer)_keycomparer).Comparer;
                }
                else if (_keycomparer == null)
                {
                    return null;
                }
                else
                {
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
                }
            }
            set
            {
                if (_keycomparer is CompatibleComparer)
                {
                    CompatibleComparer keyComparer = (CompatibleComparer)_keycomparer;
                    _keycomparer = new CompatibleComparer(value, keyComparer.HashCodeProvider);
                }
                else if (_keycomparer == null)
                {
                    _keycomparer = new CompatibleComparer(value, (IHashCodeProvider)null);
                }
                else
                {
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_CannotMixComparisonInfrastructure"));
                }
            }
        }

        protected IEqualityComparer EqualityComparer
        {
            get
            {
                return _keycomparer;
            }
        }

        // Note: this constructor is a bogus constructor that does nothing
        // and is for use only with SyncHashtable.
        internal HashVector(bool trash)
        {
        }

        // Constructs a new hashtable. The hashtable is created with an initial
        // capacity of zero and a load factor of 1.0.
        public HashVector()
            : this(0, 1.0f)
        {
        }

        // Constructs a new hashtable with the given initial capacity and a load
        // factor of 1.0. The capacity argument serves as an indication of
        // the number of entries the hashtable will contain. When this number (or
        // an approximation) is known, specifying it in the constructor can
        // eliminate a number of resizing operations that would otherwise be
        // performed when elements are added to the hashtable.
        // 
        public HashVector(int capacity)
            : this(capacity, 1.0f)
        {
        }

        // Constructs a new hashtable with the given initial capacity and load
        // factor. The capacity argument serves as an indication of the
        // number of entries the hashtable will contain. When this number (or an
        // approximation) is known, specifying it in the constructor can eliminate
        // a number of resizing operations that would otherwise be performed when
        // elements are added to the hashtable. The loadFactor argument
        // indicates the maximum ratio of hashtable entries to hashtable buckets.
        // Smaller load factors cause faster average lookup times at the cost of
        // increased memory consumption. A load factor of 1.0 generally provides
        // the best balance between speed and size.
        // 
        public HashVector(int capacity, float loadFactor)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (!(loadFactor >= 0.1f && loadFactor <= 1.0f))
                throw new ArgumentOutOfRangeException("loadFactor", ResourceHelper.GetResourceString("ArgumentOutOfRange_HashtableLoadFactor"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            // Based on perf work, .72 is the optimal load factor for this table.  
            this.loadFactor = 0.72f * loadFactor;

            double rawsize = capacity / this.loadFactor;
            if (rawsize > Int32.MaxValue)
                throw new ArgumentException(ResourceHelper.GetResourceString("Arg_HTCapacityOverflow"));

            // Avoid awfully small sizes
            int hashsize = (rawsize > InitialSize) ? HashHelpers.GetPrime((int)rawsize) : InitialSize;
            int superSize = hashsize / lengthThreshold + 1;
            buckets = new bucket[superSize][];
            bucketCount = hashsize;
            for (int i = 0; i < superSize; i++)
            {
                buckets[i] = new bucket[hashsize < lengthThreshold ? hashsize : lengthThreshold];
                hashsize -= lengthThreshold;
            }

            loadsize = (int)(this.loadFactor * bucketCount);
            isWriterInProgress = false;
            // Based on the current algorithm, loadsize must be less than hashsize.
            // Contract.Assert(loadsize < hashsize, "Invalid hashtable loadsize!");
        }

        // Constructs a new hashtable with the given initial capacity and load
        // factor. The capacity argument serves as an indication of the
        // number of entries the hashtable will contain. When this number (or an
        // approximation) is known, specifying it in the constructor can eliminate
        // a number of resizing operations that would otherwise be performed when
        // elements are added to the hashtable. The loadFactor argument
        // indicates the maximum ratio of hashtable entries to hashtable buckets.
        // Smaller load factors cause faster average lookup times at the cost of
        // increased memory consumption. A load factor of 1.0 generally provides
        // the best balance between speed and size.  The hcp argument
        // is used to specify an Object that will provide hash codes for all
        // the Objects in the table.  Using this, you can in effect override
        // GetHashCode() on each Object using your own hash function.  The 
        // comparer argument will let you specify a custom function for
        // comparing keys.  By specifying user-defined objects for hcp
        // and comparer, users could make a hash table using strings
        // as keys do case-insensitive lookups.
        // 
        [Obsolete("Please use Hashtable(int, float, IEqualityComparer) instead.")]
        public HashVector(int capacity, float loadFactor, IHashCodeProvider hcp, IComparer comparer)
            : this(capacity, loadFactor)
        {
            if (hcp == null && comparer == null)
            {
                this._keycomparer = null;
            }
            else
            {
                this._keycomparer = new CompatibleComparer(comparer, hcp);
            }
        }

        public HashVector(int capacity, float loadFactor, IEqualityComparer equalityComparer)
            : this(capacity, loadFactor)
        {
            this._keycomparer = equalityComparer;
        }

        // Constructs a new hashtable using a custom hash function
        // and a custom comparison function for keys.  This will enable scenarios
        // such as doing lookups with case-insensitive strings.
        // 
        [Obsolete("Please use Hashtable(IEqualityComparer) instead.")]
        public HashVector(IHashCodeProvider hcp, IComparer comparer)
            : this(0, 1.0f, hcp, comparer)
        {
        }

        public HashVector(IEqualityComparer equalityComparer)
            : this(0, 1.0f, equalityComparer)
        {
        }

        // Constructs a new hashtable using a custom hash function
        // and a custom comparison function for keys.  This will enable scenarios
        // such as doing lookups with case-insensitive strings.
        // 
        [Obsolete("Please use Hashtable(int, IEqualityComparer) instead.")]
        public HashVector(int capacity, IHashCodeProvider hcp, IComparer comparer)
            : this(capacity, 1.0f, hcp, comparer)
        {
        }

        public HashVector(int capacity, IEqualityComparer equalityComparer)
            : this(capacity, 1.0f, equalityComparer)
        {
        }

        // Constructs a new hashtable containing a copy of the entries in the given
        // dictionary. The hashtable is created with a load factor of 1.0.
        // 
        public HashVector(IDictionary d)
            : this(d, 1.0f)
        {
        }

        // Constructs a new hashtable containing a copy of the entries in the given
        // dictionary. The hashtable is created with the given load factor.
        // 
        public HashVector(IDictionary d, float loadFactor)
            : this(d, loadFactor, (IEqualityComparer)null)
        {
        }

        [Obsolete("Please use Hashtable(IDictionary, IEqualityComparer) instead.")]
        public HashVector(IDictionary d, IHashCodeProvider hcp, IComparer comparer)
            : this(d, 1.0f, hcp, comparer)
        {
        }

        public HashVector(IDictionary d, IEqualityComparer equalityComparer)
            : this(d, 1.0f, equalityComparer)
        {
        }

        [Obsolete("Please use Hashtable(IDictionary, float, IEqualityComparer) instead.")]
        public HashVector(IDictionary d, float loadFactor, IHashCodeProvider hcp, IComparer comparer)
            : this((d != null ? d.Count : 0), loadFactor, hcp, comparer)
        {
            if (d == null)
                throw new ArgumentNullException("d", ResourceHelper.GetResourceString("ArgumentNull_Dictionary"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            IDictionaryEnumerator e = d.GetEnumerator();
            while (e.MoveNext()) Add(e.Key, e.Value);
        }

        public HashVector(IDictionary d, float loadFactor, IEqualityComparer equalityComparer)
            : this((d != null ? d.Count : 0), loadFactor, equalityComparer)
        {
            if (d == null)
                throw new ArgumentNullException("d", ResourceHelper.GetResourceString("ArgumentNull_Dictionary"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            IDictionaryEnumerator e = d.GetEnumerator();
            while (e.MoveNext()) Add(e.Key, e.Value);
        }

        protected HashVector(SerializationInfo info, StreamingContext context)
        {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a reasonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
#if !NETCORE
            HashHelpers.SerializationInfoTable.Add(this, info);
#elif NETCORE
            _serializationInfo = info;
#endif
        }

        // ‘InitHash’ is basically an implementation of classic DoubleHashing (see http://en.wikipedia.org/wiki/Double_hashing)  
        //
        // 1) The only ‘correctness’ requirement is that the ‘increment’ used to probe 
        //    a. Be non-zero
        //    b. Be relatively prime to the table size ‘hashSize’. (This is needed to insure you probe all entries in the table before you ‘wrap’ and visit entries already probed)
        // 2) Because we choose table sizes to be primes, we just need to insure that the increment is 0 < incr < hashSize
        //
        // Thus this function would work: Incr = 1 + (seed % (hashSize-1))
        // 
        // While this works well for ‘uniformly distributed’ keys, in practice, non-uniformity is common. 
        // In particular in practice we can see ‘mostly sequential’ where you get long clusters of keys that ‘pack’. 
        // To avoid bad behavior you want it to be the case that the increment is ‘large’ even for ‘small’ values (because small 
        // values tend to happen more in practice). Thus we multiply ‘seed’ by a number that will make these small values
        // bigger (and not hurt large values). We picked HashPrime (101) because it was prime, and if ‘hashSize-1’ is not a multiple of HashPrime
        // (enforced in GetPrime), then incr has the potential of being every value from 1 to hashSize-1. The choice was largely arbitrary.
        // 
        // Computes the hash function:  H(key, i) = h1(key) + i*h2(key, hashSize).
        // The out parameter seed is h1(key), while the out parameter 
        // incr is h2(key, hashSize).  Callers of this function should 
        // add incr each time through a loop.
        private uint InitHash(Object key, int hashsize, out uint seed, out uint incr)
        {
            // Hashcode must be positive.  Also, we must not use the sign bit, since
            // that is used for the collision bit.
            uint hashcode = (uint)GetHash(key) & 0x7FFFFFFF;
            seed = (uint)hashcode;
            // Restriction: incr MUST be between 1 and hashsize - 1, inclusive for
            // the modular arithmetic to work correctly.  This guarantees you'll
            // visit every bucket in the table exactly once within hashsize 
            // iterations.  Violate this and it'll cause obscure bugs forever.
            // If you change this calculation for h2(key), update putEntry too!
            incr = (uint)(1 + ((seed * HashPrime) % ((uint)hashsize - 1)));
            return hashcode;
        }

        // Adds an entry with the given key and value to this hashtable. An
        // ArgumentException is thrown if the key is null or if the key is already
        // present in the hashtable.
        // 
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual void Add(Object key, Object value)
        {
            Insert(key, value, true);
        }

        // Removes all entries from this hashtable.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public virtual void Clear()
        {
#if DEBUG
            Contract.Assert(!isWriterInProgress, "Race condition detected in usages of Hashtable - multiple threads appear to be writing to a Hashtable instance simultaneously!  Don't do that - use Hashtable.Synchronized.");
#endif
            if (count == 0 && occupancy == 0)
                return;

#if !FEATURE_CORECLR
            Thread.BeginCriticalRegion();
#endif
            isWriterInProgress = true;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (int j = 0; j < buckets[i].Length; j++)
                {
                    buckets[i][j].hash_coll = 0;
                    buckets[i][j].key = null;
                    buckets[i][j].val = null;
                }
            }


            count = 0;
            occupancy = 0;
            UpdateVersion();
            isWriterInProgress = false;
            contractIfNeeded();
#if !FEATURE_CORECLR
            Thread.EndCriticalRegion();
#endif
        }

        // Clone returns a virtually identical copy of this hash table.  This does
        // a shallow copy - the Objects in the table aren't cloned, only the references
        // to those Objects.
        public virtual Object Clone()
        {
            bucket[][] lbuckets = buckets;
            HashVector ht = new HashVector(count, _keycomparer);
            ht.version = version;
            ht.loadFactor = loadFactor;
            ht.count = 0;

            int superBucket = lbuckets.Length;
            while (superBucket > 0)
            {
                superBucket--;
                int bucket = lbuckets[superBucket].Length;
                while (bucket > 0)
                {
                    bucket--;
                    Object keyv = lbuckets[superBucket][bucket].key;
                    if ((keyv != null) && (keyv != lbuckets))
                    {
                        ht[keyv] = lbuckets[superBucket][bucket].val;
                    }
                }
            }
            return ht;
        }

        // Checks if this hashtable contains the given key.
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual bool Contains(Object key)
        {
            return ContainsKey(key);
        }

        // Checks if this hashtable contains an entry with the given key.  This is
        // an O(1) operation.
        // 
        public virtual bool ContainsKey(Object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key", ResourceHelper.GetResourceString("ArgumentNull_Key"));
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            uint seed;
            uint incr;
            // Take a snapshot of buckets, in case another thread resizes table
            bucket[][] lbuckets = buckets;
            uint hashcode = InitHash(key, bucketCount, out seed, out incr);
            int ntry = 0;

            bucket b;
            int bucketNumber = (int)(seed % (uint)bucketCount);
            do
            {
                b = lbuckets[bucketNumber / lengthThreshold][bucketNumber % lengthThreshold];
                if (b.key == null)
                {
                    return false;
                }
                if (((b.hash_coll & 0x7FFFFFFF) == hashcode) &&
                    KeyEquals(b.key, key))
                    return true;
                bucketNumber = (int)(((long)bucketNumber + incr) % (uint)bucketCount);
            } while (b.hash_coll < 0 && ++ntry < bucketCount);
            return false;
        }



        // Checks if this hashtable contains an entry with the given value. The
        // values of the entries of the hashtable are compared to the given value
        // using the Object.Equals method. This method performs a linear
        // search and is thus be substantially slower than the ContainsKey
        // method.
        // 
        public virtual bool ContainsValue(Object value)
        {

            if (value == null)
            {
                for (int i = buckets.Length; --i >= 0; )
                {
                    for (int j = buckets[i].Length; --j >= 0; )
                    {
                        if (buckets[i][j].key != null && buckets[i][j].key != buckets && buckets[i][j].val == null)
                            return true;
                    }
                }
            }
            else
            {
                for (int i = buckets.Length; --i >= 0; )
                {
                    for (int j = buckets[i].Length; --j >= 0; )
                    {
                        Object val = buckets[i][j].val;
                        if (val != null && val.Equals(value)) return true;
                    }
                }
            }
            return false;
        }

        // Copies the keys of this hashtable to a given array starting at a given
        // index. This method is used by the implementation of the CopyTo method in
        // the KeyCollection class.
        private void CopyKeys(Array array, int arrayIndex)
        {
#if DEBUG
            Contract.Requires(array != null);
            Contract.Requires(array.Rank == 1);
#endif
            bucket[][] lbuckets = buckets;
            for (int i = lbuckets.Length; --i >= 0; )
            {
                for (int j = buckets[i].Length; --j >= 0; )
                {
                    Object keyv = lbuckets[i][j].key;
                    if ((keyv != null) && (keyv != buckets))
                    {
                        array.SetValue(keyv, arrayIndex++);
                    }
                }
            }
        }

        // Copies the keys of this hashtable to a given array starting at a given
        // index. This method is used by the implementation of the CopyTo method in
        // the KeyCollection class.
        private void CopyEntries(Array array, int arrayIndex)
        {
#if DEBUG
            Contract.Requires(array != null);
            Contract.Requires(array.Rank == 1);
#endif
            bucket[][] lbuckets = buckets;
            for (int i = lbuckets.Length; --i >= 0; )
            {
                for (int j = buckets[i].Length; --j >= 0; )
                {
                    Object keyv = lbuckets[i][j].key;
                    if ((keyv != null) && (keyv != buckets))
                    {
                        DictionaryEntry entry = new DictionaryEntry(keyv, lbuckets[i][j].val);
                        array.SetValue(entry, arrayIndex++);
                    }
                }
            }
        }

        // Copies the values in this hash table to an array at
        // a given index.  Note that this only copies values, and not keys.
        public virtual void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array", ResourceHelper.GetResourceString("ArgumentNull_Array"));
            if (array.Rank != 1)
                throw new ArgumentException(ResourceHelper.GetResourceString("Arg_RankMultiDimNotSupported"));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(ResourceHelper.GetResourceString("Arg_ArrayPlusOffTooSmall"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            CopyEntries(array, arrayIndex);
        }

        // Copies the values in this Hashtable to an KeyValuePairs array.
        // KeyValuePairs is different from Dictionary Entry in that it has special
        // debugger attributes on its fields.

        internal virtual KeyValuePairs[] ToKeyValuePairsArray()
        {

            KeyValuePairs[] array = new KeyValuePairs[count];
            int index = 0;
            bucket[][] lbuckets = buckets;
            for (int i = lbuckets.Length; --i >= 0; )
            {
                for (int j = buckets[i].Length; --j >= 0; )
                {
                    Object keyv = lbuckets[i][j].key;
                    if ((keyv != null) && (keyv != buckets))
                    {
                        array[index++] = new KeyValuePairs(keyv, lbuckets[i][j].val);
                    }
                }
            }

            return array;
        }


        // Copies the values of this hashtable to a given array starting at a given
        // index. This method is used by the implementation of the CopyTo method in
        // the ValueCollection class.
        private void CopyValues(Array array, int arrayIndex)
        {
#if DEBUG
            Contract.Requires(array != null);
            Contract.Requires(array.Rank == 1);
#endif
            bucket[][] lbuckets = buckets;
            for (int i = lbuckets.Length; --i >= 0; )
            {
                for (int j = buckets[i].Length; --j >= 0; )
                {
                    Object keyv = lbuckets[i][j].key;
                    if ((keyv != null) && (keyv != buckets))
                    {
                        array.SetValue(lbuckets[i][j].val, arrayIndex++);
                    }
                }
            }
        }

        // Returns the value associated with the given key. If an entry with the
        // given key is not found, the returned value is null.
        // 
        public virtual Object this[Object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key", ResourceHelper.GetResourceString("ArgumentNull_Key"));
                }
#if DEBUG
                Contract.EndContractBlock();
#endif
                uint seed;
                uint incr;


                // Take a snapshot of buckets, in case another thread does a resize
                bucket[][] lbuckets = buckets;
                uint hashcode = InitHash(key, bucketCount, out seed, out incr);
                int ntry = 0;

                bucket b;
                int bucketNumber = (int)(seed % (uint)bucketCount);
                do
                {
                    int currentversion;

                    //     A read operation on hashtable has three steps:
                    //        (1) calculate the hash and find the slot number.
                    //        (2) compare the hashcode, if equal, go to step 3. Otherwise end.
                    //        (3) compare the key, if equal, go to step 4. Otherwise end.
                    //        (4) return the value contained in the bucket.
                    //     After step 3 and before step 4. A writer can kick in a remove the old item and add a new one 
                    //     in the same bukcet. So in the reader we need to check if the hash table is modified during above steps.
                    //
                    // Writers (Insert, Remove, Clear) will set 'isWriterInProgress' flag before it starts modifying 
                    // the hashtable and will ckear the flag when it is done.  When the flag is cleared, the 'version'
                    // will be increased.  We will repeat the reading if a writer is in progress or done with the modification 
                    // during the read.
                    //
                    // Our memory model guarantee if we pick up the change in bucket from another processor, 
                    // we will see the 'isWriterProgress' flag to be true or 'version' is changed in the reader.
                    //                    
                    int spinCount = 0;
                    do
                    {
                        // this is violate read, following memory accesses can not be moved ahead of it.
                        currentversion = version;
                        b = lbuckets[bucketNumber / lengthThreshold][bucketNumber % lengthThreshold];

                        // The contention between reader and writer shouldn't happen frequently.
                        // But just in case this will burn CPU, yield the control of CPU if we spinned a few times.
                        // 8 is just a random number I pick. 
                        if ((++spinCount) % 8 == 0)
                        {
                            Thread.Sleep(1);   // 1 means we are yeilding control to all threads, including low-priority ones.
                        }
                    } while (isWriterInProgress || (currentversion != version));

                    if (b.key == null)
                    {
                        return null;
                    }
                    if (((b.hash_coll & 0x7FFFFFFF) == hashcode) &&
                        KeyEquals(b.key, key))
                        return b.val;
                    bucketNumber = (int)(((long)bucketNumber + incr) % (uint)bucketCount);
                } while (b.hash_coll < 0 && ++ntry < bucketCount);
                return null;
            }

#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            set
            {
                Insert(key, value, false);
            }
        }

        // Increases the bucket count of this hashtable. This method is called from
        // the Insert method when the actual load factor of the hashtable reaches
        // the upper limit specified when the hashtable was constructed. The number
        // of buckets in the hashtable is increased to the smallest prime number
        // that is larger than twice the current number of buckets, and the entries
        // in the hashtable are redistributed into the new buckets using the cached
        // hashcodes.
        private void expand()
        {
            int rawsize = HashHelpers.ExpandPrime(bucketCount);
            rehash(rawsize, false);
        }

        private void contractIfNeeded()
        {
            // int targetLoadSize = Convert.ToInt32(count / loadFactor);

            // Explicit casting is more than 2x faster than the Convert.ToInt32(), in this
            // case we can afford to truncate the number. 
            int targetLoadSize = (int)(count/loadFactor);

            if (loadsize >= 3 * targetLoadSize)
            {
                targetLoadSize = HashHelpers.ExpandPrime(targetLoadSize);
                rehash(targetLoadSize, false);
            }
        }

        public int BucketCount
        {
            get { return bucketCount; }
        }

        public int ReferenceSize
        {
            get { return sizeOfReference; }
        }

        // We occationally need to rehash the table to clean up the collision bits.
        private void rehash()
        {
            rehash(bucketCount, false);
        }

        private void UpdateVersion()
        {
            // Version might become negative when version is Int32.MaxValue, but the oddity will be still be correct. 
            // So we don't need to special case this. 
            version++;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private void rehash(int newsize, bool forceNewHashCode)
        {

            // reset occupancy
            occupancy = 0;

            // Don't replace any internal state until we've finished adding to the 
            // new bucket[].  This serves two purposes: 
            //   1) Allow concurrent readers to see valid hashtable contents 
            //      at all times
            //   2) Protect against an OutOfMemoryException while allocating this 
            //      new bucket[].
            int superIndex = newsize / lengthThreshold + 1;
            bucket[][] newBuckets = new bucket[superIndex][];
            int counter = newsize;
            for (int i = 0; i < superIndex; i++)
            {
                newBuckets[i] = new bucket[counter < lengthThreshold ? counter : lengthThreshold];
                counter -= lengthThreshold;
            }

            // rehash table into new buckets
            int nb;
            for (nb = 0; nb < bucketCount; nb++)
            {
                bucket oldb = buckets[nb / lengthThreshold][nb % lengthThreshold];
                if ((oldb.key != null) && (oldb.key != buckets))
                {
                    int hashcode = ((forceNewHashCode ? GetHash(oldb.key) : oldb.hash_coll) & 0x7FFFFFFF);
                    putEntry(newBuckets, newsize, oldb.key, oldb.val, hashcode);
                }
            }

            // New bucket[] is good to go - replace buckets and other internal state.
#if !FEATURE_CORECLR
            Thread.BeginCriticalRegion();
#endif
            isWriterInProgress = true;
            buckets = newBuckets;
            bucketCount = newsize;
            loadsize = (int)(loadFactor * newsize);
            UpdateVersion();
            isWriterInProgress = false;
#if !FEATURE_CORECLR
            Thread.EndCriticalRegion();
#endif
            // minimun size of hashtable is 3 now and maximum loadFactor is 0.72 now.
#if DEBUG
            Contract.Assert(loadsize < newsize, "Our current implementaion means this is not possible.");
#endif
            return;
        }

        // Returns an enumerator for this hashtable.
        // If modifications made to the hashtable while an enumeration is
        // in progress, the MoveNext and Current methods of the
        // enumerator will throw an exception.
        //
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ImmutableEnumerator(this, ImmutableEnumerator.DictEntry);
        }

        // Returns a dictionary enumerator for this hashtable.
        // If modifications made to the hashtable while an enumeration is
        // in progress, the MoveNext and Current methods of the
        // enumerator will throw an exception.
        //
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return new ImmutableEnumerator(this, ImmutableEnumerator.DictEntry);
        }

        // Internal method to get the hash code for an Object.  This will call
        // GetHashCode() on each object if you haven't provided an IHashCodeProvider
        // instance.  Otherwise, it calls hcp.GetHashCode(obj).
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        protected virtual int GetHash(Object key)
        {
            if (_keycomparer != null)
                return _keycomparer.GetHashCode(key);
            return key.GetHashCode();
        }

        // Is this Hashtable read-only?
        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }

        // Is this Hashtable synchronized?  See SyncRoot property
        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        // Internal method to compare two keys.  If you have provided an IComparer
        // instance in the constructor, this method will call comparer.Compare(item, key).
        // Otherwise, it will call item.Equals(key).
        // 
        protected virtual bool KeyEquals(Object item, Object key)
        {
#if DEBUG
            Contract.Assert(key != null, "key can't be null here!");
#endif
            if (Object.ReferenceEquals(buckets, item))
            {
                return false;
            }

            if (Object.ReferenceEquals(item, key))
                return true;

            if (_keycomparer != null)
                return _keycomparer.Equals(item, key);
            return item == null ? false : item.Equals(key);
        }


        // Inserts an entry into this hashtable. This method is called from the Set
        // and Add methods. If the add parameter is true and the given key already
        // exists in the hashtable, an exception is thrown.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private void Insert(Object key, Object nvalue, bool add)
        {
            // @


            if (key == null)
            {
                throw new ArgumentNullException("key", ResourceHelper.GetResourceString("ArgumentNull_Key"));
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (count >= loadsize)
            {
                expand();
            }
            else if (occupancy > loadsize && count > 100)
            {
                rehash();
            }

            uint seed;
            uint incr;
            // Assume we only have one thread writing concurrently.  Modify
            // buckets to contain new data, as long as we insert in the right order.
            uint hashcode = InitHash(key, bucketCount, out seed, out incr);
            int ntry = 0;
            int emptySlotNumber = -1; // We use the empty slot number to cache the first empty slot. We chose to reuse slots
            // create by remove that have the collision bit set over using up new slots.
            int bucketNumber = (int)(seed % (uint)bucketCount);
            do
            {

                // Set emptySlot number to current bucket if it is the first available bucket that we have seen
                // that once contained an entry and also has had a collision.
                // We need to search this entire collision chain because we have to ensure that there are no 
                // duplicate entries in the table.
                int superIndex = bucketNumber / lengthThreshold;
                int subIndedex = bucketNumber % lengthThreshold;
                if (emptySlotNumber == -1 && (buckets[superIndex][subIndedex].key == buckets) && (buckets[superIndex][subIndedex].hash_coll < 0))//(((buckets[bucketNumber].hash_coll & unchecked(0x80000000))!=0)))
                    emptySlotNumber = bucketNumber;

                // Insert the key/value pair into this bucket if this bucket is empty and has never contained an entry
                // OR
                // This bucket once contained an entry but there has never been a collision
                if ((buckets[superIndex][subIndedex].key == null) ||
                    (buckets[superIndex][subIndedex].key == buckets && ((buckets[superIndex][subIndedex].hash_coll & unchecked(0x80000000)) == 0)))
                {

                    // If we have found an available bucket that has never had a collision, but we've seen an available
                    // bucket in the past that has the collision bit set, use the previous bucket instead
                    if (emptySlotNumber != -1) // Reuse slot
                    {
                        bucketNumber = emptySlotNumber;
                        superIndex = bucketNumber / lengthThreshold;
                        subIndedex = bucketNumber % lengthThreshold;
                    }

                    // We pretty much have to insert in this order.  Don't set hash
                    // code until the value & key are set appropriately.
#if !FEATURE_CORECLR
                    Thread.BeginCriticalRegion();
#endif
                    isWriterInProgress = true;
                    buckets[superIndex][subIndedex].val = nvalue;
                    buckets[superIndex][subIndedex].key = key;
                    buckets[superIndex][subIndedex].hash_coll |= (int)hashcode;
                    count++;
                    UpdateVersion();
                    isWriterInProgress = false;
#if !FEATURE_CORECLR
                    Thread.EndCriticalRegion();
#endif

#if FEATURE_RANDOMIZED_STRING_HASHING
                    if(ntry > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(_keycomparer)) 
                    {
                        // PERF: We don't want to rehash if _keycomparer is already a RandomizedObjectEqualityComparer since in some
                        // cases there may not be any strings in the hashtable and we wouldn't get any mixing.
                        if(_keycomparer == null || !(_keycomparer is System.Collections.Generic.RandomizedObjectEqualityComparer))
                        {
                            _keycomparer = HashHelpers.GetRandomizedEqualityComparer(_keycomparer);
                            rehash(buckets.Length, true);
                        }
                    }
#endif

                    return;
                }

                // The current bucket is in use
                // OR
                // it is available, but has had the collision bit set and we have already found an available bucket
                if (((buckets[superIndex][subIndedex].hash_coll & 0x7FFFFFFF) == hashcode) &&
                    KeyEquals(buckets[superIndex][subIndedex].key, key))
                {
                    if (add)
                    {
                        throw new ArgumentException(ResourceHelper.GetResourceString("Argument_AddingDuplicate__"));
                    }
#if !FEATURE_CORECLR
                    Thread.BeginCriticalRegion();
#endif
                    isWriterInProgress = true;
                    buckets[superIndex][subIndedex].val = nvalue;
                    UpdateVersion();
                    isWriterInProgress = false;
#if !FEATURE_CORECLR
                    Thread.EndCriticalRegion();
#endif

#if FEATURE_RANDOMIZED_STRING_HASHING
                    if(ntry > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(_keycomparer)) 
                    {
                        // PERF: We don't want to rehash if _keycomparer is already a RandomizedObjectEqualityComparer since in some
                        // cases there may not be any strings in the hashtable and we wouldn't get any mixing.
                        if(_keycomparer == null || !(_keycomparer is System.Collections.Generic.RandomizedObjectEqualityComparer))
                        {
                            _keycomparer = HashHelpers.GetRandomizedEqualityComparer(_keycomparer);
                            rehash(buckets.Length, true);
                        }
                    }
#endif
                    return;
                }

                // The current bucket is full, and we have therefore collided.  We need to set the collision bit
                // UNLESS
                // we have remembered an available slot previously.
                if (emptySlotNumber == -1)
                {// We don't need to set the collision bit here since we already have an empty slot
                    if (buckets[superIndex][subIndedex].hash_coll >= 0)
                    {
                        buckets[superIndex][subIndedex].hash_coll |= unchecked((int)0x80000000);
                        occupancy++;
                    }
                }

                bucketNumber = (int)(((long)bucketNumber + incr) % (uint)bucketCount);
            } while (++ntry < bucketCount);

            // This code is here if and only if there were no buckets without a collision bit set in the entire table
            if (emptySlotNumber != -1)
            {
                // We pretty much have to insert in this order.  Don't set hash
                // code until the value & key are set appropriately.
#if !FEATURE_CORECLR
                Thread.BeginCriticalRegion();
#endif
                int superIndex = emptySlotNumber / lengthThreshold;
                int subIndedex = emptySlotNumber % lengthThreshold;
                isWriterInProgress = true;
                buckets[superIndex][subIndedex].val = nvalue;
                buckets[superIndex][subIndedex].key = key;
                buckets[superIndex][subIndedex].hash_coll |= (int)hashcode;
                count++;
                UpdateVersion();
                isWriterInProgress = false;
#if !FEATURE_CORECLR
                Thread.EndCriticalRegion();
#endif

#if FEATURE_RANDOMIZED_STRING_HASHING
                if(buckets.Length > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(_keycomparer)) 
                {
                    // PERF: We don't want to rehash if _keycomparer is already a RandomizedObjectEqualityComparer since in some
                    // cases there may not be any strings in the hashtable and we wouldn't get any mixing.
                    if(_keycomparer == null || !(_keycomparer is System.Collections.Generic.RandomizedObjectEqualityComparer))
                    {
                        _keycomparer = HashHelpers.GetRandomizedEqualityComparer(_keycomparer);
                        rehash(buckets.Length, true);
                    }
                }
#endif
                return;
            }

            // If you see this assert, make sure load factor & count are reasonable.
            // Then verify that our double hash function (h2, described at top of file)
            // meets the requirements described above. You should never see this assert.
#if DEBUG
            Contract.Assert(false, "hash table insert failed!  Load factor too high, or our double hashing function is incorrect.");
#endif
            throw new InvalidOperationException(ResourceHelper.GetResourceString("InvalidOperation_HashInsertFailed"));
        }

        private void putEntry(bucket[][] newBuckets, int newBucketCount, Object key, Object nvalue, int hashcode)
        {
#if DEBUG
            Contract.Assert(hashcode >= 0, "hashcode >= 0");  // make sure collision bit (sign bit) wasn't set.
#endif
            uint seed = (uint)hashcode;
            uint incr = (uint)(1 + ((seed * HashPrime) % ((uint)newBucketCount - 1)));
            int bucketNumber = (int)(seed % (uint)newBucketCount);
            do
            {
                int superIndex = bucketNumber / lengthThreshold;
                int subIndedex = bucketNumber % lengthThreshold;
                if ((newBuckets[superIndex][subIndedex].key == null) || (newBuckets[superIndex][subIndedex].key == buckets))
                {
                    newBuckets[superIndex][subIndedex].val = nvalue;
                    newBuckets[superIndex][subIndedex].key = key;
                    newBuckets[superIndex][subIndedex].hash_coll |= hashcode;
                    return;
                }

                if (newBuckets[superIndex][subIndedex].hash_coll >= 0)
                {
                    newBuckets[superIndex][subIndedex].hash_coll |= unchecked((int)0x80000000);
                    occupancy++;
                }
                bucketNumber = (int)(((long)bucketNumber + incr) % (uint)newBucketCount);
            } while (true);
        }

        // Removes an entry from this hashtable. If an entry with the specified
        // key exists in the hashtable, it is removed. An ArgumentException is
        // thrown if the key is null.
        // 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public virtual void Remove(Object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key", ResourceHelper.GetResourceString("ArgumentNull_Key"));
            }
#if DEBUG
            Contract.EndContractBlock();
            Contract.Assert(!isWriterInProgress, "Race condition detected in usages of Hashtable - multiple threads appear to be writing to a Hashtable instance simultaneously!  Don't do that - use Hashtable.Synchronized.");
#endif
            uint seed;
            uint incr;
            // Assuming only one concurrent writer, write directly into buckets.
            uint hashcode = InitHash(key, bucketCount, out seed, out incr);
            int ntry = 0;

            bucket b;
            int bn = (int)(seed % (uint)bucketCount);  // bucketNumber
            do
            {
                int superIndex = bn / lengthThreshold;
                int subIndedex = bn % lengthThreshold;
                b = buckets[superIndex][subIndedex];
                if (((b.hash_coll & 0x7FFFFFFF) == hashcode) &&
                    KeyEquals(b.key, key))
                {
#if !FEATURE_CORECLR
                    Thread.BeginCriticalRegion();
#endif
                    isWriterInProgress = true;
                    // Clear hash_coll field, then key, then value
                    buckets[superIndex][subIndedex].hash_coll &= unchecked((int)0x80000000);
                    if (buckets[superIndex][subIndedex].hash_coll != 0)
                    {
                        buckets[superIndex][subIndedex].key = buckets;
                    }
                    else
                    {
                        buckets[superIndex][subIndedex].key = null;
                    }
                    buckets[superIndex][subIndedex].val = null;  // Free object references sooner & simplify ContainsValue.
                    count--;
                    UpdateVersion();
                    isWriterInProgress = false;
                    contractIfNeeded();
#if !FEATURE_CORECLR
                    Thread.EndCriticalRegion();
#endif
                    return;
                }
                bn = (int)(((long)bn + incr) % (uint)bucketCount);
            } while (b.hash_coll < 0 && ++ntry < bucketCount);

            //throw new ArgumentException(ResourceHelper.GetResourceString("Arg_RemoveArgNotFound"));
        }

        // Returns the object to synchronize on for this hash table.
        public virtual Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Returns the number of associations in this hashtable.
        // 
        public virtual int Count
        {
            get { return count; }
        }

        //
        // The ISerializable Implementation
        //

        [System.Security.SecurityCritical]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            // This is imperfect - it only works well if all other writes are
            // also using our synchronized wrapper.  But it's still a good idea.
            lock (SyncRoot)
            {
                // This method hasn't been fully tweaked to be safe for a concurrent writer.
                int oldVersion = version;
                info.AddValue(LoadFactorName, loadFactor);
                info.AddValue(VersionName, version);

                //
                // We need to maintain serialization compatibility with Everett and RTM.
                // If the comparer is null or a compatible comparer, serialize Hashtable
                // in a format that can be deserialized on Everett and RTM.            
                //
                // Also, if the Hashtable is using randomized hashing, serialize the old
                // view of the _keycomparer so perevious frameworks don't see the new types
#pragma warning disable 618
#if FEATURE_RANDOMIZED_STRING_HASHING
            IEqualityComparer keyComparerForSerilization = (IEqualityComparer) HashHelpers.GetEqualityComparerForSerialization(_keycomparer);
#else
                IEqualityComparer keyComparerForSerilization = _keycomparer;
#endif

                if (keyComparerForSerilization == null)
                {
                    info.AddValue(ComparerName, null, typeof(IComparer));
                    info.AddValue(HashCodeProviderName, null, typeof(IHashCodeProvider));
                }
                else if (keyComparerForSerilization is CompatibleComparer)
                {
                    CompatibleComparer c = keyComparerForSerilization as CompatibleComparer;
                    info.AddValue(ComparerName, c.Comparer, typeof(IComparer));
                    info.AddValue(HashCodeProviderName, c.HashCodeProvider, typeof(IHashCodeProvider));
                }
                else
                {
                    info.AddValue(KeyComparerName, keyComparerForSerilization, typeof(IEqualityComparer));
                }
#pragma warning restore 618

                info.AddValue(HashSizeName, bucketCount); //This is the length of the bucket array.
                Object[] serKeys = new Object[count];
                Object[] serValues = new Object[count];
                CopyKeys(serKeys, 0);
                CopyValues(serValues, 0);
                info.AddValue(KeysName, serKeys, typeof(Object[]));
                info.AddValue(ValuesName, serValues, typeof(Object[]));

                // Explicitly check to see if anyone changed the Hashtable while we 
                // were serializing it.  That's a ---- in their code.
                if (version != oldVersion)
                    throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
            }
        }

        //
        // DeserializationEvent Listener 
        //
        public virtual void OnDeserialization(Object sender)
        {
            if (buckets != null)
            {
                // Somebody had a dependency on this hashtable and fixed us up before the ObjectManager got to it.
                return;
            }

            SerializationInfo siInfo= null;
#if !NETCORE
            HashHelpers.SerializationInfoTable.TryGetValue(this, out siInfo);
#elif NETCORE
            siInfo = _serializationInfo;
#endif

            if (siInfo == null)
            {
                throw new SerializationException(ResourceHelper.GetResourceString("Serialization_InvalidOnDeser"));
            }

            int hashsize = 0;
            IComparer c = null;

#pragma warning disable 618
            IHashCodeProvider hcp = null;
#pragma warning restore 618

            Object[] serKeys = null;
            Object[] serValues = null;

            SerializationInfoEnumerator enumerator = siInfo.GetEnumerator();

            while (enumerator.MoveNext())
            {
                switch (enumerator.Name)
                {
                    case LoadFactorName:
                        loadFactor = siInfo.GetSingle(LoadFactorName);
                        break;
                    case HashSizeName:
                        hashsize = siInfo.GetInt32(HashSizeName);
                        break;
                    case KeyComparerName:
                        _keycomparer = (IEqualityComparer)siInfo.GetValue(KeyComparerName, typeof(IEqualityComparer));
                        break;
                    case ComparerName:
                        c = (IComparer)siInfo.GetValue(ComparerName, typeof(IComparer));
                        break;
                    case HashCodeProviderName:
#pragma warning disable 618
                        hcp = (IHashCodeProvider)siInfo.GetValue(HashCodeProviderName, typeof(IHashCodeProvider));
#pragma warning restore 618
                        break;
                    case KeysName:
                        serKeys = (Object[])siInfo.GetValue(KeysName, typeof(Object[]));
                        break;
                    case ValuesName:
                        serValues = (Object[])siInfo.GetValue(ValuesName, typeof(Object[]));
                        break;
                }
            }

            loadsize = (int)(loadFactor * hashsize);

            // V1 object doesn't has _keycomparer field.
            if ((_keycomparer == null) && ((c != null) || (hcp != null)))
            {
                _keycomparer = new CompatibleComparer(c, hcp);
            }
            int superSize = hashsize / lengthThreshold + 1;
            buckets = new bucket[superSize][];
            bucketCount = hashsize;
            int counter = hashsize;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new bucket[counter < lengthThreshold ? counter : lengthThreshold];
                counter -= lengthThreshold;
            }

            if (serKeys == null)
            {
                throw new SerializationException(ResourceHelper.GetResourceString("Serialization_MissingKeys"));
            }
            if (serValues == null)
            {
                throw new SerializationException(ResourceHelper.GetResourceString("Serialization_MissingValues"));
            }
            if (serKeys.Length != serValues.Length)
            {
                throw new SerializationException(ResourceHelper.GetResourceString("Serialization_KeyValueDifferentSizes"));
            }
            for (int i = 0; i < serKeys.Length; i++)
            {
                if (serKeys[i] == null)
                {
                    throw new SerializationException(ResourceHelper.GetResourceString("Serialization_NullKey"));
                }
                Insert(serKeys[i], serValues[i], true);
            }

            version = siInfo.GetInt32(VersionName);
#if !NETCORE
            HashHelpers.SerializationInfoTable.Remove(this);
#elif NETCORE
            _serializationInfo = null;
#endif


        }
        // Implements an enumerator for a hashtable. The enumerator uses the
        // internal version number of the hashtabke to ensure that no modifications
        // are made to the hashtable while an enumeration is in progress.
        [Serializable]
        private class HashVectorEnumerator : IDictionaryEnumerator, ICloneable
        {
            private HashVector hashtable;
            private int bucket;
            private int version;
            private bool current;
            private int getObjectRetType;   // What should GetObject return?
            private Object currentKey;
            private Object currentValue;

            internal const int Keys = 1;
            internal const int Values = 2;
            internal const int DictEntry = 3;

            internal HashVectorEnumerator(HashVector hashtable, int getObjRetType)
            {
                this.hashtable = hashtable;
                bucket = hashtable.bucketCount;
                version = hashtable.version;
                current = false;
                getObjectRetType = getObjRetType;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public virtual Object Key
            {
#if DEBUG
                [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
                get
                {
                    if (current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    return currentKey;
                }
            }

            public virtual bool MoveNext()
            {
                if (version != hashtable.version) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                while (bucket > 0)
                {
                    bucket--;
                    int superIndex = bucket / lengthThreshold;
                    int subindex = bucket % lengthThreshold;

                    Object keyv = hashtable.buckets[superIndex][subindex].key;
                    if ((keyv != null) && (keyv != hashtable.buckets))
                    {
                        currentKey = keyv;
                        currentValue = hashtable.buckets[superIndex][subindex].val;
                        current = true;
                        return true;
                    }
                }
                current = false;
                return false;
            }

            public virtual DictionaryEntry Entry
            {
                get
                {
                    if (current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumOpCantHappen));
                    return new DictionaryEntry(currentKey, currentValue);
                }
            }


            public virtual Object Current
            {
                get
                {
                    if (current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumOpCantHappen));

                    if (getObjectRetType == Keys)
                        return currentKey;
                    else if (getObjectRetType == Values)
                        return currentValue;
                    else
                        return new DictionaryEntry(currentKey, currentValue);
                }
            }

            public virtual Object Value
            {
#if DEBUG
                [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
                get
                {
                    if (current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumOpCantHappen));
                    return currentValue;
                }
            }

            public virtual void Reset()
            {
                if (version != hashtable.version) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                current = false;
                bucket = hashtable.bucketCount;
                currentKey = null;
                currentValue = null;
            }
        }


        public virtual ICollection Keys
        {
            get
            {
                if (keys == null) keys = new VectorKeyCollection(this);
                return keys;
            }
        }

        public virtual ICollection Values
        {
            get
            {
                if (values == null) values = new VectorValueCollection(this);
                return values;
            }
        }

        public static HashVector Synchronized(HashVector vector)
        {
            return new SyncHashVector(vector);
        }

        [Serializable]
        private class VectorKeyCollection : ICollection
        {
            private HashVector _hashVector;

            internal VectorKeyCollection(HashVector hashVector)
            {
                _hashVector = hashVector;
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (array.Rank != 1)
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_RankMultiDimNotSupported"));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException("arrayIndex", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#if DEBUG
                Contract.EndContractBlock();
#endif
                if (array.Length - arrayIndex < _hashVector.count)
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_ArrayPlusOffTooSmall"));
                _hashVector.CopyKeys(array, arrayIndex);
            }

            public int Count
            {
                get { return _hashVector.Count; }
            }

            public bool IsSynchronized
            {
                get { return _hashVector.IsSynchronized; }
            }

            public object SyncRoot
            {
                get { return _hashVector.SyncRoot; }
            }

            public IEnumerator GetEnumerator()
            {
                return new ImmutableEnumerator(_hashVector, ImmutableEnumerator.Keys);
            }
        }

        [Serializable]
        private class VectorValueCollection : ICollection
        {
            private HashVector _hashVector;

            internal VectorValueCollection(HashVector hashVector)
            {
                _hashVector = hashVector;
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (array.Rank != 1)
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_RankMultiDimNotSupported"));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException("arrayIndex", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#if DEBUG
                Contract.EndContractBlock();
#endif
                if (array.Length - arrayIndex < _hashVector.count)
                    throw new ArgumentException(ResourceHelper.GetResourceString("Arg_ArrayPlusOffTooSmall"));
                _hashVector.CopyValues(array, arrayIndex);
            }

            public int Count
            {
                get { return _hashVector.Count; }
            }

            public bool IsSynchronized
            {
                get { return _hashVector.IsSynchronized; }
            }

            public object SyncRoot
            {
                get { return _hashVector.SyncRoot; }
            }

            public IEnumerator GetEnumerator()
            {
                return new ImmutableEnumerator(_hashVector, ImmutableEnumerator.Values);
            }
        }

        [Serializable]
        private class SyncHashVector : HashVector, IEnumerable
        {
            protected HashVector _vector;

            internal SyncHashVector(HashVector vector)
                : base(false)
            {
                _vector = vector;
            }

            [System.Security.SecurityCritical]  // auto-generated
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("info");
                }
#if DEBUG
                Contract.EndContractBlock();
#endif
                // Our serialization code hasn't been fully tweaked to be safe 
                // for a concurrent writer.
                lock (_vector.SyncRoot)
                {
                    info.AddValue("ParentTable", _vector, typeof(HashVector));
                }
            }

            public override void OnDeserialization(Object sender)
            {
                return;
            }

            public override int Count
            {
                get
                {
                    return _vector.count;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return _vector.IsReadOnly;
                }
            }

            public override bool IsFixedSize
            {
                get
                {
                    return _vector.IsFixedSize;
                }
            }

            public override bool IsSynchronized
            {
                get
                {
                    return true;
                }
            }

            public override object this[object key]
            {
                get
                {
                    return _vector[key];
                }
                set
                {
                    lock (_vector.SyncRoot)
                    {
                        _vector[key] = value;
                    }
                }
            }

            public override object SyncRoot
            {
                get
                {
                    return _vector.SyncRoot;
                }
            }

            public override void Add(Object key, Object value)
            {
                lock (_vector.SyncRoot)
                {
                    _vector.Add(key, value);
                }
            }

            public override void Clear()
            {
                lock (_vector.SyncRoot)
                {
                    _vector.Clear();
                }
            }

#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            public override bool Contains(Object key)
            {
                return _vector.Contains(key);
            }

#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            public override bool ContainsKey(Object key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key", ResourceHelper.GetResourceString("ArgumentNull_Key"));
                }
#if DEBUG
                Contract.EndContractBlock();
#endif
                return _vector.ContainsKey(key);
            }



            public override bool ContainsValue(Object key)
            {
                lock (_vector.SyncRoot)
                {
                    return _vector.ContainsValue(key);
                }
            }

            public override void CopyTo(Array array, int arrayIndex)
            {
                lock (_vector.SyncRoot)
                {
                    _vector.CopyTo(array, arrayIndex);
                }
            }

            public override Object Clone()
            {
                lock (_vector.SyncRoot)
                {
                    return Hashtable.Synchronized((Hashtable)_vector.Clone());
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _vector.GetEnumerator();
            }

            public override IDictionaryEnumerator GetEnumerator()
            {
                return _vector.GetEnumerator();
            }

            public override ICollection Keys
            {
                get
                {
                    lock (_vector.SyncRoot)
                    {
                        return _vector.Keys;
                    }
                }
            }

            public override ICollection Values
            {
                get
                {
                    lock (_vector.SyncRoot)
                    {
                        return _vector.Values;
                    }
                }
            }

            public override void Remove(Object key)
            {
                lock (_vector.SyncRoot)
                {
                    _vector.Remove(key);
                }
            }

            internal override KeyValuePairs[] ToKeyValuePairsArray()
            {
                return _vector.ToKeyValuePairsArray();
            }

        }



        public long IndexInMemorySize
        {
            get
            {
                return this.bucketCount * sizeOfReference;
            }
        }

        // Implements an enumerator for a hashtable. The enumerator uses the
        // internal version number of the hashtabke to ensure that no modifications
        // are made to the hashtable while an enumeration is in progress.
        [Serializable]
        private sealed class ImmutableEnumerator : IDictionaryEnumerator, ICloneable
        {
            private readonly bucket[][] _buckets;
            private int _bucket;
            private bool _current;
            private readonly int _bucketCount;
            private readonly int _getObjectRetType;   // What should GetObject return?
            private object _currentKey;
            private object _currentValue;

            internal const int Keys = 1;
            internal const int Values = 2;
            internal const int DictEntry = 3;

            internal ImmutableEnumerator(HashVector hashtable, int getObjRetType)
            {
                _buckets = hashtable.buckets;
                _bucket = hashtable.bucketCount;
                _bucketCount = hashtable.bucketCount;
                _current = false;
                _getObjectRetType = getObjRetType;
            }

            public object Clone()
            {
                return MemberwiseClone();
            }

            public object Key
            {
#if DEBUG
                [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
                get
                {
                    if (_current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    return _currentKey;
                }
            }

            public bool MoveNext()
            {
                while (_bucket > 0)
                {
                    _bucket--;
                    int superIndex = _bucket / lengthThreshold;
                    int subindex = _bucket % lengthThreshold;

                    object keyv = _buckets[superIndex][subindex].key;
                    if ((keyv != null) && (keyv != _buckets))
                    {
                        _currentKey = keyv;
                        _currentValue = _buckets[superIndex][subindex].val;
                        _current = true;
                        return true;
                    }
                }
                _current = false;
                return false;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if (_current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumOpCantHappen));
                    return new DictionaryEntry(_currentKey, _currentValue);
                }
            }

            public object Current
            {
                get
                {
                    if (_current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumOpCantHappen));

                    if (_getObjectRetType == Keys)
                        return _currentKey;
                    if (_getObjectRetType == Values)
                        return _currentValue;
                    return new DictionaryEntry(_currentKey, _currentValue);
                }
            }

            public object Value
            {
#if DEBUG
                [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
                get
                {
                    if (_current == false) throw new InvalidOperationException(ResourceHelper.GetResourceString(ResId.InvalidOperation_EnumOpCantHappen));
                    return _currentValue;
                }
            }

            public void Reset()
            {
                _current = false;
                _bucket = _bucketCount;
                _currentKey = null;
                _currentValue = null;
            }
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
#if DEBUG
    [DebuggerTypeProxy(typeof(VectorDebugView))]
#endif
    [System.Runtime.InteropServices.ComVisible(false)]
    public class HashVector<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, ISerializable, IDeserializationCallback,ITransactableStore
    {
        private struct Entry : IMemSizable
        {
            public int hashCode;    // Lower 31 bits of hash code, -1 if unused
            public int next;        // Index of next entry, -1 if last
            public TKey key;           // Key of entry
            public TValue value;         // Value of entry

            public int Size
            {
                get
                {
                    int size = 0;
                    size += 2 * sizeof(int);
                    Type genericType = typeof(TKey);
                    if (genericType.IsValueType)
                    {
                        size += System.Runtime.InteropServices.Marshal.SizeOf(genericType);
                    }
                    else
                        size += IntPtr.Size;

                    genericType = typeof(TValue);
                    if (genericType.IsValueType)
                    {
                        size += System.Runtime.InteropServices.Marshal.SizeOf(genericType);
                    }
                    else
                        size += IntPtr.Size;
                    return size;

                }
            }
        }

        private ClusteredArray<int> buckets;
        private ClusteredArray<Entry> entries;
        private int count;
        private int version;
        private int freeList;
        private int freeCount;
        private IEqualityComparer<TKey> comparer;
        private VectorKeyCollection keys;
        private VectorValueCollection values;
        private Object _syncRoot;
#if NETCORE
        private SerializationInfo _serializationInfo = null;
#endif

        // constants for serialization
        private const String VersionName = "Version";
        private const String HashSizeName = "HashSize";  // Must save buckets.Length
        private const String KeyValuePairsName = "KeyValuePairs";
        private const String ComparerName = "Comparer";
        [NonSerialized]
        private Transaction _transaction;

        #region /*         Transactions */
        public bool BeginTransaction()
        {
            if (_transaction == null)
                _transaction = new Transaction();
            return true;
        }
        public void CommitTransaction()
        {
            if (_transaction != null) _transaction.Commit();
        }
        public void RollbackTransaction()
        {
            if (_transaction != null) _transaction.Rollback();
        }
        #endregion

        public HashVector() : this(0, null) { }

        public HashVector(int capacity) : this(capacity, null) { }

        public HashVector(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

        public HashVector(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            if (capacity > 0) Initialize(capacity);
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public HashVector(IDictionary<TKey, TValue> dictionary) : this(dictionary, null) { }

        public HashVector(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) :
            this(dictionary != null ? dictionary.Count : 0, comparer)
        {

            if (dictionary == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }

        protected HashVector(SerializationInfo info, StreamingContext context)
        {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
#if !NETCORE
            HashHelpers.SerializationInfoTable.Add(this, info);
#elif NETCORE
            _serializationInfo = info;
#endif
        }

        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                return comparer;
            }
        }

        public int Count
        {
            get { return count - freeCount; }
        }

        public VectorKeyCollection Keys
        {
            get
            {
#if DEBUG
                Contract.Ensures(Contract.Result<VectorKeyCollection>() != null);
#endif
                if (keys == null) keys = new VectorKeyCollection(this);
                return keys;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                if (keys == null) keys = new VectorKeyCollection(this);
                return keys;
            }
        }
        public VectorValueCollection Values
        {
            get
            {
#if DEBUG
                Contract.Ensures(Contract.Result<VectorValueCollection>() != null); 
#endif
                if (values == null) values = new VectorValueCollection(this);
                return values;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                if (values == null) values = new VectorValueCollection(this);
                return values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;
                ThrowHelper.ThrowKeyNotFoundException();
                return default(TValue);
            }
            set
            {
                Insert(key, value, false);
            }
        }

        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int i = FindEntry(keyValuePair.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value))
            {
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int i = FindEntry(keyValuePair.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value))
            {
                Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (count > 0)
            {
                ClusteredArray<Entry> oldDictionary = entries;

                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                if (_transaction != null)
                    Initialize(0);
                else
                    ClusteredArray<Entry>.Clear(entries, 0, count);

                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
                if (_transaction != null)
                {
                    _transaction.AddRollbackOperation(new ClearRollbackOperation(this, oldDictionary));
                }


            }
        }

        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }

        public bool ContainsValue(TValue value)
        {
            if (value == null)
            {
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && entries[i].value == null) return true;
                }
            }
            else
            {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value)) return true;
                }
            }
            return false;
        }

        private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (index < 0 || index > array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            int count = this.count;
            ClusteredArray<Entry> entries = this.entries;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public ImmutableEnumerator GetEnumerator()
        {
            return new ImmutableEnumerator(this, VectorEnumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new ImmutableEnumerator(this, VectorEnumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> GetOriginalEnumerator()
        {
            return new ImmutableEnumerator(this, VectorEnumerator.KeyValuePair);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
            }
            info.AddValue(VersionName, version);

#if FEATURE_RANDOMIZED_STRING_HASHING
            info.AddValue(ComparerName, HashHelpers.GetEqualityComparerForSerialization(comparer), typeof(IEqualityComparer<TKey>));
#else
            info.AddValue(ComparerName, comparer, typeof(IEqualityComparer<TKey>));
#endif

            info.AddValue(HashSizeName, buckets == null ? 0 : buckets.Length); //This is the length of the bucket array.
            if (buckets != null)
            {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        public ICollection<TValue> Get(ICollection<TKey> keys)
        {
            ICollection<TValue> collection = new List<TValue>();
            foreach(var key in keys)
            {
                TValue value;
                if (this.TryGetValue(key, out value))
                    collection.Add(value);
            }
            return collection;
        }

        private int FindEntry(TKey key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) return i;
                }
            }
            return -1;
        }

        private void Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            buckets = new ClusteredArray<int>(size);
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new ClusteredArray<Entry>(size);
            freeList = -1;
        }

        private void Insert(TKey key, TValue value, bool add)
        {

            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                
            }

            if (buckets == null) Initialize(0);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;

#if FEATURE_RANDOMIZED_STRING_HASHING
            int collisionCount = 0;
#endif

            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (add)
                    {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
                    }
                    Entry entry = entries[i];
                    TValue oldVal = entry.value;
                    entry.value = value;
                    entries[i] = entry;
                    version++;

                    if (_transaction != null)
                    {
                        _transaction.AddRollbackOperation(new InsertRollbackOperation(this, key, oldVal, false));
                    }
                    return;
                }

#if FEATURE_RANDOMIZED_STRING_HASHING
                collisionCount++;
#endif
            }
            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }
            Entry prevEntry = entries[index];

            prevEntry.hashCode = hashCode;
            prevEntry.next = buckets[targetBucket];
            prevEntry.key = key;
            TValue oldEntry = prevEntry.value;
            prevEntry.value = value;
            entries[index] = prevEntry;
            buckets[targetBucket] = index;
            version++;

            if (_transaction != null)
            {
                _transaction.AddRollbackOperation(new InsertRollbackOperation(this, key, oldEntry, true));
            }


#if FEATURE_RANDOMIZED_STRING_HASHING
            if(collisionCount > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(comparer)) 
            {
                comparer = (IEqualityComparer<TKey>) HashHelpers.GetRandomizedEqualityComparer(comparer);
                Resize(entries.Length, true);
            }
#endif

        }

        public virtual void OnDeserialization(Object sender)
        {
            SerializationInfo siInfo=null;
#if !NETCORE
            HashHelpers.SerializationInfoTable.TryGetValue(this, out siInfo);
#elif NETCORE
            siInfo = _serializationInfo;
#endif

            if (siInfo == null)
            {
                // It might be necessary to call OnDeserialization from a container if the container object also implements
                // OnDeserialization. However, remoting will call OnDeserialization again.
                // We can return immediately if this function is called twice. 
                // Note we set remove the serialization info from the table at the end of this method.
                return;
            }

            int realVersion = siInfo.GetInt32(VersionName);
            int hashsize = siInfo.GetInt32(HashSizeName);
            comparer = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));

            if (hashsize != 0)
            {
                buckets = new ClusteredArray<int>(hashsize);
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                entries = new ClusteredArray<Entry>(hashsize);
                freeList = -1;

                KeyValuePair<TKey, TValue>[] array = (KeyValuePair<TKey, TValue>[])
                    siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));

                if (array == null)
                {
                    ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Key == null)
                    {
                        ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
                    }
                    Insert(array[i].Key, array[i].Value, true);
                }
            }
            else
            {
                buckets = null;
            }

            version = realVersion;
#if !NETCORE
            HashHelpers.SerializationInfoTable.Remove(this);
#elif NETCORE
            _serializationInfo = null;
#endif
        }

        private void Resize()
        {
            Resize(HashHelpers.ExpandPrime(count), false);
        }

        private void Resize(int newSize, bool forceNewHashCodes)
        {
#if DEBUG
            Contract.Assert(newSize >= entries.Length); 
#endif
            var newBuckets = new ClusteredArray<int>(newSize);
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            var newEntries = new ClusteredArray<Entry>(newSize);
            ClusteredArray<Entry>.Copy(entries, 0, newEntries, 0, count);
            if (forceNewHashCodes)
            {
                for (int i = 0; i < count; i++)
                {
                    if (newEntries[i].hashCode != -1)
                    {
                        Entry newEntry = newEntries[i];
                        newEntry.hashCode = (comparer.GetHashCode(newEntries[i].key) & 0x7FFFFFFF);
                        newEntries[i] = newEntry;
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    Entry newEntry = newEntries[i];
                    newEntry.next = newBuckets[bucket];
                    newEntries[i] = newEntry;
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = entries[i].next;
                        }
                        else
                        {
                            Entry lastEntry = entries[last];
                            lastEntry.next = entries[i].next;
                            entries[last] = lastEntry;
                        }

                        Entry entry = entries[i];
                        TValue oldValue = entry.value;
                        entry.hashCode = -1;
                        entry.next = freeList;
                        entry.key = default(TKey);
                        entry.value = default(TValue);
                        entries[i] = entry;
                        freeList = i;
                        freeCount++;
                        version++;

                        if (_transaction != null)
                        {
                            _transaction.AddRollbackOperation(new RemoveRollbackOperation(this, key, oldValue));
                        }

                        return true;
                    }
                }
            }
            return false;
        }        
        private int GenerateRandomNumber(int count, Random randomNumber)
        {
            
            return randomNumber.Next(0, count-1); 
        }
        public TKey GetRandom(Random randomNumber)
        {            
            return Keys.ElementAt(GenerateRandomNumber(Keys.Count,randomNumber));
        }
        public TKey GetRandomUniqueItem(Random randomObject, ICollection<TKey> existingItems)
        {
            int index = GenerateRandomNumber(Keys.Count, randomObject);
            for (int i = 0; i < Count; i++)
            {
                if (!existingItems.Contains(Keys.ElementAt(index)))
                {
                    return Keys.ElementAt(index);
                }
                index++;
                index =  index % Count;
            }
            throw new Exception();
        }
            
        public TKey RemoveRandom(Random randomNumber)
        {
            TKey key = GetRandom(randomNumber);
            if(key!=null)
            {
                Remove(key);               
            }
            return key;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                value = entries[i].value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        // This is a convenience method for the internal callers that were converted from using Hashtable.
        // Many were combining key doesn't exist and key exists but null value (for non-value types) checks.
        // This allows them to continue getting that behavior with minimal code delta. This is basically
        // TryGetValue without the out param
        internal TValue GetValueOrDefault(TKey key)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                return entries[i].value;
            }
            return default(TValue);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }

            if (index < 0 || index > array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            KeyValuePair<TKey, TValue>[] pairs = array as KeyValuePair<TKey, TValue>[];
            if (pairs != null)
            {
                CopyTo(pairs, index);
            }
            else if (array is DictionaryEntry[])
            {
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                ClusteredArray<Entry> entries = this.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0)
                    {
                        dictEntryArray[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                    }
                }
            }
            else
            {
                object[] objects = array as object[];
                if (objects == null)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                }

                try
                {
                    int count = this.count;
                    ClusteredArray<Entry> entries = this.entries;
                    for (int i = 0; i < count; i++)
                    {
                        if (entries[i].hashCode >= 0)
                        {
                            objects[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                        }
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VectorEnumerator(this, VectorEnumerator.KeyValuePair);
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        ICollection IDictionary.Keys
        {
            get { return (ICollection)Keys; }
        }

        ICollection IDictionary.Values
        {
            get { return (ICollection)Values; }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    int i = FindEntry((TKey)key);
                    if (i >= 0)
                    {
                        return entries[i].value;
                    }
                }
                return null;
            }
            set
            {
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);

                try
                {
                    TKey tempKey = (TKey)key;
                    try
                    {
                        this[tempKey] = (TValue)value;
                    }
                    catch (InvalidCastException)
                    {
                        ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                    }
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
                }
            }
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }
            return (key is TKey);
        }

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);

            try
            {
                TKey tempKey = (TKey)key;

                try
                {
                    Add(tempKey, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                }
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
            }
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new VectorEnumerator(this, VectorEnumerator.DictEntry);
        }

        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey)key);
            }
        }       
        #region /                       ---- Rollback Operations ----                           /
        class ClearRollbackOperation : IRollbackOperation
        {
            private ClusteredArray<Entry> _items;
            private HashVector<TKey, TValue> _parent;

            public ClearRollbackOperation(HashVector<TKey, TValue> parent, ClusteredArray<Entry> items)
            {
                _items = items;
                _parent = parent;
            }

            public void Execute()
            {
                _parent.entries = _items;
            }
        }
        class RemoveRollbackOperation : IRollbackOperation
        {
            private TKey _key;
            private TValue _oldEntry;
            private HashVector<TKey, TValue> _parent;

            public RemoveRollbackOperation(HashVector<TKey, TValue> parent, TKey key, TValue oldEntry)
            {
                _key = key;
                _oldEntry = oldEntry;
                _parent = parent;
            }

            public void Execute()
            {
                _parent.Insert(_key, _oldEntry, true);
            }
        }
        class InsertRollbackOperation : IRollbackOperation
        {
            private TKey _key;
            private TValue _oldEntry;
            private bool _isAdd = false;
            private HashVector<TKey, TValue>  _parent;

            public InsertRollbackOperation(HashVector<TKey, TValue> parent, TKey key, TValue entry,bool isAdd)
            {
                _isAdd = isAdd;
                _key = key;
                _oldEntry = entry;
                _parent = parent;
            }

            public void Execute()
            {
                if(_isAdd)
                {
                    _parent.Remove(_key);
                }
                else
                {
                   _parent.Insert(_key, _oldEntry, false);
                }
            }
        }
        #endregion
        [Serializable]
        public struct VectorEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>,
            IDictionaryEnumerator
        {
            private HashVector<TKey, TValue> dictionary;
            private int version;
            private int index;
            private KeyValuePair<TKey, TValue> current;
            private int getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal VectorEnumerator(HashVector<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext()
            {
                if (version != dictionary.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)index < (uint)dictionary.count)
                {
                    if (dictionary.entries[index].hashCode >= 0)
                    {
                        current = new KeyValuePair<TKey, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }

                index = dictionary.count + 1;
                current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new System.Collections.DictionaryEntry(current.Key, current.Value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    }
                }
            }

            void IEnumerator.Reset()
            {
                if (version != dictionary.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                current = new KeyValuePair<TKey, TValue>();
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return current.Value;
                }
            }
        }


        [Serializable]
        public struct ImmutableEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>,
            IDictionaryEnumerator
        {
            private ClusteredArray<Entry> dictionary;
            private int index;
            private int count;
            private KeyValuePair<TKey, TValue> current;
            private int getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal ImmutableEnumerator(HashVector<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                count = dictionary.count;
                this.dictionary = dictionary.entries;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext()
            {
                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)index < (uint)count)
                {
                    if (dictionary[index].hashCode >= 0)
                    {
                        current = new KeyValuePair<TKey, TValue>(dictionary[index].key, dictionary[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }

                index = count + 1;
                current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || (index == count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new System.Collections.DictionaryEntry(current.Key, current.Value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    }
                }
            }

            void IEnumerator.Reset()
            {
                index = 0;
                current = new KeyValuePair<TKey, TValue>();
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || (index == count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || (index == count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || (index == count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return current.Value;
                }
            }
        }


        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class VectorKeyCollection : ICollection<TKey>, ICollection
        {
            private HashVector<TKey, TValue> dictionary;

            public VectorKeyCollection(HashVector<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
                }
                this.dictionary = dictionary;
            }

            public ImmutableKeyEnumerator GetEnumerator()
            {
                return new ImmutableKeyEnumerator(dictionary);
            }

            public IEnumerator<TKey> GetOriginalEnumerator() { return new Enumerator(dictionary); }

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (index < 0 || index > array.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                int count = dictionary.count;
                ClusteredArray<Entry> entries = dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].key;
                }
            }

            public int Count
            {
                get { return dictionary.Count; }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get { return true; }
            }

            void ICollection<TKey>.Add(TKey item)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
            }

            void ICollection<TKey>.Clear()
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
                return false;
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new ImmutableKeyEnumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ImmutableKeyEnumerator(dictionary);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (array.Rank != 1)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                if (array.GetLowerBound(0) != 0)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                }

                if (index < 0 || index > array.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                TKey[] keys = array as TKey[];
                if (keys != null)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    object[] objects = array as object[];
                    if (objects == null)
                    {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }

                    int count = dictionary.count;
                    ClusteredArray<Entry> entries = dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].key;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            Object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TKey>, System.Collections.IEnumerator
            {
                private HashVector<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TKey currentKey;

                internal Enumerator(HashVector<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentKey = default(TKey);
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentKey = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = dictionary.count + 1;
                    currentKey = default(TKey);
                    return false;
                }

                public TKey Current
                {
                    get
                    {
                        return currentKey;
                    }
                }

                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.count + 1))
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                        }

                        return currentKey;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }

                    index = 0;
                    currentKey = default(TKey);
                }
            }

            [Serializable]
            public struct ImmutableKeyEnumerator : IEnumerator<TKey>, System.Collections.IEnumerator
            {
                private ClusteredArray<Entry> dictionary;
                private int count;
                private int index;
                private TKey currentKey;

                internal ImmutableKeyEnumerator(HashVector<TKey, TValue> dictionary)
                {
                    count = dictionary.count;
                    this.dictionary = dictionary.entries;
                    index = 0;
                    currentKey = default(TKey);
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while ((uint)index < (uint)count)
                    {
                        if (dictionary[index].hashCode >= 0)
                        {
                            currentKey = dictionary[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = count + 1;
                    currentKey = default(TKey);
                    return false;
                }

                public TKey Current
                {
                    get
                    {
                        return currentKey;
                    }
                }

                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == count + 1))
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                        }

                        return currentKey;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    index = 0;
                    currentKey = default(TKey);
                }
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class VectorValueCollection : ICollection<TValue>, ICollection
        {
            private HashVector<TKey, TValue> dictionary;

            public VectorValueCollection(HashVector<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
                }
                this.dictionary = dictionary;
            }

            public ImmutableValueEnumerator GetEnumerator()
            {
                return new ImmutableValueEnumerator(dictionary);
            }

            public IEnumerator<TValue> GetOriginalEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (index < 0 || index > array.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }

                int count = dictionary.count;
                ClusteredArray<Entry> entries = dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].value;
                }
            }

            public int Count
            {
                get { return dictionary.Count; }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get { return true; }
            }

            void ICollection<TValue>.Add(TValue item)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
                return false;
            }

            void ICollection<TValue>.Clear()
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return dictionary.ContainsValue(item);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (array.Rank != 1)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                if (array.GetLowerBound(0) != 0)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                }

                if (index < 0 || index > array.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);

                TValue[] values = array as TValue[];
                if (values != null)
                {
                    CopyTo(values, index);
                }
                else
                {
                    object[] objects = array as object[];
                    if (objects == null)
                    {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }

                    int count = dictionary.count;
                    ClusteredArray<Entry> entries = dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].value;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            Object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TValue>, System.Collections.IEnumerator
            {
                private HashVector<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TValue currentValue;

                internal Enumerator(HashVector<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default(TValue);
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentValue = dictionary.entries[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentValue = default(TValue);
                    return false;
                }

                public TValue Current
                {
                    get
                    {
                        return currentValue;
                    }
                }

                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.count + 1))
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                        }

                        return currentValue;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }
                    index = 0;
                    currentValue = default(TValue);
                }
            }

            [Serializable]
            public struct ImmutableValueEnumerator : IEnumerator<TValue>, System.Collections.IEnumerator
            {
                private ClusteredArray<Entry> dictionary;
                private int index;
                private int version;
                private int count;
                private TValue currentValue;

                internal ImmutableValueEnumerator(HashVector<TKey, TValue> dictionary)
                {
                    count = dictionary.count;
                    this.dictionary = dictionary.entries;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default(TValue);
                }


                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while ((uint)index < (uint)count)
                    {
                        if (dictionary[index].hashCode >= 0)
                        {
                            currentValue = dictionary[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = count + 1;
                    currentValue = default(TValue);
                    return false;
                }

                public TValue Current
                {
                    get
                    {
                        return currentValue;
                    }
                }

                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == count + 1))
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                        }

                        return currentValue;
                    }
                }

                void System.Collections.IEnumerator.Reset()
                {
                    index = 0;
                    currentValue = default(TValue);
                }
            }


        }

    }


}
