// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Collections;
using osu.Game.IO;
using osu.Game.IO.Legacy;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Database
{
    public class LegacyCollectionImporter
    {
        private readonly CollectionManager collections;

        public LegacyCollectionImporter(CollectionManager collections)
        {
            this.collections = collections;
        }

        public Action<Notification> PostNotification { protected get; set; }

        private const string database_name = "collection.db";

        public Task<int> GetAvailableCount(StableStorage stableStorage)
        {
            if (!stableStorage.Exists(database_name))
                return Task.FromResult(0);

            return Task.Run(() =>
            {
                using (var stream = stableStorage.GetStream(database_name))
                    return readCollections(stream).Count;
            });
        }

        /// <summary>
        /// This is a temporary method and will likely be replaced by a full-fledged (and more correctly placed) migration process in the future.
        /// </summary>
        public Task ImportFromStableAsync(StableStorage stableStorage)
        {
            if (!stableStorage.Exists(database_name))
            {
                // This handles situations like when the user does not have a collections.db file
                Logger.Log($"No {database_name} available in osu!stable installation", LoggingTarget.Information, LogLevel.Error);
                return Task.CompletedTask;
            }

            return Task.Run(async () =>
            {
                using (var stream = stableStorage.GetStream(database_name))
                    await Import(stream).ConfigureAwait(false);
            });
        }

        public async Task Import(Stream stream)
        {
            var notification = new ProgressNotification
            {
                State = ProgressNotificationState.Active,
                Text = "Collections import is initialising..."
            };

            PostNotification?.Invoke(notification);

            var importedCollections = readCollections(stream, notification);
            await importCollections(importedCollections).ConfigureAwait(false);

            notification.CompletionText = $"Imported {importedCollections.Count} collections";
            notification.State = ProgressNotificationState.Completed;
        }

        private Task importCollections(List<BeatmapCollection> newCollections)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Schedule(() =>
            // {
            try
            {
                foreach (var newCol in newCollections)
                {
                    var existing = collections.Collections.FirstOrDefault(c => c.Name.Value == newCol.Name.Value);
                    if (existing == null)
                        collections.Collections.Add(existing = new BeatmapCollection { Name = { Value = newCol.Name.Value } });

                    foreach (string newBeatmap in newCol.BeatmapHashes)
                    {
                        if (!existing.BeatmapHashes.Contains(newBeatmap))
                            existing.BeatmapHashes.Add(newBeatmap);
                    }
                }

                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to import collection.");
                tcs.SetException(e);
            }
            // });

            return tcs.Task;
        }

        private List<BeatmapCollection> readCollections(Stream stream, ProgressNotification notification = null)
        {
            if (notification != null)
            {
                notification.Text = "Reading collections...";
                notification.Progress = 0;
            }

            var result = new List<BeatmapCollection>();

            try
            {
                using (var sr = new SerializationReader(stream))
                {
                    sr.ReadInt32(); // Version

                    int collectionCount = sr.ReadInt32();
                    result.Capacity = collectionCount;

                    for (int i = 0; i < collectionCount; i++)
                    {
                        if (notification?.CancellationToken.IsCancellationRequested == true)
                            return result;

                        var collection = new BeatmapCollection { Name = { Value = sr.ReadString() } };
                        int mapCount = sr.ReadInt32();

                        for (int j = 0; j < mapCount; j++)
                        {
                            if (notification?.CancellationToken.IsCancellationRequested == true)
                                return result;

                            string checksum = sr.ReadString();

                            collection.BeatmapHashes.Add(checksum);
                        }

                        if (notification != null)
                        {
                            notification.Text = $"Imported {i + 1} of {collectionCount} collections";
                            notification.Progress = (float)(i + 1) / collectionCount;
                        }

                        result.Add(collection);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to read collection database.");
            }

            return result;
        }
    }
}
