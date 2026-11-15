// Flushes Unity's in-memory file system (persistentDataPath) to the browser's IndexedDB.
// Without this, a save written just before the tab is closed can be lost on WebGL.
// Called from SaveStore.cs after every write.
mergeInto(LibraryManager.library, {
  DearPompomSyncFiles: function () {
    try {
      FS.syncfs(false, function (err) {
        if (err) console.warn('[DearPompom] could not sync save files to IndexedDB:', err);
      });
    } catch (e) {
      console.warn('[DearPompom] file sync is unavailable:', e);
    }
  }
});
