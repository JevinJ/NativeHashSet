using System.Collections.Generic;
using NativeContainers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NativeContainerTests {
    public class NativeHashSetBasicTests {
        const int HashSetInitialCapacity = 4;
        NativeHashSet<int> testHashSet;

        [OneTimeSetUp]
        public void Setup() {
            testHashSet = new NativeHashSet<int>(HashSetInitialCapacity, Allocator.Persistent);
        }

        [OneTimeTearDown]
        public void TearDown() {
            testHashSet.Dispose();
        }

        [Test, Order(0)]
        public void Capacity_ShouldBeInitalValue() {
            Assert.AreEqual(HashSetInitialCapacity, testHashSet.Capacity);
        }

        [Test, Order(1)]
        public void Add_ShouldAdd1() {
            testHashSet.TryAdd(1);
            Assert.AreEqual(1, testHashSet.Length);
        }

        [Test, Order(2)]
        public void Remove_ShouldRemove1() {
            testHashSet.TryRemove(1);
            Assert.AreEqual(0, testHashSet.Length);
        }

        [Test, Order(3)]
        public void Add_ShouldReturnTrueOnUnique() {
            Assert.IsTrue(testHashSet.TryAdd(1));
        }

        [Test, Order(4)]
        public void Add_ShouldReturnFalseOnDuplicate() {
            Assert.IsFalse(testHashSet.TryAdd(1));
        }

        [Test, Order(5)]
        public void Remove_ShouldReturnTrueIfExists() {
            Assert.IsTrue(testHashSet.TryRemove(1));
        }

        [Test, Order(6)]
        public void Remove_ShouldReturnFalseIfNotExists() {
            Assert.IsFalse(testHashSet.TryRemove(99));
        }

        [Test, Order(7)]
        public void Clear_LengthShouldEqual0() {
            testHashSet.TryAdd(1);
            Assert.AreEqual(1, testHashSet.Length);
            testHashSet.Clear();
            Assert.AreEqual(0, testHashSet.Length);
        }
        
        [Test, Order(8)]
        public void Contains_ShouldNotContainIfNotAdded() {
            Assert.IsFalse(testHashSet.Contains(99));
        }
    }

    public class NativeHashSetExtendedRandomTests {
        const int RandomNumbersMinLength = 100;
        const int RandomNumbersMaxLength = 200;
        const int RandomNumberMaxValue = 1000000;
        NativeHashSet<int> testHashSet;
        NativeList<int> uniqueRandomNumbers;

        [OneTimeSetUp]
        public void Setup() {
            uniqueRandomNumbers = new NativeList<int>(Allocator.Persistent);
            var managedSet = new HashSet<int>();
            var randomNumbersLength = Random.Range(RandomNumbersMinLength, RandomNumbersMaxLength);
            for(int i = 0; i < randomNumbersLength; i++) {
                managedSet.Add(Random.Range(0, RandomNumberMaxValue));
            }
            foreach(var num in managedSet) {
                uniqueRandomNumbers.Add(num);
            }
        }

        [OneTimeTearDown]
        public void TearDown() {
            uniqueRandomNumbers.Dispose();
        }

        [SetUp]
        public void TestSetUp() {
            testHashSet = new NativeHashSet<int>(uniqueRandomNumbers.Length, Allocator.Temp);
        }

        [TearDown]
        public void TestTearDown() {
            testHashSet.Dispose();
        }

        [Test]
        public void Length_ShouldEqualRandomLength() {
            for(int i = 0; i < uniqueRandomNumbers.Length; i++) {
                testHashSet.TryAdd(uniqueRandomNumbers[i]);
            }
            Assert.AreEqual(uniqueRandomNumbers.Length, testHashSet.Length);
        }

        [Test]
        public void Remove_ShouldRemoveRange() {
            for(int i = 0; i < uniqueRandomNumbers.Length; i++) {
                testHashSet.TryAdd(uniqueRandomNumbers[i]);
                Assert.IsTrue(testHashSet.TryRemove(uniqueRandomNumbers[i]));
            }
            Assert.AreEqual(0, testHashSet.Length);
        }

        [Test]
        public void Contains_ShouldContainRandomNumber() {
            var randomNum = uniqueRandomNumbers[Random.Range(0, uniqueRandomNumbers.Length)];
            testHashSet.TryAdd(randomNum);
            Assert.IsTrue(testHashSet.Contains(randomNum));
        }

        [Test]
        public void Contains_ShouldContainRandomRange() {
            for(int i = 0; i < uniqueRandomNumbers.Length; i++) {
                testHashSet.TryAdd(uniqueRandomNumbers[i]);
                Assert.IsTrue(testHashSet.Contains(uniqueRandomNumbers[i]));
            }
        }

        [Test]
        public void GetValueArray_ShouldReturnCorrectRandomLength() {
            for(int i = 0; i < uniqueRandomNumbers.Length; i++) {
                testHashSet.TryAdd(uniqueRandomNumbers[i]);
            }
            var values = testHashSet.GetValueArray(Allocator.Temp);
            Assert.AreEqual(uniqueRandomNumbers.Length, values.Length);
        }

        [Test]
        public void GetValueArray_ShouldReturnAllValues() {
            var managedSet = new HashSet<int>();
            for(int i = 0; i < uniqueRandomNumbers.Length; i++) {
                managedSet.Add(uniqueRandomNumbers[i]);
                testHashSet.TryAdd(uniqueRandomNumbers[i]);
            }
            var values = testHashSet.GetValueArray(Allocator.Temp);
            Assert.AreEqual(managedSet.Count, values.Length);
            for(int i = 0; i < values.Length; i++) {
                Assert.IsTrue(managedSet.Contains(values[i]));
            }
        }
    }

    public class NativeHashSetJobRandomTests {
        const int RandomNumbersMinLength = 500;
        const int RandomNumbersMaxLength = 2000;
        const int RandomNumberMaxValue = 1000000;
        NativeHashSet<int> testHashSet;
        NativeList<int> uniqueRandomNumbers;
        
        struct AddJob : IJobParallelFor {
            public NativeHashSet<int>.Concurrent HashSet;
            [ReadOnly] public NativeArray<int> ToAdd;

            public void Execute(int index) {
                HashSet.TryAdd(ToAdd[index]);
            }
        }
        
        [OneTimeSetUp]
        public void Setup() {
            var randomNumbersLength = Random.Range(RandomNumbersMinLength, RandomNumbersMaxLength);
            uniqueRandomNumbers = new NativeList<int>(randomNumbersLength, Allocator.Persistent);
            var managedSet = new HashSet<int>();
            for(int i = 0; i < randomNumbersLength; i++) {
                managedSet.Add(Random.Range(0, RandomNumberMaxValue));
            }
            foreach(var num in managedSet) {
                uniqueRandomNumbers.Add(num);
            }
        }

        [OneTimeTearDown]
        public void TearDown() {
            uniqueRandomNumbers.Dispose();
        }
        
        [SetUp]
        public void SetUpHashSet() {
            if(testHashSet.IsCreated) {
                testHashSet.Dispose();
            }
            testHashSet = new NativeHashSet<int>(uniqueRandomNumbers.Length, Allocator.TempJob);
            var addJob = new AddJob {
                HashSet = testHashSet.ToConcurrent(),
                ToAdd = uniqueRandomNumbers.AsArray()
            }.Schedule(uniqueRandomNumbers.Length, 1);
            addJob.Complete();
        }

        [Test]
        public void Add_ShouldAddRange() {
            Assert.AreEqual(uniqueRandomNumbers.Length, testHashSet.Length);
        }
        
        [Test]
        public void Contains_ShouldContainRandomRange() {
            for(int i = 0; i < uniqueRandomNumbers.Length; i++) {
                Assert.IsTrue(testHashSet.Contains(uniqueRandomNumbers[i]));
            }
        }
        
        [Test]
        public void Contains_ShouldContainRandomRangeAfterRemovingSome() {
            var numberToRemove = Random.Range(0, uniqueRandomNumbers.Length + 1);
            for(int i = 0; i < numberToRemove; i++) {
                testHashSet.TryRemove(uniqueRandomNumbers[i]);
            }
            Assert.AreEqual(uniqueRandomNumbers.Length - numberToRemove, testHashSet.Length);
            for(int i = numberToRemove; i < uniqueRandomNumbers.Length; i++) {
                Assert.IsTrue(testHashSet.Contains(uniqueRandomNumbers[i]));
            }
        }

        [Test]
        public void Clear_ShouldClearAdditions() {
            Assert.AreEqual(uniqueRandomNumbers.Length, testHashSet.Length);
            testHashSet.Clear();
            Assert.AreEqual(0, testHashSet.Length);
        }
    }
}
