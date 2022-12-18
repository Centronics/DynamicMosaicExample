using System;
using System.IO;
using System.Threading;
using ProcStorType = DynamicMosaicExample.ConcurrentProcessorStorage.ProcessorStorageType;

namespace DynamicMosaicExample
{
    internal sealed partial class FrmExample
    {
        enum SourceChanged
        {
            IMAGES,
            RECOGNIZE,
            WORKDIR
        }

        ConcurrentProcessorStorage GetStorage(SourceChanged source, string fullPath)
        {
            switch (source)
            {
                case SourceChanged.IMAGES:
                    return _imagesProcessorStorage;
                case SourceChanged.RECOGNIZE:
                    return _recognizeProcessorStorage;
                case SourceChanged.WORKDIR:
                    if (_imagesProcessorStorage.IsWorkingPath(fullPath, true))
                        return _imagesProcessorStorage;

                    return _recognizeProcessorStorage.IsWorkingPath(fullPath, true)
                        ? _recognizeProcessorStorage
                        : null;
            }

            throw new ArgumentException($@"Неизвестно, в каком хранилище требуется произвести изменение {nameof(SourceChanged)}: {source}", nameof(source));
        }

        void EnqueueRemoveStorage(ConcurrentProcessorStorage storage)
        {
            if (storage == null)
                return;

            _concurrentFileTasks.Enqueue(new FileTask(FileTaskAction.CLEARED, storage));

            RefreshRecognizer();
        }

        void EnqueueRemove(string fullPath, ConcurrentProcessorStorage storage)
        {
            if (storage == null)
                return;

            _concurrentFileTasks.Enqueue(new Common(FileTaskAction.REMOVED, storage, ConcurrentProcessorStorage.AddEndingSlash(fullPath)));

            RefreshRecognizer();
        }

        void EnqueueCreate(string fullPath, ConcurrentProcessorStorage storage)
        {
            if (storage == null)
                return;

            _concurrentFileTasks.Enqueue(new Common(FileTaskAction.CREATED, storage, fullPath));

            RefreshRecognizer();
        }

        static FileTaskAction ConvertWatcherChangeTypes(WatcherChangeTypes c)
        {
            switch (c)
            {
                case WatcherChangeTypes.Created:
                    return FileTaskAction.CREATED;
                case WatcherChangeTypes.Deleted:
                    return FileTaskAction.REMOVED;
                case WatcherChangeTypes.Changed:
                    return FileTaskAction.CHANGED;
                case WatcherChangeTypes.Renamed:
                    return FileTaskAction.RENAMED;
                case WatcherChangeTypes.All:
                default:
                    throw new ArgumentOutOfRangeException(nameof(c), c, @"Неизвестная операция файловой системы.");
            }
        }

        void TrackStorage(ConcurrentProcessorStorage storage, bool enable)
        {
            if (storage == null)
                return;

            ThreadPool.QueueUserWorkItem(_ => InvokeAction(() =>
            {
                switch (storage.StorageType)
                {
                    case ProcStorType.IMAGE:
                        if (enable)
                            CreateImageWatcher();
                        else
                            DisposeImageWatcher();
                        return;
                    case ProcStorType.RECOGNIZE:
                        if (enable)
                            CreateRecognizeWatcher();
                        else
                            DisposeRecognizeWatcher();
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(storage.StorageType),
                            storage.StorageType,
                            @"Неизвестный тип хранилища.");
                }
            }));
        }

        void RenamedThreadFunction(string oldFullPath, string newFullPath, SourceChanged source, ConcurrentProcessorStorage oldStorage)
        {
            if (source != SourceChanged.WORKDIR)
            {
                EnqueueRemove(oldFullPath, oldStorage);
                EnqueueCreate(newFullPath, oldStorage);
                return;
            }

            TrackStorage(oldStorage, false);
            EnqueueRemoveStorage(oldStorage);

            TrackStorage(oldStorage, true);
            EnqueueCreate(newFullPath, oldStorage);
        }

        void ChangedThreadFunction(WatcherChangeTypes type, string fullPath, ConcurrentProcessorStorage storage, SourceChanged source)
        {
            switch (type)
            {
                case WatcherChangeTypes.Deleted:
                    if (source == SourceChanged.WORKDIR)
                    {
                        TrackStorage(storage, false);
                        EnqueueRemoveStorage(storage);
                    }
                    else
                        EnqueueRemove(fullPath, storage);
                    return;
                case WatcherChangeTypes.Created:
                    if (source == SourceChanged.WORKDIR)
                        TrackStorage(storage, true);

                    EnqueueCreate(fullPath, storage);
                    return;
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $@"{nameof(ChangedThreadFunction)}: Неизвестный тип изменения.");
            }
        }

        void OnChanged(FileSystemEventArgs e, SourceChanged source) => SafetyExecute(() =>
        {
            if (string.IsNullOrWhiteSpace(e.FullPath))
                return;

            ConcurrentProcessorStorage storage = GetStorage(source, e.FullPath);

            if (storage == null)
                return;

            if (!storage.IsProcessorFile(e.FullPath))
            {
                ThreadPool.QueueUserWorkItem(state => SafetyExecute(() => ChangedThreadFunction(e.ChangeType, e.FullPath, storage, source)));
                return;
            }

            _concurrentFileTasks.Enqueue(new Common(ConvertWatcherChangeTypes(e.ChangeType), storage, e.FullPath));
            RefreshRecognizer();
        });

        void OnRenamed(RenamedEventArgs e, SourceChanged source) => SafetyExecute(() =>
        {
            if (string.IsNullOrWhiteSpace(e.FullPath) || string.IsNullOrWhiteSpace(e.OldFullPath))
                return;

            ConcurrentProcessorStorage oldStorage = GetStorage(source, e.OldFullPath);

            if (oldStorage == null)
                return;

            bool renamedFrom = oldStorage.IsProcessorFile(e.OldFullPath);
            bool renamedTo = oldStorage.IsProcessorFile(e.FullPath);

            if (!renamedTo && !renamedFrom)
            {
                ThreadPool.QueueUserWorkItem(state => SafetyExecute(() => RenamedThreadFunction(e.OldFullPath, e.FullPath, source, oldStorage)));
                return;
            }

            _concurrentFileTasks.Enqueue(new Renamed(ConvertWatcherChangeTypes(e.ChangeType), oldStorage, e.FullPath, e.OldFullPath, renamedTo, renamedFrom));
            RefreshRecognizer();
        });

        void IntRecognizeOnChanged(object _, FileSystemEventArgs e) => SafetyExecute(() => OnChanged(e, SourceChanged.RECOGNIZE));
        void IntRecognizeOnRenamed(object _, RenamedEventArgs e) => SafetyExecute(() => OnRenamed(e, SourceChanged.RECOGNIZE));

        void IntImagesOnChanged(object _, FileSystemEventArgs e) => SafetyExecute(() => OnChanged(e, SourceChanged.IMAGES));
        void IntImagesOnRenamed(object _, RenamedEventArgs e) => SafetyExecute(() => OnRenamed(e, SourceChanged.IMAGES));

        void IntWorkDirOnChanged(object _, FileSystemEventArgs e) => SafetyExecute(() => OnChanged(e, SourceChanged.WORKDIR));
        void IntWorkDirOnRenamed(object _, RenamedEventArgs e) => SafetyExecute(() => OnRenamed(e, SourceChanged.WORKDIR));

        void CreateRecognizeWatcher()
        {
            DisposeRecognizeWatcher();

            FileSystemWatcher rw = new FileSystemWatcher();

            rw.BeginInit();
            rw.IncludeSubdirectories = true;
            rw.NotifyFilter = NotifyFilters.FileName;
            rw.SynchronizingObject = this;
            rw.Path = RecognizeImagesPath;
            rw.IncludeSubdirectories = true;
            rw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
            rw.Filter = "*.*";
            rw.Changed += IntRecognizeOnChanged;
            rw.Created += IntRecognizeOnChanged;
            rw.Deleted += IntRecognizeOnChanged;
            rw.Renamed += IntRecognizeOnRenamed;
            rw.EndInit();

            rw.EnableRaisingEvents = true;

            _fswRecognizeChanged = rw;
        }

        void CreateImageWatcher()
        {
            DisposeImageWatcher();

            FileSystemWatcher ic = new FileSystemWatcher();

            ic.BeginInit();
            ic.IncludeSubdirectories = true;
            ic.NotifyFilter = NotifyFilters.FileName;
            ic.SynchronizingObject = this;
            ic.Path = SearchImagesPath;
            ic.IncludeSubdirectories = true;
            ic.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
            ic.Filter = "*.*";
            ic.Changed += IntImagesOnChanged;
            ic.Created += IntImagesOnChanged;
            ic.Deleted += IntImagesOnChanged;
            ic.Renamed += IntImagesOnRenamed;
            ic.EndInit();

            ic.EnableRaisingEvents = true;

            _fswImageChanged = ic;
        }

        void CreateWorkDirWatcher()
        {
            DisposeWorkDirWatcher();

            FileSystemWatcher wc = new FileSystemWatcher();

            wc.BeginInit();
            wc.Path = WorkingDirectory;
            wc.IncludeSubdirectories = false;
            wc.NotifyFilter = NotifyFilters.DirectoryName;
            wc.Filter = "*.*";
            wc.SynchronizingObject = this;
            wc.Changed += IntWorkDirOnChanged;
            wc.Created += IntWorkDirOnChanged;
            wc.Deleted += IntWorkDirOnChanged;
            wc.Renamed += IntWorkDirOnRenamed;
            wc.EndInit();

            wc.EnableRaisingEvents = true;

            _fswWorkDirChanged = wc;
        }

        void DisposeRecognizeWatcher()
        {
            _fswRecognizeChanged?.Dispose();
            _fswRecognizeChanged = null;
        }

        void DisposeImageWatcher()
        {
            _fswImageChanged?.Dispose();
            _fswImageChanged = null;
        }

        void DisposeWorkDirWatcher()
        {
            _fswWorkDirChanged?.Dispose();
            _fswWorkDirChanged = null;
        }

        public enum FileTaskAction
        {
            /// <summary>
            /// Создание файла или папки.
            /// </summary>
            CREATED,

            /// <summary>
            /// Удаление файла или папки.
            /// </summary>
            REMOVED,

            /// <summary>
            ///   Изменение файла или папки.
            ///   Типы изменений включают: изменения размера, атрибутов, параметры безопасности, последняя запись и время последнего доступа.
            /// </summary>
            CHANGED,

            /// <summary>
            /// Переименование файла или папки.
            /// </summary>
            RENAMED,

            /// <summary>
            /// Удаление всех файлов.
            /// </summary>
            CLEARED
        }

        /// <summary>
        ///     Содержит данные о задаче, связанной с изменениями в файловой системе.
        /// </summary>
        public class FileTask
        {
            /// <summary>
            ///     Изменения, возникшие в файле или папке.
            /// </summary>
            public FileTaskAction Type { get; }

            public ConcurrentProcessorStorage Storage { get; }

            /// <summary>
            ///     Инициализирует новый экземпляр параметрами добавляемой задачи.
            ///     Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="storage">Указывает тип источника данных (либо распознаваемые изображения, либо искомые).</param>
            public FileTask(FileTaskAction changes, ConcurrentProcessorStorage storage)
            {
                Type = changes;
                Storage = storage;
            }
        }

        public class Common : FileTask
        {
            public Common(FileTaskAction changes, ConcurrentProcessorStorage storage, string path) : base(changes, storage)
            {
                Path = path;
            }

            /// <summary>
            ///     Путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string Path { get; }
        }

        public sealed class Renamed : Common
        {
            /// <summary>
            ///     Инициализирует новый экземпляр параметрами добавляемой задачи.
            ///     Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="storage">Указывает тип источника данных (либо распознаваемые изображения, либо искомые).</param>
            /// <param name="path">Путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="oldFilePath">Исходный путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="renamedTo">Указывает, был ли файл переименован в требуемуе для программы расширение.</param>
            /// <param name="renamedFrom">Указывает, был ли файл переименован из требуемуемого для программы расширения.</param>
            public Renamed(FileTaskAction changes, ConcurrentProcessorStorage storage, string path, string oldFilePath, bool renamedTo,
                bool renamedFrom) : base(changes, storage, path)
            {
                OldPath = oldFilePath;
                RenamedTo = renamedTo;
                RenamedFrom = renamedFrom;
            }

            /// <summary>
            ///     Исходный путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string OldPath { get; }

            /// <summary>
            ///     Указывает, был ли файл переименован в требуемуе для программы расширение.
            /// </summary>
            public bool RenamedTo { get; }

            /// <summary>
            ///     Указывает, был ли файл переименован из требуемуемого для программы расширения.
            /// </summary>
            public bool RenamedFrom { get; }
        }
    }
}
