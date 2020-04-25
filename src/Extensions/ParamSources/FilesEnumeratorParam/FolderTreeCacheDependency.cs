using System;
using System.IO;
using System.Linq;
using System.Web.Caching;

namespace MIISFilesEnumeratorFMS
{
    /// <summary>
    /// Class that uses a FileSystemWatcher to create a dependency on folders and subfolders
    /// It will invalidate the cache only for .md or .mdh files
    /// </summary>
    public class FolderTreeCacheDependency : CacheDependency
    {
        private FileSystemWatcher watcher;
        private string _folderPath;
        private bool _CacheInvalidated = false;  //Flag to prevent do the cache invalidation ore than once

        public FolderTreeCacheDependency(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new FileNotFoundException(folderPath + "does not exist");

            _folderPath = folderPath;

            watcher = new FileSystemWatcher(folderPath);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.FileName 
                                 | NotifyFilters.DirectoryName 
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite;

            //Events to notify cache invalidation
            watcher.Changed += new FileSystemEventHandler(ChangeDetectedHandler);
            watcher.Deleted += new FileSystemEventHandler(DeletedHandler);
            watcher.Created += new FileSystemEventHandler(ChangeDetectedHandler);
            watcher.Renamed += new RenamedEventHandler(ChangeDetectedHandler);

            //Start watching
            watcher.EnableRaisingEvents = true;
        }

        public override string GetUniqueID()
        {
            return _folderPath;
        }

        //Deletion of file or folder
        void DeletedHandler(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.Name).ToLowerInvariant();
            //If the deleted item has no extension (normally a folder: we can't check because it's gone)
            //or has one of the MIIS extensions, the invalidate cache
            if (string.IsNullOrEmpty(ext) || FilesEnumeratorHelper.MIIS_EXTS.Contains(ext))
            { 
                InvalidateCache(e);
            }
        }

        //Any change in existent files or folders
        void ChangeDetectedHandler(object sender, FileSystemEventArgs e)
        {
            bool isFolder = Directory.Exists(e.FullPath);

            //If its a folder that has been created, this doesn't affect the current cache at all, just leave.
            if (isFolder && e.ChangeType == WatcherChangeTypes.Created)
                return;

            //If it's a folder (changed or deleted), just invalidate cache
            if (isFolder)
            {
                InvalidateCache(e);
            }
            else
            {
                //If it's a file, the affected file should be one of the MIIS' extensions 
                //(in other case, do nothing: it doesn't affect the cache)
                if (FilesEnumeratorHelper.MIIS_EXTS.Contains(Path.GetExtension(e.Name).ToLowerInvariant()))
                {
                    InvalidateCache(e);
                }
            }
        }

        /// <summary>
        /// Invalidates the cache and stops the Fil eWatcher
        /// </summary>
        /// <param name="e"></param>
        private void InvalidateCache(FileSystemEventArgs e)
        {
            if (!_CacheInvalidated) //Just one call is enough, prevent more
            {
                //Stop watching
                watcher.EnableRaisingEvents = false;
                //Invalidate cache
                base.NotifyDependencyChanged(this, e);
                //_CacheInvalidated = true;
                watcher.Dispose();
            }
        }
    }
}
