using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NativeContainers {
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeHashSet<T> : IDisposable where T : struct, IEquatable<T> {
        [NativeDisableUnsafePtrRestriction] NativeHashSetData* buffer;
        Allocator allocator;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;
#endif

        public NativeHashSet(int capacity, Allocator allocator) {
            NativeHashSetData.AllocateHashSet<T>(capacity, allocator, out buffer);
            this.allocator = allocator;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(
                out m_Safety, out m_DisposeSentinel, callSiteStackDepth:8, allocator:allocator);
#endif
            Clear();
        }
        
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct Concurrent {
            [NativeDisableUnsafePtrRestriction] public NativeHashSetData* buffer;
            [NativeSetThreadIndex] public int threadIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public AtomicSafetyHandle m_Safety;
#endif

            public int Capacity {
                get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return buffer->Capacity;
                }
            }

            public bool TryAdd(T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                return buffer->TryAddThreaded(ref value, threadIndex);
            }
        }

        public int Capacity {
            get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return buffer->Capacity;
            }
        }

        public int Length {
            get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return buffer->Length;
            }
        }

        public bool IsCreated => buffer != null;

        public void Dispose() {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            NativeHashSetData.DeallocateHashSet(buffer, allocator);
            buffer = null;
        }

        public void Clear() {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            buffer->Clear<T>();
        }

        public Concurrent ToConcurrent() {
            Concurrent concurrent;
            concurrent.threadIndex = 0;
            concurrent.buffer = buffer;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            concurrent.m_Safety = m_Safety;
#endif
            return concurrent;
        }

        public bool TryAdd(T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return buffer->TryAdd(ref value, allocator);
        }

        public bool TryRemove(T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return buffer->TryRemove(value);
        }

        public bool Contains(T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return buffer->Contains(ref value);
        }

        public NativeArray<T> GetValueArray(Allocator allocator) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var result = new NativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            buffer->GetValueArray(result);
            return result;
        }
    }
}
