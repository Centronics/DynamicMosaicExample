using System;
using System.IO;
using ProcStorType = DynamicMosaicExample.ConcurrentProcessorStorage.ProcessorStorageType;

namespace DynamicMosaicExample
{
    internal sealed partial class FrmExample
    {
        /// <summary>
        /// Предоставляет сведения об изменении, произошедшем в файловой системе.
        /// Используется при постановке задач по изменнию коллекции, в связи с изменениями, произошедшими в файловой системе.
        /// </summary>
        public enum FileTaskAction
        {
            /// <summary>
            ///     Создание файла или папки.
            /// </summary>
            CREATED,

            /// <summary>
            ///     Удаление файла или папки.
            /// </summary>
            REMOVED,

            /// <summary>
            ///     Изменение файла или папки.
            ///     Типы изменений включают: изменения размера, атрибутов, параметры безопасности, последняя запись и время последнего
            ///     доступа.
            /// </summary>
            CHANGED,

            /// <summary>
            ///     Переименование файла или папки.
            /// </summary>
            RENAMED,

            /// <summary>
            ///     Удаление всех файлов.
            /// </summary>
            CLEARED
        }

        /// <summary>
        /// Получает хранилище, на которое указывают предоставленные сведения о нём.
        /// </summary>
        /// <param name="source">Хранилище, которое необходимо получить.</param>
        /// <param name="fullPath">Путь к хранилищу.</param>
        /// <returns>Возвращает требуемое хранилище или <see langword="null"/>, если оно не было найдено по указанным признакам.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Если хранилище указано как <see cref="SourceChanged.WORKDIR"/>, оно определяется с помощью метода <see cref="ConcurrentProcessorStorage.IsWorkingDirectory(string, bool)"/>, при этом, <paramref name="fullPath"/> должен быть обязательно указан.
        /// Если хранилище НЕ указано как <see cref="SourceChanged.WORKDIR"/>, <paramref name="fullPath"/> указывать необязательно.
        /// </remarks>
        ConcurrentProcessorStorage GetStorage(SourceChanged source, string fullPath)
        {
            switch (source)
            {
                case SourceChanged.IMAGES:
                    return _imagesProcessorStorage;
                case SourceChanged.RECOGNIZE:
                    return _recognizeProcessorStorage;
                case SourceChanged.WORKDIR:
                    if (_imagesProcessorStorage.IsWorkingDirectory(fullPath, true))
                        return _imagesProcessorStorage;

                    return _recognizeProcessorStorage.IsWorkingDirectory(fullPath, true)
                        ? _recognizeProcessorStorage
                        : null;
            }

            throw new ArgumentException(
                $@"Неизвестно, в каком хранилище требуется произвести изменение {nameof(SourceChanged)}: {source}",
                nameof(source));
        }

        /// <summary>
        /// Помещает в очередь задачу (<see cref="FileTask"/>) на очистку (<see cref="FileTaskAction.CLEARED"/>) указанного хранилища (<see cref="ConcurrentProcessorStorage.Clear()"/>).
        /// </summary>
        /// <param name="storage">Хранилище, которое требуется очистить.</param>
        /// <remarks>
        /// Если хранилище не указано (<see langword="null"/>), вызов игнорируется.
        /// Задание будет выполнено, когда до него дойдёт очередь.
        /// В случае, если <see cref="ConcurrentProcessorStorage.StorageType"/> является <see cref="ProcStorType.IMAGE"/>, тестируемый <see cref="Recognizer"/> будет сброшен (<see langword="null"/>).
        /// Метод потокобезопасен.
        /// </remarks>
        /// <seealso cref="FileTask"/>
        /// <seealso cref="FileTaskAction"/>
        /// <seealso cref="ProcStorType"/>
        /// <seealso cref="ConcurrentProcessorStorage.StorageType"/>
        void EnqueueRemoveStorage(ConcurrentProcessorStorage storage)
        {
            if (storage == null)
                return;

            _concurrentFileTasks.Enqueue(new FileTask(FileTaskAction.CLEARED, storage));

            if (storage.StorageType == ProcStorType.IMAGE)
                RefreshRecognizer();

            _refreshEvent.Set();
        }

        /// <summary>
        /// Помещает в очередь задачу (<see cref="Common"/>) на удаление карт, содержащийся в определённой папке (<see cref="FileTaskAction.REMOVED"/>), из указанного хранилища (<see cref="ConcurrentProcessorStorage.RemoveProcessor(string)"/>).
        /// </summary>
        /// <param name="fullPath">Путь к папке с картами.</param>
        /// <param name="storage">Хранилище, из которого требуется удалить карты.</param>
        /// <remarks>
        /// Если хранилище не указано (<see langword="null"/>), вызов игнорируется.
        /// Задание будет выполнено, когда до него дойдёт очередь.
        /// В случае, если <see cref="ConcurrentProcessorStorage.StorageType"/> является <see cref="ProcStorType.IMAGE"/>, тестируемый <see cref="Recognizer"/> будет сброшен (<see langword="null"/>).
        /// Метод автоматически добавляет разделитель каталогов в конец пути, используя метод <see cref="ConcurrentProcessorStorage.AddEndingSlash"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        /// <seealso cref="Common"/>
        /// <seealso cref="FileTaskAction"/>
        /// <seealso cref="ProcStorType"/>
        /// <seealso cref="ConcurrentProcessorStorage.StorageType"/>
        /// <seealso cref="ConcurrentProcessorStorage.AddEndingSlash"/>
        void EnqueueRemove(string fullPath, ConcurrentProcessorStorage storage)
        {
            if (storage == null)
                return;

            _concurrentFileTasks.Enqueue(new Common(FileTaskAction.REMOVED, storage,
                ConcurrentProcessorStorage.AddEndingSlash(fullPath)));

            if (storage.StorageType == ProcStorType.IMAGE)
                RefreshRecognizer();

            _refreshEvent.Set();
        }

        /// <summary>
        /// Помещает в очередь задачу (<see cref="Common"/>) на добавление (<see cref="FileTaskAction.CREATED"/>) карт в указанное хранилище (<see cref="ConcurrentProcessorStorage.AddProcessor(string)"/>), находящихся в указанной папке.
        /// </summary>
        /// <param name="fullPath">Путь к папке с картами.</param>
        /// <param name="storage">Хранилище, в которое необходимо добавить карты.</param>
        /// <remarks>
        /// Если хранилище не указано (<see langword="null"/>), вызов будет игнорирован.
        /// Метод определяет, куда указывает <paramref name="fullPath"/>, с помощью метода <see cref="ConcurrentProcessorStorage.IsDirectory(string)"/>.
        /// Если он не указывает на папку, вызов будет игнорирован.
        /// Задание будет выполнено, когда до него дойдёт очередь.
        /// В случае, если <see cref="ConcurrentProcessorStorage.StorageType"/> является <see cref="ProcStorType.IMAGE"/>, тестируемый <see cref="Recognizer"/> будет сброшен (<see langword="null"/>).
        /// Метод автоматически добавляет разделитель каталогов в конец пути (<paramref name="fullPath"/>), используя метод <see cref="ConcurrentProcessorStorage.AddEndingSlash"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        /// <seealso cref="Common"/>
        /// <seealso cref="FileTaskAction"/>
        /// <seealso cref="ProcStorType"/>
        /// <seealso cref="ConcurrentProcessorStorage.IsDirectory(string)"/>
        /// <seealso cref="ConcurrentProcessorStorage.StorageType"/>
        /// <seealso cref="ConcurrentProcessorStorage.AddEndingSlash"/>
        void EnqueueCreate(string fullPath, ConcurrentProcessorStorage storage)
        {
            if (storage == null)
                return;

            if (!ConcurrentProcessorStorage.IsDirectory(fullPath))
                return;

            _concurrentFileTasks.Enqueue(new Common(FileTaskAction.CREATED, storage,
                ConcurrentProcessorStorage.AddEndingSlash(fullPath)));

            if (storage.StorageType == ProcStorType.IMAGE)
                RefreshRecognizer();

            _refreshEvent.Set();
        }

        /// <summary>
        /// Преобразует тип данных из стандартного типа <see cref="WatcherChangeTypes"/> во внутренний тип <see cref="FileTaskAction"/>.
        /// </summary>
        /// <param name="c">Значение <see cref="WatcherChangeTypes"/>, которое требуется преобразовать.</param>
        /// <returns>Возвращает значение, преобразованное в <see cref="FileTaskAction"/>.</returns>
        /// <remarks>
        /// Преобразует следующие величины:
        /// 1) <see cref="WatcherChangeTypes.Created"/>,
        /// 2) <see cref="WatcherChangeTypes.Deleted"/>,
        /// 3) <see cref="WatcherChangeTypes.Changed"/>,
        /// 4) <see cref="WatcherChangeTypes.Renamed"/>.
        /// В остальных случаях метод выбросит исключение <see cref="ArgumentOutOfRangeException"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"/>
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

        /// <summary>
        /// Выполняет активацию или дезактивацию наблюдения за состоянием каталога хранилища.
        /// </summary>
        /// <param name="storage">Хранилище, которое необходимо отслеживать.</param>
        /// <param name="enable">Флаг активации или дезактивации отслеживания.</param>
        /// <remarks>
        /// Если хранилище не указано (<see langword="null"/>), вызов игнорируется.
        /// Отслеживается рабочий каталог хранилища (<see cref="ConcurrentProcessorStorage.WorkingDirectory"/>).
        /// Метод может использоваться только в том потоке, в котором была создана форма <see cref="FrmExample"/>.
        /// Разработан для решения проблемы с удалением (перемещением) или повторным созданием рабочего каталога.
        /// Проблема была в том, что отслеживание прекращалось сразу после удаления, переименования или перемещения каталога.
        /// Для этой цели было создано "хранилище хранилищ" (<see cref="SourceChanged.WORKDIR"/>).
        /// В случае, если каталог удалён (или переименован), то его отслеживание необходимо отключить.
        /// Если рабочий каталог хранилища появился, необходимо начать его отслеживать.
        /// Поддерживает только два типа хранилища (<see cref="ConcurrentProcessorStorage.StorageType"/>): <see cref="ProcStorType.IMAGE"/> и <see cref="ProcStorType.RECOGNIZE"/>.
        /// В случае неподдерживаемого типа хранилища, метод выбрасывает исключение <see cref="ArgumentOutOfRangeException"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <seealso cref="ConcurrentProcessorStorage.StorageType"/>
        /// <seealso cref="ProcStorType.IMAGE"/>
        /// <seealso cref="ProcStorType.RECOGNIZE"/>
        /// <seealso cref="SourceChanged.WORKDIR"/>
        /// <seealso cref="ConcurrentProcessorStorage.WorkingDirectory"/>
        void TrackStorage(ConcurrentProcessorStorage storage, bool enable)
        {
            if (storage == null)
                return;

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
        }

        /// <summary>
        /// Обрабатывает нештатные ситуации при переименовании в каталоге хранилища.
        /// </summary>
        /// <param name="oldFullPath">Путь к папке, в которой произошло переименование, к примеру, самой папки. Она будет удалена из хранилища (вместе с содержимым).</param>
        /// <param name="newFullPath">Новый путь к папке с картами (после переименования). Карты будут добавлены в хранилище.</param>
        /// <param name="source">Тип хранилища, в котором произошли изменения.</param>
        /// <param name="oldStorage">Если путь существовал ранее, его необходимо указать, иначе <see langword="null"/>.</param>
        /// <param name="newStorage">Если рабочий каталог (<see cref="ConcurrentProcessorStorage.WorkingDirectory"/>) хранилища появился, то необходимо указать путь к нему. Иначе <see langword="null"/>.</param>
        /// <remarks>
        /// Эти ситуации связаны с переименованием не только файлов карт (тех, что с расширением <see cref="ExtImg"/>), но и папок, и даже папок самих хранилищ (<see cref="SourceChanged.WORKDIR"/>).
        /// Выполняет различные действия (в случае <see cref="SourceChanged.WORKDIR"/>), в зависимости от ситуации, например, очистка хранилища, загрузка карт в него или <see cref="TrackStorage(ConcurrentProcessorStorage, bool)"/>.
        /// Может добавлять или удалять карты из хранилища, в случае, если не требуется обслуживать само хранилище.
        /// Метод только ставит задачи, которые выполняются посредством очереди, в отдельном потоке. Сам метод не выполняет никакие операции над хранилищами.
        /// Метод может выполняться только в потоке, в котором была создана форма <see cref="FrmExample"/>.
        /// Метод не выбрасывает исключения.
        /// </remarks>
        /// <seealso cref="ConcurrentProcessorStorage.WorkingDirectory"/>
        /// <seealso cref="ExtImg"/>
        /// <seealso cref="TrackStorage(ConcurrentProcessorStorage, bool)"/>
        /// <seealso cref="SourceChanged.WORKDIR"/>
        void RenamedThreadFunction(string oldFullPath, string newFullPath, SourceChanged source,
            ConcurrentProcessorStorage oldStorage, ConcurrentProcessorStorage newStorage)
        {
            SafeExecute(() =>
            {
                if (source != SourceChanged.WORKDIR)
                {
                    EnqueueRemove(oldFullPath, oldStorage);
                    EnqueueCreate(newFullPath, oldStorage);
                    return;
                }

                if (oldStorage != null)
                {
                    TrackStorage(oldStorage, false);
                    EnqueueRemoveStorage(oldStorage);
                }

                if (newStorage == null)
                    return;

                TrackStorage(newStorage, true);
                EnqueueCreate(newFullPath, newStorage);
            });
        }

        /// <summary>
        /// Обрабатывает нештатные ситуации при изменении в каталоге хранилища.
        /// </summary>
        /// <param name="type">Произошедшее изменение.</param>
        /// <param name="fullPath">Путь к папке, в которой произошли изменения.</param>
        /// <param name="storage">Хранилище, в рабочем каталоге (<see cref="ConcurrentProcessorStorage.WorkingDirectory"/>) которого произошли изменения. Может быть <see langword="null"/>, тогда вызов будет игнорирован.</param>
        /// <param name="source">Тип хранилища, в котором произошли изменения.</param>
        /// <remarks>
        /// Примеры обрабатываемых ситуаций: изменение содержимого вложенной папки (только если это не карты), удаление или создание рабочего каталога хранилища <see cref="ConcurrentProcessorStorage.WorkingDirectory"/>.
        /// В случае создания или удаления рабочего каталога хранилища (<see cref="SourceChanged.WORKDIR"/>), испольуется метод <see cref="TrackStorage(ConcurrentProcessorStorage, bool)"/>.
        /// Эти ситуации связаны с изменением любых сущностей, кроме файлов карт (тех, что с расширением <see cref="ExtImg"/>).
        /// Поддерживаются только следующие значения:
        /// 1) <see cref="WatcherChangeTypes.Deleted"/>,
        /// 2) <see cref="WatcherChangeTypes.Created"/>.
        /// Метод можно использовать только в том потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Может добавлять или удалять карты из хранилища, в случае, если не требуется обслуживать само хранилище.
        /// Метод только ставит задачи, которые выполняются посредством очереди, в отдельном потоке. Сам метод не выполняет никакие операции над хранилищами.
        /// Метод не выбрасывает исключения.
        /// </remarks>
        /// <seealso cref="ConcurrentProcessorStorage.WorkingDirectory"/>
        /// <seealso cref="ExtImg"/>
        /// <seealso cref="TrackStorage(ConcurrentProcessorStorage, bool)"/>
        /// <seealso cref="SourceChanged.WORKDIR"/>
        void ChangedThreadFunction(WatcherChangeTypes type, string fullPath, ConcurrentProcessorStorage storage,
            SourceChanged source)
        {
            SafeExecute(() =>
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
                        {
                            EnqueueRemove(fullPath, storage);
                        }

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
                        throw new ArgumentOutOfRangeException(nameof(type), type,
                            $@"{nameof(ChangedThreadFunction)}: Неизвестный тип изменения.");
                }
            });
        }

        /// <summary>
        /// Функция-обёртка для обработки события изменения содержимого какого-либо хранилища (<see cref="SourceChanged"/>).
        /// </summary>
        /// <param name="e">Сведения о событии файловой системы.</param>
        /// <param name="source">Сведения о хранилище, в котором произошло событие.</param>
        /// <remarks>
        /// Функция обрабатывает событие как можно быстрее, помещая задачи в очередь, определив целевое хранилище.
        /// Далее выполнение поставленных задач происходит в отдельном потоке.
        /// Если хранилище не определено, вызов игнорируется.
        /// В случае, если <see cref="ConcurrentProcessorStorage.StorageType"/> является <see cref="ProcStorType.IMAGE"/>, тестируемый <see cref="Recognizer"/> будет сброшен (<see langword="null"/>).
        /// Функция не выбрасывает исключения.
        /// Функция может использоваться только в том потоке, в котором была создана форма <see cref="FrmExample"/>.
        /// </remarks>
        void OnChanged(FileSystemEventArgs e, SourceChanged source)
        {
            SafeExecute(() =>
            {
                if (string.IsNullOrWhiteSpace(e.FullPath))
                    return;

                ConcurrentProcessorStorage storage = GetStorage(source, e.FullPath);

                if (storage == null)
                    return;

                if (!storage.IsProcessorFile(e.FullPath))
                {
                    ChangedThreadFunction(e.ChangeType, e.FullPath, storage, source);
                    return;
                }

                _concurrentFileTasks.Enqueue(new Common(ConvertWatcherChangeTypes(e.ChangeType), storage, e.FullPath));

                if (storage.StorageType == ProcStorType.IMAGE)
                    RefreshRecognizer();

                _refreshEvent.Set();
            });
        }

        /// <summary>
        /// Функция-обёртка для события перименования файлов и папок в хранилище.
        /// </summary>
        /// <param name="e">Сведения о произошедшем событии в файловой системе.</param>
        /// <param name="source">Сведения о хранилище, в котором произошло событие.</param>
        /// <remarks>
        /// Функция может использоваться только в том потоке, в котором была создана форма <see cref="FrmExample"/>.
        /// В случае, если <see cref="ConcurrentProcessorStorage.StorageType"/> какого-либо хранилища является <see cref="ProcStorType.IMAGE"/>, тестируемый <see cref="Recognizer"/> будет сброшен (<see langword="null"/>).
        /// Функция обрабатывает событие асинхронно, не производя никаких операций непосредственно.
        /// Значение <see cref="SourceChanged.WORKDIR"/> поддерживается.
        /// Функция не выбрасывает исключения.
        /// </remarks>
        void OnRenamed(RenamedEventArgs e, SourceChanged source)
        {
            SafeExecute(() =>
            {
                if (string.IsNullOrWhiteSpace(e.FullPath) || string.IsNullOrWhiteSpace(e.OldFullPath))
                    return;

                ConcurrentProcessorStorage oldStorage = GetStorage(source, e.OldFullPath);
                ConcurrentProcessorStorage newStorage = GetStorage(source, e.FullPath);

                bool renamedFrom = oldStorage != null;
                bool renamedTo = newStorage != null;

                if (!renamedTo && !renamedFrom)
                    return;

                if (!renamedTo || !renamedFrom)
                {
                    RenamedThreadFunction(e.OldFullPath, e.FullPath, source, oldStorage, newStorage);
                    return;
                }

                _concurrentFileTasks.Enqueue(new Renamed(ConvertWatcherChangeTypes(e.ChangeType), oldStorage,
                    e.FullPath, e.OldFullPath, true, true));

                if (oldStorage.StorageType == ProcStorType.IMAGE || newStorage.StorageType == ProcStorType.IMAGE)
                    RefreshRecognizer();

                _refreshEvent.Set();
            });
        }

        /// <summary>
        /// Функция-обёртка в виде обработчика событий для <see cref="FileSystemWatcher"/>.
        /// Обрабатывает хранилище <see cref="SourceChanged.RECOGNIZE"/>.
        /// Отслеживает все операции (<see cref="NotifyFilters.FileName"/> | <see cref="NotifyFilters.LastWrite"/> | <see cref="NotifyFilters.DirectoryName"/>), включая все подкаталоги.
        /// </summary>
        /// <param name="_">Игнорируется.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Функция не выбрасывает исключения.
        /// Обрабатывает следующие события:
        /// 1) <see cref="FileSystemWatcher.Changed"/>,
        /// 2) <see cref="FileSystemWatcher.Created"/>,
        /// 3) <see cref="FileSystemWatcher.Deleted"/>.
        /// </remarks>
        void IntRecognizeOnChanged(object _, FileSystemEventArgs e)
        {
            SafeExecute(() => OnChanged(e, SourceChanged.RECOGNIZE));
        }

        /// <summary>
        /// Функция-обёртка в виде обработчика событий для <see cref="FileSystemWatcher"/>.
        /// Обрабатывает хранилище <see cref="SourceChanged.RECOGNIZE"/>.
        /// Отслеживает все операции (<see cref="NotifyFilters.FileName"/> | <see cref="NotifyFilters.LastWrite"/> | <see cref="NotifyFilters.DirectoryName"/>), включая все подкаталоги.
        /// </summary>
        /// <param name="_">Игнорируется.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Функция не выбрасывает исключения.
        /// Обрабатывает событие <see cref="FileSystemWatcher.Renamed"/>.
        /// </remarks>
        void IntRecognizeOnRenamed(object _, RenamedEventArgs e)
        {
            SafeExecute(() => OnRenamed(e, SourceChanged.RECOGNIZE));
        }

        /// <summary>
        /// Функция-обёртка для обработки событий <see cref="FileSystemWatcher"/>.
        /// Обрабатывает хранилище <see cref="SourceChanged.IMAGES"/>.
        /// Отслеживает все операции (<see cref="NotifyFilters.FileName"/> | <see cref="NotifyFilters.LastWrite"/> | <see cref="NotifyFilters.DirectoryName"/>), включая все подкаталоги.
        /// </summary>
        /// <param name="_">Игнорируется.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Функция не выбрасывает исключения.
        /// Обрабатывает следующие события:
        /// 1) <see cref="FileSystemWatcher.Changed"/>,
        /// 2) <see cref="FileSystemWatcher.Created"/>,
        /// 3) <see cref="FileSystemWatcher.Deleted"/>.
        /// </remarks>
        void IntImagesOnChanged(object _, FileSystemEventArgs e)
        {
            SafeExecute(() => OnChanged(e, SourceChanged.IMAGES));
        }

        /// <summary>
        /// Функция-обёртка в виде обработчика событий для <see cref="FileSystemWatcher"/>.
        /// Обрабатывает хранилище <see cref="SourceChanged.IMAGES"/>.
        /// Отслеживает все операции (<see cref="NotifyFilters.FileName"/> | <see cref="NotifyFilters.LastWrite"/> | <see cref="NotifyFilters.DirectoryName"/>), включая все подкаталоги.
        /// </summary>
        /// <param name="_">Игнорируется.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Функция не выбрасывает исключения.
        /// Обрабатывает событие <see cref="FileSystemWatcher.Renamed"/>.
        /// </remarks>
        void IntImagesOnRenamed(object _, RenamedEventArgs e)
        {
            SafeExecute(() => OnRenamed(e, SourceChanged.IMAGES));
        }

        /// <summary>
        /// Функция-обёртка в виде обработчика событий для <see cref="FileSystemWatcher"/>.
        /// Обслуживает хранилища (<see cref="SourceChanged.WORKDIR"/>), находящиеся в рабочем каталоге приложения (<see cref="WorkingDirectory"/>).
        /// Отслеживает только переименование (<see cref="NotifyFilters.DirectoryName"/>) каталогов внутри <see cref="WorkingDirectory"/>, не включая подкаталоги.
        /// </summary>
        /// <param name="_">Игнорируется.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Функция не выбрасывает исключения.
        /// Обрабатывает следующие события:
        /// 1) <see cref="FileSystemWatcher.Changed"/>,
        /// 2) <see cref="FileSystemWatcher.Created"/>,
        /// 3) <see cref="FileSystemWatcher.Deleted"/>.
        /// </remarks>
        void IntWorkDirOnChanged(object _, FileSystemEventArgs e)
        {
            SafeExecute(() => OnChanged(e, SourceChanged.WORKDIR));
        }

        /// <summary>
        /// Функция-обёртка в виде обработчика событий для <see cref="FileSystemWatcher"/>.
        /// Обслуживает хранилища (<see cref="SourceChanged.WORKDIR"/>), находящиеся в рабочем каталоге приложения (<see cref="WorkingDirectory"/>).
        /// Отслеживает только переименование (<see cref="NotifyFilters.DirectoryName"/>) каталогов внутри <see cref="WorkingDirectory"/>, не включая подкаталоги.
        /// </summary>
        /// <param name="_">Игнорируется.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Функция не выбрасывает исключения.
        /// Обрабатывает событие <see cref="FileSystemWatcher.Renamed"/>.
        /// </remarks>
        void IntWorkDirOnRenamed(object _, RenamedEventArgs e)
        {
            SafeExecute(() => OnRenamed(e, SourceChanged.WORKDIR));
        }

        /// <summary>
        /// Активирует наблюдение за хранилищем <see cref="SourceChanged.RECOGNIZE"/>, с помощью <see cref="FileSystemWatcher"/>.
        /// </summary>
        /// <remarks>
        /// Метод может использоваться только в том же потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Если наблюдение за этим хранилищем уже производилось, оно будет перезапущено.
        /// Отслеживаемый путь: <see cref="RecognizeImagesPath"/>.
        /// Использует поле <see cref="_fswRecognizeChanged"/>.
        /// Для отключения наблюдения следует использовать метод <see cref="DisposeRecognizeWatcher()"/>.
        /// </remarks>
        /// <seealso cref="RecognizeImagesPath"/>
        /// <seealso cref="SourceChanged.RECOGNIZE"/>
        /// <seealso cref="FileSystemWatcher"/>
        /// <seealso cref="_fswRecognizeChanged"/>
        /// <seealso cref="DisposeRecognizeWatcher()"/>
        void CreateRecognizeWatcher()
        {
            DisposeRecognizeWatcher();

            FileSystemWatcher rw = new FileSystemWatcher();

            rw.BeginInit();
            rw.IncludeSubdirectories = true;
            rw.SynchronizingObject = this;
            rw.Path = RecognizeImagesPath;
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

        /// <summary>
        /// Активирует наблюдение за хранилищем <see cref="SourceChanged.IMAGES"/>, с помощью <see cref="FileSystemWatcher"/>.
        /// </summary>
        /// <remarks>
        /// Метод может использоваться только в том же потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Если наблюдение за этим хранилищем уже производилось, оно будет перезапущено.
        /// Отслеживаемый путь: <see cref="SearchImagesPath"/>.
        /// Использует поле <see cref="_fswImageChanged"/>.
        /// Для отключения наблюдения следует использовать метод <see cref="DisposeImageWatcher()"/>.
        /// </remarks>
        /// <seealso cref="SearchImagesPath"/>
        /// <seealso cref="SourceChanged.IMAGES"/>
        /// <seealso cref="FileSystemWatcher"/>
        /// <seealso cref="_fswImageChanged"/>
        /// <seealso cref="DisposeImageWatcher()"/>
        void CreateImageWatcher()
        {
            DisposeImageWatcher();

            FileSystemWatcher ic = new FileSystemWatcher();

            ic.BeginInit();
            ic.IncludeSubdirectories = true;
            ic.SynchronizingObject = this;
            ic.Path = SearchImagesPath;
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

        /// <summary>
        /// Активирует наблюдение за состоянием папок хранилищ (<see cref="SourceChanged.WORKDIR"/>), с помощью <see cref="FileSystemWatcher"/>.
        /// </summary>
        /// <remarks>
        /// Метод может использоваться только в том же потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Если наблюдение за этим хранилищем уже производилось, оно будет перезапущено.
        /// Отслеживаемый путь: <see cref="WorkingDirectory"/>.
        /// Использует поле <see cref="_fswWorkDirChanged"/>.
        /// Для отключения наблюдения следует использовать метод <see cref="DisposeWorkDirWatcher()"/>.
        /// </remarks>
        /// <seealso cref="WorkingDirectory"/>
        /// <seealso cref="SourceChanged.WORKDIR"/>
        /// <seealso cref="FileSystemWatcher"/>
        /// <seealso cref="_fswWorkDirChanged"/>
        /// <seealso cref="DisposeWorkDirWatcher()"/>
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

        /// <summary>
        /// Прекращает наблюдение за хранилищем <see cref="SourceChanged.RECOGNIZE"/>.
        /// </summary>
        /// <remarks>
        /// Этот метод может использоваться только в том же потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Его можно вызывать сколько угодно раз подряд, т.к. если наблюдение отключено, никакого эффекта не будет.
        /// Использует поле <see cref="_fswRecognizeChanged"/>.
        /// Для того, чтобы активировать наблюдение за этим хранилищем, используйте метод <see cref="CreateRecognizeWatcher()"/>.
        /// При завершении работы программы, этот метод необходимо вызывать в обязательном порядке.
        /// </remarks>
        /// <seealso cref="SourceChanged.RECOGNIZE"/>
        /// <seealso cref="_fswRecognizeChanged"/>
        /// <seealso cref="CreateRecognizeWatcher()"/>
        void DisposeRecognizeWatcher()
        {
            _fswRecognizeChanged?.Dispose();
            _fswRecognizeChanged = null;
        }

        /// <summary>
        /// Прекращает наблюдение за хранилищем <see cref="SourceChanged.IMAGES"/>.
        /// </summary>
        /// <remarks>
        /// Этот метод может использоваться только в том же потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Его можно вызывать сколько угодно раз подряд, т.к. если наблюдение отключено, никакого эффекта не будет.
        /// Использует поле <see cref="_fswImageChanged"/>.
        /// Для того, чтобы активировать наблюдение за этим хранилищем, используйте метод <see cref="CreateImageWatcher()"/>.
        /// При завершении работы программы, этот метод необходимо вызывать в обязательном порядке.
        /// </remarks>
        /// <seealso cref="SourceChanged.IMAGES"/>
        /// <seealso cref="_fswImageChanged"/>
        /// <seealso cref="CreateImageWatcher()"/>
        void DisposeImageWatcher()
        {
            _fswImageChanged?.Dispose();
            _fswImageChanged = null;
        }

        /// <summary>
        /// Прекращает наблюдение за хранилищем <see cref="SourceChanged.WORKDIR"/>.
        /// </summary>
        /// <remarks>
        /// Этот метод может использоваться только в том же потоке, в котором создана основная форма приложения <see cref="FrmExample"/>.
        /// Его можно вызывать сколько угодно раз подряд, т.к. если наблюдение отключено, никакого эффекта не будет.
        /// Использует поле <see cref="_fswWorkDirChanged"/>.
        /// Для того, чтобы активировать наблюдение за этим хранилищем, используйте метод <see cref="CreateWorkDirWatcher()"/>.
        /// При завершении работы программы, этот метод необходимо вызывать в обязательном порядке.
        /// </remarks>
        /// <seealso cref="SourceChanged.WORKDIR"/>
        /// <seealso cref="_fswWorkDirChanged"/>
        /// <seealso cref="CreateWorkDirWatcher()"/>
        void DisposeWorkDirWatcher()
        {
            _fswWorkDirChanged?.Dispose();
            _fswWorkDirChanged = null;
        }

        /// <summary>
        /// Предоставляет сведения о хранилище, над которым требуется произвести операцию.
        /// </summary>
        enum SourceChanged
        {
            /// <summary>
            /// Хранилище карт, которые требуется найти.
            /// </summary>
            IMAGES,

            /// <summary>
            /// Хранилище карт, на которых производится поиск данных.
            /// </summary>
            RECOGNIZE,

            /// <summary>
            /// Означает, что необходимо выполнить обслуживание самого хранилища.
            /// При этом, тип хранилища определяется по его рабочему каталогу (<see cref="ConcurrentProcessorStorage.WorkingDirectory"/>).
            /// </summary>
            WORKDIR
        }

        /// <summary>
        ///     Базовый класс.
        ///     Содержит основные сведения о задаче, связанной с изменениями данных в хранилищах.
        ///     Содержит сведения для обработки события <see cref="FileTaskAction.CLEARED"/>.
        /// </summary>
        public class FileTask
        {
            /// <summary>
            ///     Инициализирует новый экземпляр параметрами добавляемой задачи.
            ///     Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="storage">Хранилище, в котором требуется произвести изменения.</param>
            public FileTask(FileTaskAction changes, ConcurrentProcessorStorage storage)
            {
                Type = changes;
                Storage = storage;
            }

            /// <summary>
            ///     Изменения, возникшие в файле или папке.
            /// </summary>
            public FileTaskAction Type { get; }

            /// <summary>
            /// Хранилище, в котором требуется произвести изменения.
            /// </summary>
            public ConcurrentProcessorStorage Storage { get; }
        }

        /// <summary>
        /// Содержит сведения для обработки следующих событий: 1) <see cref="FileTaskAction.CHANGED"/> 2) <see cref="FileTaskAction.CREATED"/> 3) <see cref="FileTaskAction.REMOVED"/>.
        /// </summary>
        public class Common : FileTask
        {
            /// <summary>
            /// Инициализирует новый экземпляр параметром <paramref name="path"/>, остальные параметры добавляемой задачи передаются в базовый класс <see cref="FileTask"/>.
            /// Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="storage">Хранилище, в котором требуется произвести изменения.</param>
            /// <param name="path">Путь к файлу или папке, в которой произошли изменения.</param>
            public Common(FileTaskAction changes, ConcurrentProcessorStorage storage, string path) : base(changes,
                storage)
            {
                Path = path;
            }

            /// <summary>
            ///     Путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string Path { get; }
        }

        /// <summary>
        /// Содержит сведения для обработки события переименования файла или папки (<see cref="FileTaskAction.RENAMED"/>).
        /// </summary>
        public sealed class Renamed : Common
        {
            /// <summary>
            ///     Инициализирует новый экземпляр параметрами <paramref name="oldFilePath"/>, <paramref name="renamedTo"/>, <paramref name="renamedFrom"/>, остальные параметры добавляемой задачи передаются в базовый класс <see cref="Common"/>.
            ///     Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="storage">Хранилище, в котором требуется произвести изменения.</param>
            /// <param name="path">Путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="oldFilePath">Исходный путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="renamedTo">Указывает, находится ли путь <see cref="Common.Path"/> в каком-либо хранилище.</param>
            /// <param name="renamedFrom">Указывает, находился ли ранее путь <see cref="OldPath"/> в каком-либо хранилище.</param>
            public Renamed(FileTaskAction changes, ConcurrentProcessorStorage storage, string path, string oldFilePath,
                bool renamedTo,
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
            ///     Указывает, находится ли путь <see cref="Common.Path"/> в каком-либо хранилище.
            ///     Если значение <see langword="false"/>, <see cref="Common.Path"/> игнорируется.
            /// </summary>
            public bool RenamedTo { get; }

            /// <summary>
            ///     Указывает, находился ли ранее путь <see cref="OldPath"/> в каком-либо хранилище.
            ///     Если значение <see langword="false"/>, <see cref="OldPath"/> игнорируется.
            /// </summary>
            public bool RenamedFrom { get; }
        }
    }
}