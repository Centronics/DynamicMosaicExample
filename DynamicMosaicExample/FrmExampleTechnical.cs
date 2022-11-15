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

namespace DynamicMosaicExample
{
    internal sealed partial class FrmExample
    {
        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrRecognize = "Ждите   ";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrRecognize1 = "Ждите.  ";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrRecognize2 = "Ждите.. ";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrRecognize3 = "Ждите...";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading = "Загрузка   ";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading1 = "Загрузка.  ";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading2 = "Загрузка.. ";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrLoading3 = "Загрузка...";

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

        FileSystemWatcher _fswRecognizeChanged;

        FileSystemWatcher _fswImageChanged;

        FileSystemWatcher _fswWorkDirChanged;

        /// <summary>
        ///     Указывает, было ли просмотрено сообщение о том, что в процессе работы программы уже произошла ошибка.
        /// </summary>
        static volatile bool _errorMessageIsShowed;

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        public static Pen BlackPen = new Pen(CheckAlphaColor(Color.Black), 2.0f);

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
        public static Color DefaultColor = CheckAlphaColor(Color.White);

        /// <summary>
        ///     Поток, отвечающий за актуализацию содержимого карт <see cref="Processor" />.
        /// </summary>
        Thread _fileRefreshThread;

        /// <summary>
        ///     Отражает статус работы потока, который служит для получения всех имеющихся на данный момент образов букв для
        ///     поиска их на распознаваемом изображении, в том числе, для актуализации их содержимого.
        /// </summary>
        readonly ManualResetEvent _fileActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Уведомляет о необходимости запустить поток для обновления списка файлов изображений.
        /// </summary>
        readonly AutoResetEvent _needRefreshEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Хранит загруженные карты, которые требуется искать на основной карте.
        ///     Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ImageProcessorStorage _imagesProcessorStorage = new ImageProcessorStorage();


        readonly RecognizeProcessorStorage _recognizeProcessorStorage;

        /// <summary>
        ///     Хранит значение свойства <see cref="GroupBox.Text" /> объекта <see cref="grpResults" />.
        /// </summary>
        readonly string _strGrpResults;

        /// <summary>
        ///     Текст кнопки "Найти". Сохраняет исходное значение свойства <see cref="Button.Text" /> кнопки
        ///     <see cref="btnRecognizeImage" />.
        /// </summary>
        readonly string _strRecog;

        /// <summary>
        ///     Таймер для измерения времени, затраченного на поиск символов на распознаваемой карте.
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
        public static Pen WhitePen = new Pen(DefaultColor, 2.0f);

        /// <summary>
        ///     Коллекция задействованных элементов <see cref="DynamicReflex" />.
        ///     Содержит <see cref="DynamicReflex" />, запрос, статус выполнения запроса, номер просматриваемой карты на данный момент.
        /// </summary>
        readonly List<(Processor[] processors, int reflexMapIndex, string systemName)> _recognizeResults = new List<(Processor[] processors, int reflexMapIndex, string systemName)>();

        /// <summary>
        ///     Отражает статус работы потока, отвечающего за поиск символов на распознаваемом изображении.
        /// </summary>
        readonly ManualResetEvent _recognizerActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Поток, отвечающий за отображение процесса ожидания завершения операции.
        /// </summary>
        Thread _userInterfaceThread;

        /// <summary>
        ///     Изображение, которое выводится в окне создания распознаваемого изображения.
        /// </summary>
        Bitmap _btmRecognizeImage;

        Bitmap _btmSavedRecognizeCopy;

        string _savedRecognizeQuery = string.Empty;

        /// <summary>
        ///     Отражает статус всех кнопок на данный момент.
        /// </summary>
        bool _buttonsEnabled = true;

        /// <summary>
        ///     Индекс <see cref="ImageRect" />, рассматриваемый в данный момент.
        /// </summary>
        int _currentImageIndex;

        int _currentRecognizeProcIndex;

        bool _needInitRecognizeImage;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае - <see langword="false" />.
        /// </summary>
        bool _draw;

        readonly string _grpWordsDefaultValue;

        /// <summary>
        ///     Поверхность рисования в окне создания распознаваемого изображения.
        /// </summary>
        Graphics _grRecognizeImage;

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
        ///     Поток, отвечающий за выполнение процедуры поиска символов на распознаваемом изображении.
        /// </summary>
        Thread _recognizerThread;

        bool _txtWordTextChecking;

        /// <summary>
        /// 
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

        public static byte DefaultOpacity => 0xFF;

        internal static Color CheckAlphaColor(Color c) => c.A == DefaultOpacity ? c : throw new InvalidOperationException($@"Значение прозрачности не может быть задано 0x{c.A:X2}. Должно быть задано как 0x{DefaultOpacity:X2}.");

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
                txtWord.TextChanged += TxtWordTextCheck;
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

        public static string WorkingDirectory { get; } = Application.StartupPath;

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
        ///     Отражает статус процесса актуализации содержимого карт с жёсткого диска.
        /// </summary>
        bool IsFileActivity => _fileActivity.WaitOne(0);

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

        internal static bool CompareBitmaps(Bitmap bitmap1, Bitmap bitmap2)
        {
            if (bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
                return false;

            if (ReferenceEquals(bitmap1, bitmap2))
                throw new InvalidOperationException("Ссылки на проверяемые изображения совпадают.");

            for (int x = 0; x < bitmap1.Width; x++)
                for (int y = 0; y < bitmap1.Height; y++)
                    if (bitmap1.GetPixel(x, y) != bitmap2.GetPixel(x, y))
                        return false;

            return true;
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
                        if (_btmRecognizeImage.GetPixel(x, y) != DefaultColor)
                            return true;

                return false;
            }
        }

        /// <summary>
        ///     Получает список файлов изображений карт в указанной папке.
        ///     Это файлы с расширением <see cref="ExtImg" />.
        ///     В случае какой-либо ошибки возвращает пустой массив.
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
        ///     Останавливает процесс поиска символов на распознаваемом изображении.
        ///     Возвращает значение <see langword="true" /> в случае успешной остановки процесса распознавания, в противном случае
        ///     возвращает значение <see langword="false" />, в том числе, если процесс распознавания не был запущен.
        /// </summary>
        /// <returns>
        ///     Возвращает значение <see langword="true" /> в случае успешной остановки процесса распознавания, в противном случае
        ///     возвращает значение <see langword="false" />, в том числе, если процесс распознавания не был запущен.
        /// </returns>
        bool StopRecognize()
        {
            if (!IsRecognizing)
                return false;

            try
            {
                _recognizerThread.Abort();

                if (!_recognizerThread.Join(15000))
                    MessageBox.Show(this,
                        @"Во время остановки процесса поиска произошла ошибка: поток, отвечающий за поиск, завис. Рекомендуется перезапустить программу.",
                        @"Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $@"Во время остановки процесса поиска произошла ошибка:{Environment.NewLine}{ex.Message}", @"Ошибка",
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
        ///     Создаёт новый поток для обновления списка файлов изображений, в случае, если поток (<see cref="_fileRefreshThread" />) не
        ///     выполняется.
        ///     Созданный поток находится в состояниях <see cref="System.Threading.ThreadState.Unstarted" /> и <see cref="System.Threading.ThreadState.Background" />.
        ///     Поток служит для получения всех имеющихся на данный момент образов букв для поиска, в том числе, для
        ///     актуализации их содержимого.
        ///     Возвращает экземпляр созданного потока или <see langword="null" />, в случае, этот поток выполняется.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null" />, в случае, этот поток выполняется.</returns>
        void CreateFileRefreshThread()
        {
            if (_fileRefreshThread != null)
                throw new InvalidOperationException($@"Попытка вызвать метод {nameof(CreateFileRefreshThread)} в то время, когда поток {nameof(_fileRefreshThread)} уже существует.");

            Thread thread = new Thread(() => SafetyExecute(() =>
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

                        void Execute(ConcurrentProcessorStorage storage, string searchPath) => SafetyExecute(() =>
                            Parallel.ForEach(
                                Directory.EnumerateFiles(searchPath, $"*.{ExtImg}", SearchOption.AllDirectories),
                                (fullPath, state) =>
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
                                try
                                {
                                    switch (task.Type)
                                    {
                                        case FileTaskAction.REMOVED:
                                        {
                                            Common c = (Common)task;
                                            ExceptionClause(
                                                () => c.Storage.RemoveProcessor(c.Path),
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
                                                ExceptionClause(
                                                    () => r.Storage.RemoveProcessor(r.OldPath),
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
                                                @"Неизвестный тип изменения файловой системы.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    WriteLogMessage(ex.Message);
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

            thread.Start();

            _fileRefreshThread = thread;
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
                string path = Path.Combine(WorkingDirectory, "DynamicMosaicExampleLog.log");
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
        ///     Состояние программы после поиска символов на изображении.
        /// </summary>
        enum RecognizeState
        {
            /// <summary>
            ///     Неизвестно.
            ///     Этот статус означает, что процесс поиска ещё не был запущен.
            /// </summary>
            UNKNOWN,

            /// <summary>
            ///     Ошибка, слово изменено.
            ///     Вернуться из этого статуса в статус <see cref="ERROR" /> можно путём возвращения слова в предыдущее состояние,
            ///     какое оно было при выполнении процедуры поиска.
            ///     Регистр не учитывается.
            /// </summary>
            ERRORWORD,

            /// <summary>
            ///     Успех, слово изменено.
            ///     Вернуться из этого статуса в статус <see cref="SUCCESS" /> можно путём возвращения слова в предыдущее состояние,
            ///     какое оно было при выполнении процедуры поиска.
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
            ///     Искомое слово, написанное пользователем в момент запуска процедуры поиска символов на распознаваемом изображении.
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
                            _curForm.pbSuccess.Image = Resources.Result_Error;
                            _state = RecognizeState.ERROR;
                            return;
                        case RecognizeState.SUCCESS:
                            _curForm.pbSuccess.Image = Resources.Result_OK;
                            _state = RecognizeState.SUCCESS;
                            return;
                    }
                }
            }

            /// <summary>
            ///     Используется для подписи на событие изменения критических данных, относящихся к поиску символов на распознаваемом изображении.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            internal void CriticalChange(object sender, EventArgs e)
            {
                _curForm.InvokeAction(() => _curForm.pbSuccess.Image = Resources.Result_Unknown);
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
                        _curForm.pbSuccess.Image = Resources.Result_Unknown;
                        _state = _state == RecognizeState.ERROR ? RecognizeState.ERRORWORD : RecognizeState.SUCCESSWORD;
                        return;
                    case RecognizeState.ERRORWORD:
                    case RecognizeState.SUCCESSWORD:
                        if (string.Compare(CurWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) != 0)
                            return;
                        _curForm.pbSuccess.Image = _state == RecognizeState.ERRORWORD ? Resources.Result_Error : Resources.Result_OK;
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
    }
}