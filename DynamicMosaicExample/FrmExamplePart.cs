using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynamicMosaic;
using DynamicParser;
using ThreadState = System.Threading.ThreadState;

namespace DynamicMosaicExample
{
    internal sealed partial class FrmExample : Form
    {
        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize = "Ждите   ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize1 = "Ждите.  ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize2 = "Ждите.. ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize3 = "Ждите...";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrLoading = "Загрузка   ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrLoading1 = "Загрузка.  ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrLoading2 = "Загрузка.. ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrLoading3 = "Загрузка...";





        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrPreparing = "Подготовка   ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrPreparing1 = "Подготовка.  ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrPreparing2 = "Подготовка.. ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrPreparing3 = "Подготовка...";





        /// <summary>
        ///     Текст ошибки в случае, если отсутствуют образы для поиска (распознавания).
        /// </summary>
        const string ImagesNoExists =
            @"Образы отсутствуют. Для их добавления и распознавания необходимо создать искомые образы, нажав кнопку 'Создать образ', затем добавить искомое слово, которое так или иначе можно составить из названий искомых образов. Затем необходимо нарисовать его в поле исходного изображения. Далее нажать кнопку 'Распознать'.";

        const string SaveImageQueryError = @"Необходимо написать какой-либо запрос, который будет использоваться в качестве имени файла изображения.";

        /// <summary>
        ///     Определяет шаг (в пикселях), на который изменяется ширина сканируемого (создаваемого) изображения при нажатии
        ///     кнопок сужения или расширения.
        /// </summary>
        const int WidthStep = 20;

        /// <summary>
        ///     Синхронизирует потоки, пытающиеся записать сообщение в лог-файл.
        /// </summary>
        static readonly object LogLockerObject = new object();

        /// <summary>
        ///     Служит для блокировки одновременного доступа к процедуре отображения сообщения об ошибке.
        /// </summary>
        static readonly object LogLocker = new object();

        /// <summary>
        ///     Указывает, было ли просмотрено сообщение о том, что в процессе работы программы уже произошла ошибка.
        /// </summary>
        static volatile bool _errorMessageIsShowed;

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);

        /// <summary>
        ///     Предназначена для хранения задач, связанных с изменениями в файловой системе.
        /// </summary>
        readonly ConcurrentQueue<FileTask> _concurrentFileTasks = new ConcurrentQueue<FileTask>();

        /// <summary>
        ///     Объект, наблюдающий за состоянием основной формы приложения.
        /// </summary>
        readonly CurrentState _currentState;

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        readonly Color _defaultColor = Color.White;

        /// <summary>
        ///     Поток, отвечающий за актуализацию содержимого карт <see cref="Processor" />.
        /// </summary>
        readonly Thread _fileThread;

        /// <summary>
        ///     Отражает статус работы потока, который служит для получения всех имеющихся на данный момент образов букв для
        ///     распознавания, в том числе, для актуализации их содержимого.
        /// </summary>
        readonly ManualResetEvent _fileActivity = new ManualResetEvent(false);

        readonly ManualResetEvent _preparingActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Уведомляет о необходимости запустить поток для обновления списка файлов изображений.
        /// </summary>
        readonly AutoResetEvent _needRefreshEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Хранит загруженные карты, которые требуется искать на основной карте.
        ///     Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ConcurrentProcessorStorage _imagesProcessorStorage = new ImageProcessorStorage();


        readonly RecognizeProcessorStorage _recognizeProcessorStorage;

        /// <summary>
        ///     Хранит значение свойства <see cref="GroupBox.Text" /> объекта <see cref="grpResults" />.
        /// </summary>
        readonly string _strGrpResults;

        /// <summary>
        ///     Текст кнопки "Распознать". Сохраняет исходное значение свойства <see cref="Button.Text" /> кнопки
        ///     <see cref="btnRecognizeImage" />.
        /// </summary>
        readonly string _strRecog;

        /// <summary>
        ///     Таймер для измерения времени, затраченного на распознавание.
        /// </summary>
        readonly Stopwatch _stwRecognize = new Stopwatch();

        /// <summary>
        ///     Содержит изначальное значение поля "Название" искомого образа буквы.
        /// </summary>
        readonly string _unknownSymbolName;

        /// <summary>
        ///     Содержит изначальное значение поля "Название" искомого образа в <see cref="DynamicReflex" />.
        /// </summary>
        readonly string _unknownSystemName;

        /// <summary>
        ///     Задаёт цвет и ширину для стирания в окне создания распознаваемого изображения.
        /// </summary>
        readonly Pen _whitePen;

        /// <summary>
        ///     Коллекция задействованных элементов <see cref="DynamicReflex" />.
        ///     Содержит <see cref="DynamicReflex" />, запрос, статус выполнения запроса, номер просматриваемой карты на данный момент.
        /// </summary>
        readonly List<(Processor[] processors, int reflexMapIndex, string systemName)> _recognizeResults = new List<(Processor[] processors, int reflexMapIndex, string systemName)>();

        /// <summary>
        ///     Отражает статус работы потока распознавания изображения.
        /// </summary>
        readonly ManualResetEvent _recognizerActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Поток, отвечающий за отображение процесса ожидания завершения операции.
        /// </summary>
        readonly Thread _workWaitThread;

        /// <summary>
        ///     Изображение, которое выводится в окне создания распознаваемого изображения.
        /// </summary>
        Bitmap _btmFront;

        Bitmap _savedCopy;

        string _savedQuery = string.Empty;

        string _savedRecognizePath = string.Empty;

        int _prevSelectedIndex = -1;

        /// <summary>
        ///     Отражает статус всех кнопок на данный момент.
        /// </summary>
        bool _buttonsEnabled = true;

        /// <summary>
        ///     Индекс <see cref="ImageRect" />, рассматриваемый в данный момент.
        /// </summary>
        int _currentImage;

        int _currentRecognizeProcIndex;

        bool _needInitRecognizeImage = true;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае - <see langword="false" />.
        /// </summary>
        bool _draw;

        readonly string _grpWordsDefaultValue;

        /// <summary>
        ///     Поверхность рисования в окне создания распознаваемого изображения.
        /// </summary>
        Graphics _grFront;

        /// <summary>
        ///     Сигнал остановки потокам, работающим на фоне.
        /// </summary>
        readonly ManualResetEvent _stopBackgroundThreadEventFlag = new ManualResetEvent(false);

        bool NeedStopBackground => _stopBackgroundThreadEventFlag.WaitOne(0);

        (Processor[] processors, int reflexMapIndex, string systemName) SelectedResult
        {
            get => _recognizeResults[lstResults.SelectedIndex];
            set => _recognizeResults[lstResults.SelectedIndex] = value;
        }

        /// <summary>
        ///     Поток, отвечающий за выполнение процедуры распознавания.
        /// </summary>
        Thread _recognizerThread;

        enum SourceChanged
        {
            IMAGES,
            RECOGNIZE
        }

        /// <summary>
        ///     Конструктор основной формы приложения.
        /// </summary>
        internal FrmExample()
        {
            try
            {
                InitializeComponent();
                _recognizeProcessorStorage = new RecognizeProcessorStorage(pbDraw.MinimumSize.Width, pbDraw.MaximumSize.Width, WidthStep, pbDraw.Height);
                Directory.CreateDirectory(SearchImagesPath);
                Directory.CreateDirectory(RecognizeImagesPath);
                _whitePen = new Pen(_defaultColor, 2.0f);
                _prevSelectedIndex = lstResults.SelectedIndex;
                _unknownSymbolName = txtSymbolPath.Text;
                _unknownSystemName = txtConSymbol.Text;
                _strRecog = btnRecognizeImage.Text;
                _strGrpResults = grpResults.Text;
                _grpWordsDefaultValue = grpWords.Text;
                ImageWidth = pbBrowse.Width;
                ImageHeight = pbBrowse.Height;
                _currentState = new CurrentState(this);
                btnNarrow.Click += _currentState.CriticalChange;
                btnWide.Click += _currentState.CriticalChange;
                btnClearImage.Click += _currentState.CriticalChange;
                btnLoadRecognizeImage.Click += _currentState.CriticalChange;
                txtWord.TextChanged += _currentState.WordChange;
                fswRecognizeChanged.Path = RecognizeImagesPath;
                fswRecognizeChanged.IncludeSubdirectories = true;
                fswRecognizeChanged.NotifyFilter =
                    NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
                fswRecognizeChanged.Filter = "*.*";
                fswImageChanged.Path = SearchImagesPath;
                fswImageChanged.IncludeSubdirectories = true;
                fswImageChanged.NotifyFilter =
                    NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
                fswImageChanged.Filter = "*.*";

                void NeedDelete(string fullPath, SourceChanged source)
                {
                    ConcurrentProcessorStorage storage;

                    switch (source)
                    {
                        case SourceChanged.IMAGES:
                            storage = _imagesProcessorStorage;
                            break;
                        case SourceChanged.RECOGNIZE:
                            storage = _recognizeProcessorStorage;
                            break;
                        default:
                            throw new ArgumentException($@"Неизвестное значение {nameof(SourceChanged)}: {source}", nameof(source));
                    }

                    foreach ((Processor _, string path) in storage.Elements)
                        if (path.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
                            _concurrentFileTasks.Enqueue(new FileTask(WatcherChangeTypes.Deleted, path, string.Empty, false, false, source));
                }

                void NeedCreate(string fullPath, SourceChanged source)
                {
                    string[] paths = GetFiles(fullPath);
                    if (paths == null || paths.Length < 1)
                        return;
                    foreach (string path in paths)
                        _concurrentFileTasks.Enqueue(new FileTask(WatcherChangeTypes.Created, path, string.Empty, false, false, source));
                }

                void OnChanged(FileSystemEventArgs e, SourceChanged source) => SafetyExecute(() =>
                {
                    if (string.IsNullOrWhiteSpace(e.FullPath))
                        return;
                    if (string.Compare(Path.GetExtension(e.FullPath), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        ThreadPool.QueueUserWorkItem(state => SafetyExecute(() =>
                        {
                            (WatcherChangeTypes type, string fullPath, SourceChanged src) = ((WatcherChangeTypes, string, SourceChanged))state;

                            switch (type)
                            {
                                case WatcherChangeTypes.Deleted:
                                    NeedDelete(fullPath, src);
                                    _needRefreshEvent.Set();
                                    return;
                                case WatcherChangeTypes.Created:
                                    NeedCreate(fullPath, src);
                                    _needRefreshEvent.Set();
                                    return;
                            }
                        }), (e.ChangeType, e.FullPath, source));
                        return;
                    }

                    _concurrentFileTasks.Enqueue(new FileTask(e.ChangeType, e.FullPath, string.Empty, false, false, source));
                    _needRefreshEvent.Set();
                });

                void OnRenamed(RenamedEventArgs e, SourceChanged source) => SafetyExecute(() =>
                {
                    if (string.IsNullOrWhiteSpace(e.FullPath) || string.IsNullOrWhiteSpace(e.OldFullPath))
                        return;
                    bool renamedTo = string.Compare(Path.GetExtension(e.FullPath), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0;
                    bool renamedFrom = string.Compare(Path.GetExtension(e.OldFullPath), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0;
                    if (!renamedTo && !renamedFrom)
                    {
                        ThreadPool.QueueUserWorkItem(state => SafetyExecute(() =>
                        {
                            (string oldFullPath, string newFullPath, SourceChanged src) = ((string, string, SourceChanged))state;
                            NeedDelete(oldFullPath, src);
                            NeedCreate(newFullPath, src);
                            _needRefreshEvent.Set();
                        }), (e.OldFullPath, e.FullPath, source));
                        return;
                    }

                    _concurrentFileTasks.Enqueue(new FileTask(e.ChangeType, e.FullPath, e.OldFullPath, renamedTo, renamedFrom, source));
                    _needRefreshEvent.Set();
                });

                void IntRecognizeOnChanged(object _, FileSystemEventArgs e) => SafetyExecute(() => OnChanged(e, SourceChanged.RECOGNIZE));
                void IntRecognizeOnRenamed(object _, RenamedEventArgs e) => SafetyExecute(() => OnRenamed(e, SourceChanged.RECOGNIZE));

                void IntImagesOnChanged(object _, FileSystemEventArgs e) => SafetyExecute(() => OnChanged(e, SourceChanged.IMAGES));
                void IntImagesOnRenamed(object _, RenamedEventArgs e) => SafetyExecute(() => OnRenamed(e, SourceChanged.IMAGES));

                fswRecognizeChanged.Changed += IntRecognizeOnChanged;
                fswRecognizeChanged.Created += IntRecognizeOnChanged;
                fswRecognizeChanged.Deleted += IntRecognizeOnChanged;
                fswRecognizeChanged.Renamed += IntRecognizeOnRenamed;

                fswImageChanged.Changed += IntImagesOnChanged;
                fswImageChanged.Created += IntImagesOnChanged;
                fswImageChanged.Deleted += IntImagesOnChanged;
                fswImageChanged.Renamed += IntImagesOnRenamed;

                fswRecognizeChanged.EnableRaisingEvents = true;
                fswImageChanged.EnableRaisingEvents = true;

                _fileThread = CreateFileRefreshThread();
                _workWaitThread = CreateWaitThread();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        ///     Получает значение, отражающее статус рабочего процесса по распознаванию изображения.
        /// </summary>
        bool IsRecognizing => _recognizerActivity.WaitOne(0);

        /// <summary>
        ///     Ширина образа для распознавания.
        /// </summary>
        internal static int ImageWidth { get; private set; }

        /// <summary>
        ///     Высота образа для распознавания.
        /// </summary>
        internal static int ImageHeight { get; private set; }

        internal static string ImagesFolder => "Images";

        internal static string RecognizeFolder => "Recognize";

        /// <summary>
        ///     Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor" />, поиск которых
        ///     будет осуществляться на основной карте.
        /// </summary>
        internal static string SearchImagesPath { get; } = Path.Combine(Application.StartupPath, ImagesFolder);

        internal static string RecognizeImagesPath { get; } = Path.Combine(Application.StartupPath, RecognizeFolder);

        /// <summary>
        ///     Расширение изображений, которые интерпретируются как карты <see cref="Processor" />.
        /// </summary>
        internal static string ExtImg => "bmp";

        /// <summary>
        ///     Отражает статус процесса актуализации содержимого карт с жёсткого диска.
        /// </summary>
        bool IsFileActivity => _fileActivity.WaitOne(0);

        bool IsPreparingActivity => _preparingActivity.WaitOne(0);

        /// <summary>
        ///     Отключает или включает доступность кнопок на время выполнения операции.
        /// </summary>
        bool EnableButtons
        {
            get => _buttonsEnabled;
            set
            {
                if (value == _buttonsEnabled)
                    return;

                InvokeAction(() =>
                {
                    pbDraw.Enabled = value;
                    btnImageCreate.Enabled = value;
                    btnImageDelete.Enabled = value;
                    btnImageUpToQueries.Enabled = value;
                    btnImagePrev.Enabled = value;
                    btnImageNext.Enabled = value;
                    txtImagesCount.Enabled = value;
                    txtWord.ReadOnly = !value;
                    btnLoadRecognizeImage.Enabled = value;
                    btnClearImage.Enabled = value && IsPainting;
                    btnPrevRecogImage.Enabled = value;
                    btnNextRecogImage.Enabled = value;
                    btnDeleteRecognizeImage.Enabled = value;

                    if (value)
                    {
                        btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
                        btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
                        return;
                    }

                    btnWide.Enabled = false;
                    btnNarrow.Enabled = false;
                });
                _buttonsEnabled = value;
            }
        }

        /// <summary>
        ///     Возвращает значение <see langword="true" /> в случае, если пользователь нарисовал что-либо в окне создания
        ///     исходного изображения.
        ///     В противном случае возвращает значение <see langword="false" />.
        /// </summary>
        bool IsPainting
        {
            get
            {
                for (int y = 0; y < _btmFront.Height; y++)
                    for (int x = 0; x < _btmFront.Width; x++)
                    {
                        Color c = _btmFront.GetPixel(x, y);
                        if (c.A != _defaultColor.A || c.R != _defaultColor.R || c.G != _defaultColor.G ||
                            c.B != _defaultColor.B)
                            return true;
                    }

                return false;
            }
        }

        /// <summary>
        ///     Получает список файлов изображений карт в указанной папке.
        ///     Это файлы с расширением <see cref="ExtImg" />.
        ///     В случае какой-либо ошибки возвращает <see langword="null" />.
        /// </summary>
        /// <param name="path">Путь, по которому требуется получить список файлов изображений карт.</param>
        /// <returns>Возвращает список файлов изображений карт в указанной папке.</returns>
        static string[] GetFiles(string path)
        {
            try
            {
                return Directory.EnumerateFiles(path, $"*.{ExtImg}", SearchOption.AllDirectories).Where(p =>
                        string.Compare(Path.GetExtension(p), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        ///     Останавливает процесс распознавания.
        ///     Возвращает значение <see langword="true" /> в случае успешной остановки процесса распознавания, в противном случае
        ///     возвращает значение <see langword="false" />, в том числе, если процесс распознавания не был запущен.
        /// </summary>
        /// <returns>
        ///     Возвращает значение <see langword="true" /> в случае успешной остановки процесса распознавания, в противном случае
        ///     возвращает значение <see langword="false" />, в том числе, если процесс распознавания не был запущен.
        /// </returns>
        bool StopRecognize()
        {
            if (!IsRecognizing && !IsPreparingActivity)
                return false;

            try
            {
                _recognizerThread.Abort();

                if (!_recognizerThread.Join(15000))
                    MessageBox.Show(this,
                        @"Во время остановки распознавания произошла ошибка: поток, отвечающий за распознавание, завис. Рекомендуется перезапустить программу.",
                        @"Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $@"Во время остановки распознавания произошла ошибка:{Environment.NewLine}{ex.Message}", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _recognizerThread = null;
            }

            return true;
        }

        /// <summary>
        ///     Представляет собой обёртку для метода, который может выдать исключение.
        ///     Преобразует текст исключения, добавляя указанное имя метода, который его выдал.
        /// </summary>
        /// <param name="act">Метод, который необходимо выполнить. <see langword="null" /> игнорируется.</param>
        /// <param name="name">Имя метода, которое будет необходимо добавить к тексту исключения.</param>
        static void ExceptionClause(Action act, string name)
        {
            try
            {
                act?.Invoke();
            }
            catch (Exception ex)
            {
                throw new Exception($@"{name}: {ex.Message}");
            }
        }

        /// <summary>
        ///     Создаёт новый поток для обновления списка файлов изображений, в случае, если поток (<see cref="_fileThread" />) не
        ///     выполняется.
        ///     Созданный поток находится в состояниях <see cref="ThreadState.Unstarted" /> и <see cref="ThreadState.Background" />
        ///     .
        ///     Поток служит для получения всех имеющихся на данный момент образов букв для распознавания, в том числе, для
        ///     актуализации их содержимого.
        ///     Возвращает экземпляр созданного потока или <see langword="null" />, в случае, этот поток выполняется.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null" />, в случае, этот поток выполняется.</returns>
        Thread CreateFileRefreshThread()
        {
            if (IsFileActivity)
                return null;

            return new Thread(() => SafetyExecute(() =>
            {
                try
                {
                    _fileActivity.Set();
                    try
                    {
                        ThreadPool.GetMinThreads(out _, out int comPortMin);
                        ThreadPool.SetMinThreads(Environment.ProcessorCount * 3, comPortMin);
                        ThreadPool.GetMaxThreads(out _, out int comPortMax);
                        ThreadPool.SetMaxThreads(Environment.ProcessorCount * 15, comPortMax);

                        void Execute(ConcurrentProcessorStorage storage, string searchPath) => SafetyExecute(() => Parallel.ForEach(Directory.EnumerateFiles(searchPath, $"*.{ExtImg}", SearchOption.AllDirectories), (fullPath, state) =>
                        {
                            try
                            {
                                if (NeedStopBackground || state.IsStopped)
                                {
                                    state.Stop();
                                    return;
                                }

                                if (!string.IsNullOrWhiteSpace(fullPath) &&
                                    string.Compare(Path.GetExtension(fullPath), $".{ExtImg}",
                                        StringComparison.OrdinalIgnoreCase) == 0)
                                    ExceptionClause(() => storage.AddProcessor(fullPath),
                                        $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> (addition) -> {fullPath}");
                            }
                            catch (Exception ex)
                            {
                                WriteLogMessage(ex.Message);
                            }
                        }));

                        Execute(_imagesProcessorStorage, SearchImagesPath);
                        Execute(_recognizeProcessorStorage, RecognizeImagesPath);
                    }
                    finally
                    {
                        _fileActivity.Reset();
                    }

                    while (!NeedStopBackground)
                    {
                        _needRefreshEvent.WaitOne();
                        _fileActivity.Set();
                        try
                        {
                            while (!NeedStopBackground && _concurrentFileTasks.TryDequeue(out FileTask task))
                            {
                                void Execute(ConcurrentProcessorStorage storage) => SafetyExecute(() =>
                                {
                                    try
                                    {
                                        switch (task.TaskType)
                                        {
                                            case WatcherChangeTypes.Deleted:
                                                if (!File.Exists(task.FilePath))
                                                    ExceptionClause(
                                                        () => storage.RemoveProcessor(task.FilePath),
                                                        $"{nameof(ConcurrentProcessorStorage.RemoveProcessor)} -> {task.TaskType} -> {task.FilePath}");
                                                break;
                                            case WatcherChangeTypes.Renamed:
                                                if (task.RenamedFrom)
                                                    ExceptionClause(
                                                        () => storage.RemoveProcessor(task.OldFilePath),
                                                        $"{nameof(ConcurrentProcessorStorage.RemoveProcessor)} -> {task.TaskType} -> {task.OldFilePath}");
                                                if (task.RenamedTo)
                                                    ExceptionClause(() => storage.AddProcessor(task.FilePath),
                                                        $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> {task.TaskType} -> {task.FilePath}");
                                                break;
                                            case WatcherChangeTypes.Created:
                                            case WatcherChangeTypes.Changed:
                                            case WatcherChangeTypes.All:
                                                ExceptionClause(() => storage.AddProcessor(task.FilePath),
                                                    $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> {task.TaskType} -> {task.FilePath}");
                                                break;
                                            default:
                                                throw new ArgumentOutOfRangeException(
                                                    $"{nameof(task)} -> {task.TaskType} -> {task.FilePath}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteLogMessage(ex.Message);
                                    }
                                });

                                switch (task.Source)
                                {
                                    case SourceChanged.IMAGES:
                                        Execute(_imagesProcessorStorage);
                                        break;
                                    case SourceChanged.RECOGNIZE:
                                        Execute(_recognizeProcessorStorage);
                                        break;
                                    default:
                                        throw new Exception($@"Неизвестное значение {nameof(SourceChanged)}: {task.Source}");
                                }
                            }
                        }
                        finally
                        {
                            _fileActivity.Reset();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($@"{ex.Message}{Environment.NewLine}Список карт теперь обновляться не будет.");
                }
            }))
            {
                IsBackground = true,
                Name = "FileRefreshThread"
            };
        }

        /// <summary>
        ///     Записывает сообщение в лог-файл, в синхронном режиме.
        ///     Доступ к этому методу синхронизируется.
        ///     К сообщению автоматически прибавляется текущая дата в полном формате.
        /// </summary>
        /// <param name="logstr">Строка лога, которую надо записать.</param>
        void WriteLogMessage(string logstr)
        {
            void ShowMessage(string addMes)
            {
                if (_errorMessageIsShowed)
                    return;
                try
                {
                    if (!Monitor.TryEnter(LogLocker) || _errorMessageIsShowed)
                        return;
                    _errorMessageIsShowed = true;
                    ErrorMessageInOtherThread(string.IsNullOrWhiteSpace(addMes)
                        ? @"Содержимое лог-файла обновлено. Есть новые сообщения."
                        : addMes);
                }
                finally
                {
                    if (Monitor.IsEntered(LogLocker))
                        Monitor.Exit(LogLocker);
                }
            }

            try
            {
                logstr = $@"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {logstr}";
                string path = Path.Combine(Application.StartupPath, "DynamicMosaicExampleLog.log");
                lock (LogLockerObject)
                    using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write,
                        FileShare.ReadWrite | FileShare.Delete))
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                        sw.WriteLine(logstr);
                ShowMessage(string.Empty);
            }
            catch (Exception ex)
            {
                ShowMessage($@"Ошибка при записи логов: {ex.Message}{Environment.NewLine}Сообщение лога: {logstr}");
            }
        }

        /// <summary>
        ///     Состояние программы после распознавания карты.
        /// </summary>
        enum RecognizeState
        {
            /// <summary>
            ///     Неизвестно.
            ///     Этот статус означает, что распознавание ещё не было запущено.
            /// </summary>
            UNKNOWN,

            /// <summary>
            ///     Ошибка, слово изменено.
            ///     Вернуться из этого статуса в статус <see cref="ERROR" /> можно путём возвращения слова в предыдущее состояние,
            ///     какое оно было при выполнении процедуры распознавания.
            ///     Регистр не учитывается.
            /// </summary>
            ERRORWORD,

            /// <summary>
            ///     Успех, слово изменено.
            ///     Вернуться из этого статуса в статус <see cref="SUCCESS" /> можно путём возвращения слова в предыдущее состояние,
            ///     какое оно было при выполнении процедуры распознавания.
            ///     Регистр не учитывается.
            /// </summary>
            SUCCESSWORD,

            /// <summary>
            ///     Ошибка, условия не изменялись.
            /// </summary>
            ERROR,

            /// <summary>
            ///     Успех, условия не изменялись.
            /// </summary>
            SUCCESS
        }

        /// <summary>
        ///     Предназначен для отображения статуса программы.
        /// </summary>
        sealed class CurrentState
        {
            /// <summary>
            ///     Форма, за которой производится наблюдение.
            /// </summary>
            readonly FrmExample _curForm;

            /// <summary>
            ///     Текущее состояние программы.
            /// </summary>
            RecognizeState _state = RecognizeState.UNKNOWN;

            /// <summary>
            ///     Инициализирует объект-наблюдатель с указанием формы, за которой требуется наблюдение.
            ///     Форма не может быть равна <see langword="null" />.
            /// </summary>
            /// <param name="frm">Форма, за которой требуется наблюдение.</param>
            internal CurrentState(FrmExample frm)
            {
                _curForm = frm ?? throw new ArgumentNullException(nameof(frm));
            }

            /// <summary>
            ///     Искомое слово, написанное пользователем в момент запуска процедуры распознавания.
            /// </summary>
            internal string CurWord { get; private set; } = string.Empty;

            /// <summary>
            ///     Устанавливает текущее состояние программы.
            ///     Оно может быть установлено только, если текущее состояние равно <see cref="RecognizeState.UNKNOWN" />.
            ///     В других случаях новое состояние будет игнорироваться.
            ///     Установить можно либо <see cref="RecognizeState.ERROR" />, либо <see cref="RecognizeState.SUCCESS" />.
            /// </summary>
            internal RecognizeState State
            {
                set
                {
                    if (_state != RecognizeState.UNKNOWN)
                        return;
                    switch (value)
                    {
                        case RecognizeState.ERROR:
                            _curForm.pbSuccess.Image = Resources.Error_128;
                            _state = RecognizeState.ERROR;
                            return;
                        case RecognizeState.SUCCESS:
                            _curForm.pbSuccess.Image = Resources.OK_128;
                            _state = RecognizeState.SUCCESS;
                            return;
                    }
                }
            }

            /// <summary>
            ///     Используется для подписи на событие изменения критических данных, относящихся к распознаванию.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            internal void CriticalChange(object sender, EventArgs e)
            {
                _curForm.InvokeAction(() => _curForm.pbSuccess.Image = Resources.Unk_128);
                _state = RecognizeState.UNKNOWN;
            }

            /// <summary>
            ///     Используется для подписи на событие изменения искомого слова.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            internal void WordChange(object sender, EventArgs e)
            {
                switch (_state)
                {
                    case RecognizeState.ERROR:
                    case RecognizeState.SUCCESS:
                        if (string.Compare(CurWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) == 0)
                            return;
                        _curForm.pbSuccess.Image = Resources.Unk_128;
                        _state = _state == RecognizeState.ERROR ? RecognizeState.ERRORWORD : RecognizeState.SUCCESSWORD;
                        return;
                    case RecognizeState.ERRORWORD:
                    case RecognizeState.SUCCESSWORD:
                        if (string.Compare(CurWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) != 0)
                            return;
                        _curForm.pbSuccess.Image = _state == RecognizeState.ERRORWORD ? Resources.Error_128 : Resources.OK_128;
                        _state = _state == RecognizeState.ERRORWORD ? RecognizeState.ERROR : RecognizeState.SUCCESS;
                        return;
                    case RecognizeState.UNKNOWN:
                        CurWord = _curForm.txtWord.Text;
                        return;
                    default:
                        throw new Exception($@"Неизвестное состояние программы ({_state}).");
                }
            }
        }

        /// <summary>
        ///     Содержит данные о задаче, связанной с изменениями в файловой системе.
        /// </summary>
        readonly struct FileTask
        {
            /// <summary>
            ///     Изменения, возникшие в файле или папке.
            /// </summary>
            public WatcherChangeTypes TaskType { get; }

            /// <summary>
            ///     Путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string FilePath { get; }

            /// <summary>
            ///     Исходный путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string OldFilePath { get; }

            /// <summary>
            ///     Указывает, был ли файл переименован в требуемуе для программы расширение.
            /// </summary>
            public bool RenamedTo { get; }

            /// <summary>
            ///     Указывает, был ли файл переименован из требуемуемого для программы расширения.
            /// </summary>
            public bool RenamedFrom { get; }

            public SourceChanged Source { get; }

            /// <summary>
            ///     Инициализирует новый экземпляр параметрами добавляемой задачи.
            ///     Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="filePath">Путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="oldFilePath">Исходный путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="renamedTo">Указывает, был ли файл переименован в требуемуе для программы расширение.</param>
            /// <param name="renamedFrom">Указывает, был ли файл переименован из требуемуемого для программы расширения.</param>
            /// <param name="source">Указывает тип источника данных (либо распознаваемые изображения, либо искомые).</param>
            public FileTask(WatcherChangeTypes changes, string filePath, string oldFilePath, bool renamedTo,
                bool renamedFrom, SourceChanged source)
            {
                TaskType = changes;
                FilePath = filePath;
                OldFilePath = oldFilePath;
                RenamedTo = renamedTo;
                RenamedFrom = renamedFrom;
                Source = source;
            }
        }
    }
}