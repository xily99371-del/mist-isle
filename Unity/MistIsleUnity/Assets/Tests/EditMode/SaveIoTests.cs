using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MistIsle.Creatures;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class SaveIoTests
    {
        private string temporaryRoot;
        private string leaf;
        private string directory;
        private ChapterSaveStore store;
        private readonly Dictionary<string, CreatureSpecies> catalog = new Dictionary<string, CreatureSpecies>();

        [SetUp]
        public void SetUp()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("These tests exercise Windows file replacement and sharing semantics.");
            temporaryRoot = Path.GetFullPath(Path.GetTempPath())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            leaf = "MistIsle-SaveIo-" + Guid.NewGuid().ToString("N");
            directory = Path.GetFullPath(Path.Combine(temporaryRoot, leaf));
            Directory.CreateDirectory(directory);
            store = new ChapterSaveStore(directory);
            Assert.That(store.Save(Journey(1), out var firstError), Is.True, firstError);
            Assert.That(store.Save(Journey(2), out var secondError), Is.True, secondError);
        }

        [TearDown]
        public void TearDown()
        {
            if (directory == null) return;
            string resolved = Path.GetFullPath(directory);
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(resolved), leaf, StringComparison.Ordinal)
                || !string.Equals(resolved, Path.Combine(temporaryRoot, leaf), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to delete an unverified save-I/O test directory.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }

        private static WorldSaveData Journey(int count)
        {
            var data = new WorldSaveData { roomId = "coast-0", hasPosition = true, playerX = 24.5f };
            data.inventoryItemIds.Add("shell");
            data.inventoryCounts.Add(count);
            data.collectedNodeIds.Add("shell-coast-" + count);
            return data;
        }

        private FileStream HoldReader(bool backup)
            => File.Open(store.Path + (backup ? ".bak" : ""), FileMode.Open, FileAccess.Read, FileShare.Read);

        private void AssertSavedProgress(int count)
        {
            Assert.That(store.TryLoad(catalog, out var loaded, out var message), Is.True, message);
            Assert.That(loaded.inventoryCounts, Is.EqualTo(new[] { count }));
            Assert.That(loaded.collectedNodeIds, Is.EqualTo(new[] { "shell-coast-" + count }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WindowsReplacementWithReadOnlySharingPreservesBothFilesUntilReaderCloses(bool backup)
        {
            byte[] primaryBefore = File.ReadAllBytes(store.Path);
            byte[] backupBefore = File.ReadAllBytes(store.Path + ".bak");
            string temporary = store.Path + ".probe";
            File.WriteAllText(temporary, JsonUtility.ToJson(Journey(3)));
            using (HoldReader(backup))
            {
                var exception = Assert.Throws<IOException>(() => File.Replace(temporary, store.Path, store.Path + ".bak"));
                TestContext.WriteLine("Held " + (backup ? "backup" : "primary")
                    + ": HRESULT=0x" + exception.HResult.ToString("X8") + " " + exception.Message);
                CollectionAssert.AreEqual(primaryBefore, File.ReadAllBytes(store.Path));
                CollectionAssert.AreEqual(backupBefore, File.ReadAllBytes(store.Path + ".bak"));
            }
            File.Replace(temporary, store.Path, store.Path + ".bak");
            AssertSavedProgress(3);
            CollectionAssert.AreEqual(primaryBefore, File.ReadAllBytes(store.Path + ".bak"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PersistentReaderReturnsFailureWithExistingProgressIntactAndAllowsLaterSave(bool backup)
        {
            byte[] primaryBefore = File.ReadAllBytes(store.Path);
            byte[] backupBefore = File.ReadAllBytes(store.Path + ".bak");
            using (HoldReader(backup))
            {
                var elapsed = Stopwatch.StartNew();
                Assert.That(store.Save(Journey(3), out var error), Is.False);
                elapsed.Stop();
                Assert.That(error, Does.Contain("保存失败"));
                Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(1500), "A persistent reader must not stall the game.");
                TestContext.WriteLine("Persistent " + (backup ? "backup" : "primary")
                    + " reader: Save returned in " + elapsed.ElapsedMilliseconds + "ms. " + error);
                CollectionAssert.AreEqual(primaryBefore, File.ReadAllBytes(store.Path));
                CollectionAssert.AreEqual(backupBefore, File.ReadAllBytes(store.Path + ".bak"));
                AssertSavedProgress(2);
                Assert.That(JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(store.Path + ".bak")).inventoryCounts,
                    Is.EqualTo(new[] { 1 }));
            }
            Assert.That(store.Save(Journey(3), out var retryError), Is.True, retryError);
            AssertSavedProgress(3);
            CollectionAssert.AreEqual(primaryBefore, File.ReadAllBytes(store.Path + ".bak"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BriefReaderCanCloseDuringSaveAndPreviousProgressBecomesBackup(bool backup)
        {
            byte[] primaryBefore = File.ReadAllBytes(store.Path);
            var reader = HoldReader(backup);
            using (var start = new ManualResetEventSlim(false))
            {
                var release = new Thread(() =>
                {
                    start.Wait();
                    Thread.Sleep(40);
                    reader.Dispose();
                });
                release.Start();
                bool saved;
                string error;
                var elapsed = Stopwatch.StartNew();
                try
                {
                    start.Set();
                    saved = store.Save(Journey(3), out error);
                    elapsed.Stop();
                }
                finally
                {
                    start.Set();
                    release.Join();
                    reader.Dispose();
                }
                TestContext.WriteLine("Brief " + (backup ? "backup" : "primary")
                    + " reader: Save returned " + saved + " in " + elapsed.ElapsedMilliseconds + "ms. " + error);
                Assert.That(saved, Is.True, error);
            }
            AssertSavedProgress(3);
            CollectionAssert.AreEqual(primaryBefore, File.ReadAllBytes(store.Path + ".bak"));
            Assert.That(File.Exists(store.Path + ".tmp"), Is.False);
        }
    }
}
