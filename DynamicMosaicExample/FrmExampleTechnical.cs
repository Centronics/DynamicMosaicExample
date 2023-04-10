using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DynamicMosaic;
using DynamicParser;
using ThreadState = System.Threading.ThreadState;

namespace DynamicMosaicExample
{
    internal sealed partial class FrmExample
    {
        const string StrPreparing0 = @"Подготовка(/)";

        const string StrPreparing1 = @"Подготовка(~)";

        const string StrPreparing2 = @"Подготовка(|)";

        const string StrPreparing3 = @"Подготовка(\)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading0 = @"Загрузка(+)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading1 = @"Загрузка(-)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading2 = @"Загрузка(*)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading3 = @"Загрузка(/)";

        const string NeedSaveQuery = @"Перед запуском процедуры поиска необходимо сохранить текущий запрос.";

        const string SaveImageQueryError =
            @"Необходимо написать какой-либо запрос, который будет использоваться в качестве имени файла изображения.";

        const string QueryErrorSymbols = @"Запрос содержит недопустимые символы.";

        const string LogRefreshedMessage = @"Содержимое лог-файла обновлено. Есть новые сообщения.";

        const string SearchStopError =
            @"Во время остановки процесса поиска произошла ошибка. Программа будет завершена.";

        const string UnknownFSChangeType = @"Неизвестный тип изменения файловой системы.";

        /// <summary>
        ///     Синхронизирует потоки, пытающиеся записать сообщение в лог-файл.
        /// </summary>
        static readonly object LogLockerObject = new object();

        /// <summary>
        ///     Указывает, было ли просмотрено сообщение о том, что в процессе работы программы уже произошла ошибка.
        /// </summary>
        static volatile bool _errorMessageIsShowed;

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        public static readonly Pen BlackPen = new Pen(CheckAlphaColor(Color.Black), 2.0f);

        public static readonly Pen ImageFramePen = new Pen(Color.Black);

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        public static Color DefaultColor = CheckAlphaColor(Color.White);

        /// <summary>
        ///     Задаёт цвет и ширину для стирания в окне создания распознаваемого изображения.
        /// </summary>
        public static readonly Pen WhitePen = new Pen(DefaultColor, 2.0f);

        readonly object _commonLocker = new object();

        /// <summary>
        ///     Предназначена для хранения задач, связанных с изменениями в файловой системе.
        /// </summary>
        readonly ConcurrentQueue<FileTask> _concurrentFileTasks = new ConcurrentQueue<FileTask>();

        /// <summary>
        ///     Отражает статус работы потока, который служит для получения всех имеющихся на данный момент образов букв для
        ///     поиска их на распознаваемом изображении, в том числе, для актуализации их содержимого.
        /// </summary>
        readonly ManualResetEvent _fileActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Хранит загруженные карты, которые требуется искать на основной карте.
        ///     Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ImageProcessorStorage _imagesProcessorStorage;

        /// <summary>
        ///     Текст кнопки "Найти". Сохраняет исходное значение свойства <see cref="Button.Text" /> кнопки
        ///     <see cref="btnRecognizeImage" />.
        /// </summary>
        readonly Image _imgSearchDefault;


        readonly RecognizeProcessorStorage _recognizeProcessorStorage;

        /// <summary>
        ///     Отражает статус работы потока, отвечающего за поиск символов на распознаваемом изображении.
        /// </summary>
        readonly ManualResetEvent _recognizerActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Коллекция задействованных элементов <see cref="DynamicReflex" />.
        ///     Содержит <see cref="DynamicReflex" />, запрос, статус выполнения запроса, номер просматриваемой карты на данный
        ///     момент.
        /// </summary>
        readonly List<(Processor[] processors, int reflexMapIndex, string systemName)> _recognizeResults =
            new List<(Processor[] processors, int reflexMapIndex, string systemName)>();

        readonly ManualResetEvent _recogPreparing = new ManualResetEvent(false);

        /// <summary>
        ///     Уведомляет о необходимости запустить поток для обновления списка файлов изображений.
        /// </summary>
        readonly AutoResetEvent _refreshEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Сигнал остановки потокам, работающим на фоне.
        /// </summary>
        readonly ManualResetEvent _stopBackground = new ManualResetEvent(false);

        /// <summary>
        ///     Таймер для измерения времени, затраченного на поиск символов на распознаваемой карте.
        /// </summary>
        readonly Stopwatch _stwRecognize = new Stopwatch();

        /// <summary>
        ///     Содержит изначальное значение поля "Название" искомого образа буквы.
        /// </summary>
        readonly string _unknownSymbolName;

        /// <summary>
        ///     Определяет шаг (в пикселях), на который изменяется ширина сканируемого (создаваемого) изображения при нажатии
        ///     кнопок сужения или расширения.
        /// </summary>
        readonly int _widthStep;

        /// <summary>
        ///     Изображение, которое выводится в окне создания распознаваемого изображения.
        /// </summary>
        Bitmap _btmRecognizeImage;

        Bitmap _btmSavedRecognizeCopy;

        /// <summary>
        ///     Отражает статус всех кнопок на данный момент.
        /// </summary>
        bool _buttonsEnabled = true;

        int _currentHistoryPos;

        /// <summary>
        ///     Индекс <see cref="ImageRect" />, рассматриваемый в данный момент.
        /// </summary>
        int _currentImageIndex;

        int _currentRecognizeProcIndex;

        /// <summary>
        ///     Текущее состояние программы.
        /// </summary>
        UndoRedoState _currentUndoRedoState = UndoRedoState.UNKNOWN;

        string _currentUndoRedoWord = string.Empty;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае - <see langword="false" />.
        /// </summary>
        bool _draw;

        Thread _exitingThread;

        /// <summary>
        ///     Поток, отвечающий за актуализацию содержимого карт <see cref="Processor" />.
        /// </summary>
        Thread _fileRefreshThread;

        FileSystemWatcher _fswImageChanged;

        FileSystemWatcher _fswRecognizeChanged;

        FileSystemWatcher _fswWorkDirChanged;

        Graphics _grpImagesGraphics;

        Graphics _grpResultsGraphics;

        Graphics _grpSourceImageGraphics;

        /// <summary>
        ///     Поверхность рисования в окне создания распознаваемого изображения.
        /// </summary>
        Graphics _grRecognizeImageGraphics;

        Pen _imageFrameResetPen;

        bool _isExited;

        bool _needInitImage = true;

        bool _needInitRecognizeImage = true;

        DynamicReflex _recognizer;

        /// <summary>
        ///     Поток, отвечающий за выполнение процедуры поиска символов на распознаваемом изображении.
        /// </summary>
        Thread _recognizerThread;

        string _savedRecognizeQuery = string.Empty;

        bool _txtWordTextChecking;

        /// <summary>
        ///     Поток, отвечающий за отображение процесса ожидания завершения операции.
        /// </summary>
        Thread _userInterfaceThread;

        static FrmExample()
        {
            InvalidCharSet = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (char c in Path.GetInvalidPathChars())
                InvalidCharSet.Add(c);
        }

        /// <summary>
        ///     Конструктор основной формы приложения.
        /// </summary>
        internal FrmExample()
        {
            try
            {
                InitializeComponent();

                LogPath = Path.Combine(WorkingDirectory, "log.log");

                ThreadPool.GetMinThreads(out _, out int comPortMin);
                ThreadPool.SetMinThreads(Environment.ProcessorCount * 3, comPortMin);
                ThreadPool.GetMaxThreads(out _, out int comPortMax);
                ThreadPool.SetMaxThreads(Environment.ProcessorCount * 15, comPortMax);

                _widthStep = pbBrowse.Width;

                _imagesProcessorStorage = new ImageProcessorStorage(ExtImg);
                _recognizeProcessorStorage = new RecognizeProcessorStorage(pbDraw.MinimumSize.Width,
                    pbDraw.MaximumSize.Width, pbDraw.Height, ExtImg);

                _savedRecognizeQuery = txtWord.Text;
                _unknownSymbolName = txtSymbolPath.Text;
                _imgSearchDefault = btnRecognizeImage.Image;
                ImageWidth = pbBrowse.Width;
                ImageHeight = pbBrowse.Height;
                txtWord.TextChanged += TxtWordTextCheck;
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        DynamicReflex Recognizer
        {
            get
            {
                lock (_commonLocker)
                {
                    return _recognizer;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    if (value != null && _recognizer != null)
                        throw new ArgumentException(
                            $@"Нельзя создать новый экземпляр {nameof(DynamicReflex)}, когда предыдущий ещё существует.",
                            nameof(value));

                    _recognizer = value;
                    CurrentUndoRedoState = UndoRedoState.UNKNOWN;
                }
            }
        }

        bool IsExited
        {
            get
            {
                lock (_commonLocker)
                {
                    return _isExited;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _isExited = value;
                }
            }
        }

        bool NeedStopBackground => _stopBackground.WaitOne(0);

        (Processor[] processors, int reflexMapIndex, string systemName) SelectedResult
        {
            get => _recognizeResults[lstHistory.SelectedIndex];
            set => _recognizeResults[lstHistory.SelectedIndex] = value;
        }

        Thread RecognizerThread
        {
            get
            {
                lock (_commonLocker)
                {
                    return _recognizerThread;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _recognizerThread = value;
                }
            }
        }

        public static byte DefaultOpacity => 0xFF;

        internal static HashSet<char> InvalidCharSet { get; }

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

        public static string WorkingDirectory { get; } = Application.StartupPath;

        public string LogPath { get; }

        /// <summary>
        ///     Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor" />, поиск которых
        ///     будет осуществляться на основной карте.
        /// </summary>
        internal static string SearchImagesPath { get; } = Path.Combine(WorkingDirectory, ImagesFolder);

        internal static string RecognizeImagesPath { get; } = Path.Combine(WorkingDirectory, RecognizeFolder);

        /// <summary>
        ///     Расширение изображений, которые интерпретируются как карты <see cref="Processor" />.
        /// </summary>
        internal static string ExtImg => "bmp";

        /// <summary>
        ///     Отключает или включает доступность кнопок на время выполнения операции.
        /// </summary>
        bool ButtonsEnabled
        {
            get
            {
                lock (_commonLocker)
                {
                    return _buttonsEnabled;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    if (value == _buttonsEnabled)
                        return;

                    SafeExecute(() =>
                    {
                        pbDraw.Enabled = value;
                        btnImageCreate.Enabled = value;
                        btnImageDelete.Enabled = value;
                        btnImageUpToQueries.Enabled = value;
                        btnImagePrev.Enabled = value;
                        btnImageNext.Enabled = value;
                        txtImagesNumber.Enabled = value;
                        txtImagesCount.Enabled = value;
                        txtWord.ReadOnly = !value;
                        btnLoadRecognizeImage.Enabled = value;
                        btnClearImage.Enabled = value && IsPainting;
                        btnRecogPrev.Enabled = value;
                        btnRecogNext.Enabled = value;
                        txtRecogNumber.Enabled = value;
                        txtRecogCount.Enabled = value;
                        btnDeleteRecognizeImage.Enabled = value;
                        lblSourceCount.Enabled = value;
                        lblImagesCount.Enabled = value;

                        if (value)
                        {
                            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
                            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
                            return;
                        }

                        btnWide.Enabled = false;
                        btnNarrow.Enabled = false;
                    }, true);

                    _buttonsEnabled = value;
                }
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
                for (int y = 0; y < _btmRecognizeImage.Height; y++)
                for (int x = 0; x < _btmRecognizeImage.Width; x++)
                    if (_btmRecognizeImage.GetPixel(x, y).ToArgb() != DefaultColor.ToArgb())
                        return true;

                return false;
            }
        }

        /// <summary>
        ///     Искомое слово, написанное пользователем в момент запуска процедуры поиска символов на распознаваемом изображении.
        /// </summary>
        internal string CurrentUndoRedoWord
        {
            get => _currentUndoRedoWord;

            set
            {
                if (CurrentUndoRedoState == UndoRedoState.UNKNOWN)
                    return;

                pbSuccess.Image = value == _currentUndoRedoWord
                    ? CurrentUndoRedoState == UndoRedoState.ERROR
                        ? Resources.Result_Error
                        : Resources.Result_OK
                    : Resources.Result_Unknown;
            }
        }

        /// <summary>
        ///     Устанавливает текущее состояние программы.
        ///     Оно может быть установлено только, если текущее состояние равно <see cref="UndoRedoState.UNKNOWN" />.
        ///     В других случаях новое состояние будет игнорироваться.
        ///     Установить можно либо <see cref="UndoRedoState.ERROR" />, либо <see cref="UndoRedoState.SUCCESS" />.
        /// </summary>
        internal UndoRedoState CurrentUndoRedoState
        {
            get => _currentUndoRedoState;

            set
            {
                switch (value)
                {
                    case UndoRedoState.ERROR:
                        pbSuccess.Image = Resources.Result_Error;
                        break;
                    case UndoRedoState.SUCCESS:
                        pbSuccess.Image = Resources.Result_OK;
                        break;
                    case UndoRedoState.UNKNOWN:
                        pbSuccess.Image = Resources.Result_Unknown;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            $@"{nameof(UndoRedoState)} {nameof(CurrentUndoRedoState)}");
                }

                _currentUndoRedoState = value;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWordTextCheck(object sender, EventArgs e)
        {
            if (_txtWordTextChecking)
                return;

            if (txtWord.Text.Length <= txtWord.MaxLength)
                return;

            _txtWordTextChecking = true;
            _savedRecognizeQuery = txtWord.Text = txtWord.Text.Remove(txtWord.MaxLength);
            _txtWordTextChecking = false;
        }

        internal static Color CheckAlphaColor(Color c)
        {
            return c.A == DefaultOpacity
                ? c
                : throw new InvalidOperationException(
                    $@"Значение прозрачности не может быть задано 0x{c.A:X2}. Должно быть задано как 0x{DefaultOpacity:X2}.");
        }

        internal static bool CompareBitmaps(Bitmap bitmap1, Bitmap bitmap2)
        {
            if (bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
                return false;

            if (ReferenceEquals(bitmap1, bitmap2))
                throw new InvalidOperationException("Ссылки на проверяемые изображения совпадают.");

            for (int x = 0; x < bitmap1.Width; x++)
            for (int y = 0; y < bitmap1.Height; y++)
                if (bitmap1.GetPixel(x, y).ToArgb() != bitmap2.GetPixel(x, y).ToArgb())
                    return false;

            return true;
        }

        void RefreshRecognizer()
        {
            Recognizer = null;
        }

        /// <summary>
        ///     Останавливает процесс поиска символов на распознаваемом изображении.
        ///     Возвращает значение <see langword="true" /> в случае успешной остановки процесса распознавания, в противном случае
        ///     возвращает значение <see langword="false" />, в том числе, если процесс распознавания не был запущен.
        /// </summary>
        /// <returns>
        ///     Возвращает значение <see langword="true" /> в случае успешной остановки процесса распознавания, в противном случае
        ///     возвращает значение <see langword="false" />, в том числе, если процесс распознавания не был запущен.
        /// </returns>
        bool StopRecognize(bool userNotify = true)
        {
            try
            {
                Thread t = RecognizerThread;

                if (t == null)
                    return false;

                t.Abort();

                t.Join();

                RecognizerThread = null;

                return true;
            }
            catch (Exception ex)
            {
                if (!userNotify)
                    return false;

                WriteLogMessage($@"{nameof(StopRecognize)}: {SearchStopError}{Environment.NewLine}{ex.Message}");
                MessageBox.Show(this, SearchStopError, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
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
            catch (AggregateException ex)
            {
                throw new AggregateException($@"{name}: {ex.Message}", ex.InnerExceptions);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Создаёт новый поток для обновления списка файлов изображений, в случае, если поток (
        ///     <see cref="_fileRefreshThread" />) не
        ///     выполняется.
        ///     Созданный поток находится в состояниях <see cref="System.Threading.ThreadState.Unstarted" /> и
        ///     <see cref="System.Threading.ThreadState.Background" />.
        ///     Поток служит для получения всех имеющихся на данный момент образов букв для поиска, в том числе, для
        ///     актуализации их содержимого.
        ///     Возвращает экземпляр созданного потока или <see langword="null" />, в случае, этот поток выполняется.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null" />, в случае, этот поток выполняется.</returns>
        void CreateFileRefreshThread()
        {
            if (_fileRefreshThread != null)
                throw new InvalidOperationException(
                    $@"Попытка вызвать метод {nameof(CreateFileRefreshThread)} в то время, когда поток {nameof(_fileRefreshThread)} уже существует.");

            Thread thread = new Thread(() => SafeExecute(() =>
            {
                try
                {
                    WaitHandle[] waitHandles = { _stopBackground, _refreshEvent };

                    while (WaitHandle.WaitAny(waitHandles) != 0)
                    {
                        _fileActivity.Set();

                        try
                        {
                            while (!NeedStopBackground && _concurrentFileTasks.TryDequeue(out FileTask task))
                                try
                                {
                                    switch (task.Type)
                                    {
                                        case FileTaskAction.REMOVED:
                                        {
                                            Common c = (Common)task;
                                            ExceptionClause(() => c.Storage.RemoveProcessor(c.Path),
                                                $"{nameof(ConcurrentProcessorStorage.RemoveProcessor)} -> {task.Type} -> {c.Path}");
                                            break;
                                        }
                                        case FileTaskAction.CLEARED:
                                        {
                                            FileTask t = task;
                                            ExceptionClause(() => t.Storage.Clear(),
                                                $"{nameof(ConcurrentProcessorStorage.Clear)} -> {task.Type}");
                                            break;
                                        }
                                        case FileTaskAction.RENAMED:
                                        {
                                            Renamed r = (Renamed)task;
                                            if (r.RenamedFrom)
                                                ExceptionClause(() => r.Storage.RemoveProcessor(r.OldPath),
                                                    $"{nameof(ConcurrentProcessorStorage.RemoveProcessor)} -> {task.Type} -> {r.OldPath}");
                                            if (r.RenamedTo)
                                                ExceptionClause(() => r.Storage.AddProcessor(r.Path),
                                                    $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> {task.Type} -> {r.Path}");
                                            break;
                                        }
                                        case FileTaskAction.CREATED:
                                        case FileTaskAction.CHANGED:
                                        {
                                            Common c = (Common)task;
                                            ExceptionClause(() => c.Storage.AddProcessor(c.Path),
                                                $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> {task.Type} -> {c.Path}");
                                            break;
                                        }
                                        default:
                                            throw new ArgumentOutOfRangeException(nameof(task), task.Type,
                                                UnknownFSChangeType);
                                    }
                                }
                                catch (AggregateException ex)
                                {
                                    StringBuilder sb =
                                        new StringBuilder(
                                            $@"{ex.Message}{Environment.NewLine}Список возникших исключений:");

                                    int index = 1;

                                    foreach (Exception e in ex.Flatten().InnerExceptions)
                                    {
                                        sb.Append(Environment.NewLine);
                                        sb.Append($@"{index++}) {e.Message}");
                                    }

                                    WriteLogMessage($@"{nameof(CreateFileRefreshThread)}: {sb}");
                                }
                                catch (Exception ex)
                                {
                                    WriteLogMessage($@"{nameof(CreateFileRefreshThread)}: {ex.Message}");
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

            _fileRefreshThread = thread;

            thread.Start();
        }

        /// <summary>
        ///     Записывает сообщение в лог-файл, в синхронном режиме.
        ///     Доступ к этому методу синхронизируется.
        ///     К сообщению автоматически прибавляется текущая дата в полном формате.
        /// </summary>
        /// <param name="logstr">Строка лога, которую надо записать.</param>
        void WriteLogMessage(string logstr)
        {
            void ShowOnceUserMessage(string addMes = null)
            {
                string m = string.Empty;

                try
                {
                    lock (_commonLocker)
                    {
                        if (_errorMessageIsShowed)
                            return;

                        _errorMessageIsShowed = true;
                    }

                    m = string.IsNullOrWhiteSpace(addMes) ? LogRefreshedMessage : addMes;

                    void Act()
                    {
                        try
                        {
                            MessageBox.Show(this, m, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                        catch
                        {
                            MessageBox.Show(m, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    new Thread(() =>
                    {
                        try
                        {
                            Invoke((Action)Act);
                        }
                        catch
                        {
                            MessageBox.Show(m, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    })
                    {
                        Name = @"Message"
                    }.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.IsNullOrEmpty(m) ? ex.Message : m, @"Ошибка", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            try
            {
                logstr = $@"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {logstr}";
                lock (LogLockerObject)
                {
                    using (FileStream fs = new FileStream(LogPath, FileMode.Append, FileAccess.Write,
                               FileShare.ReadWrite | FileShare.Delete))
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.WriteLine(logstr);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowOnceUserMessage(
                    $@"Ошибка при записи логов: {ex.Message}{Environment.NewLine}Сообщение лога: {logstr}");
                return;
            }

            ShowOnceUserMessage();
        }

        /// <summary>
        ///     Отображает сообщение с указанным текстом в другом потоке.
        /// </summary>
        /// <param name="message">Текст отображаемого сообщения.</param>
        void ErrorMessageInOtherThread(string message)
        {
            Thread t = new Thread(() =>
                SafeExecute(
                    () => MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation),
                    true))
            {
                IsBackground = true,
                Name = @"Message"
            };

            t.Start();
        }

        /// <summary>
        ///     Представляет обёртку для выполнения функций с применением блоков <see langword="try" />-<see langword="catch" />,
        ///     а также выдачей сообщений обо всех
        ///     ошибках.
        /// </summary>
        /// <param name="funcAction">Функция, которая должна быть выполнена.</param>
        /// <param name="needInvoke">Значение <see langword="true" /> в случае необходимости выполнить функцию в основном потоке.</param>
        void SafeExecute(Action funcAction, bool needInvoke = false)
        {
            if (funcAction == null)
            {
                string s = $@"{nameof(SafeExecute)}(1): Выполняемая функция отсутствует.";

                WriteLogMessage(s);

                throw new ArgumentNullException(nameof(funcAction), s);
            }

            try
            {
                void Act()
                {
                    try
                    {
                        funcAction.Invoke();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            ResetAbort();

                            WriteLogMessage($@"{nameof(SafeExecute)}(2): {ex.Message}.");
                        }
                        catch
                        {
                            throw ex;
                        }
                    }
                }

                if (needInvoke && InvokeRequired)
                    Invoke((Action)Act);
                else
                    Act();
            }
            catch (Exception ex)
            {
                ResetAbort();

                WriteLogMessage($@"{nameof(SafeExecute)}(3): {ex.Message}.");
            }
        }

        static void ResetAbort()
        {
            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
                Thread.ResetAbort();
        }

        /// <summary>
        ///     Состояние программы после поиска символов на изображении.
        /// </summary>
        internal enum UndoRedoState
        {
            /// <summary>
            ///     Неизвестно.
            ///     Этот статус означает, что процесс поиска ещё не был запущен.
            /// </summary>
            UNKNOWN,

            /// <summary>
            ///     Ошибка, условия не изменялись.
            /// </summary>
            ERROR,

            /// <summary>
            ///     Успех, условия не изменялись.
            /// </summary>
            SUCCESS
        }
    }
}