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
        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrPreparing0 = @"Подготовка(/)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrPreparing1 = @"Подготовка(~)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrPreparing2 = @"Подготовка(|)";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
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

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrStopping0 = @"Остановка";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrStopping1 = @"Остановка.";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrStopping2 = @"Остановка..";

        /// <summary>
        ///     Текст кнопки "Найти".
        /// </summary>
        const string StrStopping3 = @"Остановка...";

        /// <summary>
        /// Строка сообщения.
        /// </summary>
        /// <remarks>
        /// Призвана сократить длину строк в коде.
        /// </remarks>
        const string NeedSaveQuery = @"Перед запуском процедуры поиска необходимо сохранить текущий запрос.";

        /// <summary>
        /// Строка сообщения.
        /// </summary>
        /// <remarks>
        /// Призвана сократить длину строк в коде.
        /// </remarks>
        const string SaveImageQueryError = @"Необходимо написать какой-либо запрос, который будет использоваться в качестве имени файла изображения.";

        /// <summary>
        /// Строка сообщения.
        /// </summary>
        /// <remarks>
        /// Призвана сократить длину строк в коде.
        /// </remarks>
        const string QueryErrorSymbols = @"Запрос содержит недопустимые символы.";

        /// <summary>
        /// Строка сообщения.
        /// </summary>
        /// <remarks>
        /// Призвана сократить длину строк в коде.
        /// </remarks>
        const string LogRefreshedMessage = @"Содержимое лог-файла обновлено. Есть новые сообщения.";

        /// <summary>
        /// Строка сообщения.
        /// </summary>
        /// <remarks>
        /// Призвана сократить длину строк в коде.
        /// </remarks>
        const string SearchStopError = @"Во время остановки процесса поиска произошла ошибка.";

        /// <summary>
        /// Строка сообщения.
        /// </summary>
        /// <remarks>
        /// Призвана сократить длину строк в коде.
        /// </remarks>
        const string UnknownFSChangeType = @"Неизвестный тип изменения файловой системы.";

        /// <summary>
        ///     Синхронизирует потоки во время записи сообщения в лог-файл.
        /// </summary>
        static readonly object LogLockerObject = new object();

        /// <summary>
        ///     Указывает, было ли просмотрено сообщение о том, что в процессе работы программы уже произошла ошибка.
        /// </summary>
        /// <remarks>
        /// Для сброса значения служит метод <see cref="ResetLogWriteMessage()"/>.
        /// </remarks>
        static volatile bool _errorMessageIsShowed;

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        public static readonly Pen BlackPen = new Pen(CheckAlphaColor(Color.Black), 2.0f);

        /// <summary>
        /// Служит для рисования рамки вокруг элемента управления.
        /// </summary>
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

        /// <summary>
        /// Обеспечивает потокобезопасность членов этого класса.
        /// </summary>
        readonly object _commonLocker = new object();

        /// <summary>
        ///     Очередь, которая предназначена для хранения задач <see cref="FileTask"/>, связанных с обновлением содержимого коллекций карт.
        /// </summary>
        /// <seealso cref="FileTask"/>
        readonly ConcurrentQueue<FileTask> _concurrentFileTasks = new ConcurrentQueue<FileTask>();

        /// <summary>
        ///     Отражает статус работы потока, который служит для обновления содержимого всех имеющихся коллекций карт.
        ///     У этого сигнала самый низкий приоритет (0).
        /// </summary>
        readonly ManualResetEvent _fileActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Хранит карты, которые требуется искать, в процессе выполнения поисковых запросов, на картах из <see cref="RecognizeProcessorStorage"/>.
        /// </summary>
        /// <seealso cref="RecognizeProcessorStorage"/>
        readonly ImageProcessorStorage _imagesProcessorStorage;

        /// <summary>
        /// Хранит карты, на которых требуется выполнить поисковые запросы, посредством карт из <see cref="ImageProcessorStorage"/>.
        /// </summary>
        /// <seealso cref="ImageProcessorStorage"/>
        readonly RecognizeProcessorStorage _recognizeProcessorStorage;

        /// <summary>
        ///     Сохраняет исходное значение свойства <see cref="ButtonBase.Image" /> кнопки <see cref="btnRecognizeImage" />.
        /// </summary>
        /// <seealso cref="ButtonBase.Image"/>
        /// <seealso cref="btnRecognizeImage"/>
        readonly Image _imgSearchDefault;

        /// <summary>
        ///     Отражает статус работы потока, отвечающего за выполнение поискового запроса на распознаваемом изображении.
        /// </summary>
        /// <remarks>
        /// У этого сигнала самый высокий приоритет (2) среди всех сигналов, кроме <see cref="_stopBackground"/>.
        /// </remarks>
        /// <seealso cref="_stopBackground"/>
        readonly ManualResetEvent _recognizerActivity = new ManualResetEvent(false);

        /// <summary>
        ///     Служит для хранения исторических сведений тестируемого класса <see cref="DynamicReflex"/>.
        /// </summary>
        /// <remarks>
        /// Содержит копию коллекции карт текущего экземпляра, номер карты, на которой пользователь остановил просмотр истории, и комментарий к историческому событию, как указано при вызове функции <see cref="OutHistory(bool?, Processor[], string)"/>.
        /// </remarks>
        /// <seealso cref="DynamicReflex"/>
        /// <seealso cref="OutHistory(bool?, Processor[], string)"/>
        readonly List<(Processor[] processors, int reflexMapIndex, string comment)> _recognizeResults =
            new List<(Processor[] processors, int reflexMapIndex, string comment)>();

        /// <summary>
        /// Сигнал о том, что начат процесс подготовки к запуску процесса выполнения поискового запроса.
        /// </summary>
        /// <remarks>
        /// У этого сигнала приоритет чуть выше, чем у <see cref="_fileActivity"/> (1).
        /// </remarks>
        readonly ManualResetEvent _recogPreparing = new ManualResetEvent(false);

        /// <summary>
        ///     Уведомляет о необходимости запустить поток, отвечающий за обновление содержимого коллекций карт.
        /// </summary>
        readonly AutoResetEvent _refreshEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Сигнал необходимости завершения всех фоновых процессов.
        /// </summary>
        /// <remarks>
        /// У этого сигнала самый высокий приоритет среди всех остальных сигналов (3).
        /// </remarks>
        readonly ManualResetEvent _stopBackground = new ManualResetEvent(false);

        /// <summary>
        ///     Таймер для измерения времени, затраченного на выполнение поискового запроса на распознаваемой карте.
        /// </summary>
        readonly Stopwatch _stwRecognize = new Stopwatch();

        /// <summary>
        ///     Содержит изначальное значение поля пути к искомому образу.
        /// </summary>
        readonly string _unknownSymbolName;

        /// <summary>
        ///     Определяет шаг (в пикселях), на который изменяется ширина распознаваемого изображения при нажатии
        ///     кнопок сужения или расширения.
        /// </summary>
        readonly int _widthStep;

        /// <summary>
        ///     Изображение, которое выводится в окне создания распознаваемого изображения.
        /// </summary>
        Bitmap _btmRecognizeImage;

        /// <summary>
        /// Содержит копию распознаваемого изображения.
        /// Необходимо для обнаружения наличия изменений распознаваемого изображения на экране.
        /// </summary>
        Bitmap _btmSavedRecognizeCopy;

        /// <summary>
        ///     Отражает состояние активации или деактивации пользовательского интерфейса на время выполнения длительной операции.
        /// </summary>
        /// <remarks>
        /// Хранит значение свойства <see cref="IsButtonsEnabled"/>.
        /// </remarks>
        /// <seealso cref="IsButtonsEnabled"/>
        bool _isButtonsEnabled = true;

        /// <summary>
        /// Текущая позиция курсора в списке исторических событий экземпляра тестируемого класса.
        /// </summary>
        int _currentHistoryPos;

        /// <summary>
        ///     Индекс искомой карты, выбранной на данный момент.
        /// </summary>
        int _currentImageIndex;

        /// <summary>
        /// Индекс распознаваемой карты, выбранной на данный момент.
        /// </summary>
        int _currentRecognizeProcIndex;

        /// <summary>
        ///     Содержит результат завершения последнего выполненного поискового запроса.
        /// </summary>
        /// <remarks>
        /// Хранит значение свойства <see cref="CurrentUndoRedoState"/>.
        /// </remarks>
        /// <seealso cref="CurrentUndoRedoState"/>
        UndoRedoState _currentUndoRedoState = UndoRedoState.UNKNOWN;

        /// <summary>
        /// Поисковый запрос, написанный пользователем в момент запуска процедуры поиска символов на распознаваемом изображении.
        /// </summary>
        string _currentUndoRedoWord = string.Empty;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае - <see langword="false" />.
        /// </summary>
        bool _drawAllowed;

        /// <summary>
        /// Закрывает все процессы в программе, освобождает все используемые ресурсы, и закрывает форму <see cref="FrmExample"/>.
        /// </summary>
        Thread _exitingThread;

        /// <summary>
        ///     Поток, отвечающий за обновление содержимого коллекций карт.
        /// </summary>
        Thread _fileRefreshThread;

        /// <summary>
        /// Осуществляет наблюдение за хранилищем <see cref="SourceChanged.IMAGES"/>.
        /// </summary>
        FileSystemWatcher _fswImageChanged;

        /// <summary>
        /// Осуществляет наблюдение за хранилищем <see cref="SourceChanged.RECOGNIZE"/>.
        /// </summary>
        FileSystemWatcher _fswRecognizeChanged;

        /// <summary>
        /// Осуществляет наблюдение за хранилищем <see cref="SourceChanged.WORKDIR"/>.
        /// </summary>
        FileSystemWatcher _fswWorkDirChanged;

        /// <summary>
        /// Поверхность для рисования на <see cref="grpImages"/>.
        /// </summary>
        Graphics _grpImagesGraphics;

        /// <summary>
        /// Поверхность для рисования на <see cref="grpResults"/>.
        /// </summary>
        Graphics _grpResultsGraphics;

        /// <summary>
        /// Поверхность для рисования на <see cref="grpSourceImage"/>.
        /// </summary>
        Graphics _grpSourceImageGraphics;

        /// <summary>
        ///     Поверхность рисования в окне создания распознаваемого изображения.
        /// </summary>
        Graphics _grRecognizeImageGraphics;

        /// <summary>
        /// Служит для стирания рамки вокруг элемента управления.
        /// </summary>
        Pen _imageFrameResetPen;

        /// <summary>
        /// Хранит значение свойства <see cref="IsExited"/>.
        /// </summary>
        /// <seealso cref="IsExited"/>
        bool _isExited;

        /// <summary>
        /// Значение <see langword="false"/> - указывает на необходимость обновления сведений о выбранной искомой карте на экране.
        /// Значение <see langword="true"/> - указывает на необходимость загрузки выбранной карты из коллекции <see cref="_imagesProcessorStorage"/>, по индексу <see cref="_currentImageIndex"/>, и обновлением (на экране) сведений о ней.
        /// </summary>
        bool _needInitImage = true;

        /// <summary>
        /// Значение <see langword="false"/> - указывает на необходимость обновления сведений о выбранной распознаваемой карте на экране.
        /// Значение <see langword="true"/> - указывает на необходимость загрузки выбранной карты из коллекции <see cref="_recognizeProcessorStorage"/>, по индексу <see cref="_currentRecognizeProcIndex"/>, и обновлением (на экране) сведений о ней.
        /// </summary>
        bool _needInitRecognizeImage = true;

        /// <summary>
        /// Хранит значение свойства <see cref="Recognizer"/>.
        /// </summary>
        /// <seealso cref="Recognizer"/>
        DynamicReflex _recognizer;

        /// <summary>
        ///     Поток, отвечающий за выполнение текущего поискового запроса.
        /// </summary>
        /// <remarks>
        /// Хранит значение свойства <see cref="RecognizerThread"/>.
        /// </remarks>
        /// <seealso cref="RecognizerThread"/>
        Thread _recognizerThread;

        /// <summary>
        /// Поток, который останавливает процесс выполнения поискового запроса.
        /// </summary>
        /// <remarks>
        /// Хранит значение свойства <see cref="StopperThread"/>.
        /// </remarks>
        /// <seealso cref="StopperThread"/>
        Thread _stoppingThread;

        /// <summary>
        /// Последний сохранённый поисковый запрос.
        /// </summary>
        string _savedRecognizeQuery = string.Empty;

        /// <summary>
        /// Служит для защиты от бесконечного цикла вызовов обработчика события при изменении текста в поле ввода поискового запроса.
        /// Поле предназначено только для метода <see cref="TxtRecogQueryWordTextCheck(object, EventArgs)"/>.
        /// </summary>
        /// <seealso cref="TxtRecogQueryWordTextCheck(object, EventArgs)"/>
        bool _txtRecogQueryWordTextChecking;

        /// <summary>
        ///     Поток, отвечающий за инициализацию и актуализацию состояния пользовательского интерфейса в реальном времени.
        /// </summary>
        Thread _userInterfaceActualizeThread;

        /// <summary>
        /// Необходим для инициализации коллекции недопустимых символов пути к файлу или папке.
        /// </summary>
        static FrmExample()
        {
            InvalidCharSet = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (char c in Path.GetInvalidPathChars())
                InvalidCharSet.Add(c);
        }

        /// <summary>
        ///     Конструктор основной формы приложения.
        /// </summary>
        /// <remarks>
        /// Осуществляет инициализацию критически важных для работы программы полей и свойств.
        /// В случае какой-либо ошибки, этот конструктор принудительно завершит работу программы.
        /// </remarks>
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

                _widthStep = pbImage.Width;

                _imagesProcessorStorage = new ImageProcessorStorage(ExtImg);
                _recognizeProcessorStorage = new RecognizeProcessorStorage(pbRecognizeImageDraw.MinimumSize.Width,
                    pbRecognizeImageDraw.MaximumSize.Width, pbRecognizeImageDraw.Height, ExtImg);

                _savedRecognizeQuery = txtRecogQueryWord.Text;
                _unknownSymbolName = txtSymbolPath.Text;
                _imgSearchDefault = btnRecognizeImage.Image;
                ImageWidth = pbImage.Width;
                ImageHeight = pbImage.Height;
                txtRecogQueryWord.TextChanged += TxtRecogQueryWordTextCheck;
            }
            catch (Exception ex)
            {
                WriteLogMessage($@"{ex.Message}{Environment.NewLine}Программа будет завершена.");
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Экземпляр тестируемого класса <see cref="DynamicReflex"/>.
        /// </summary>
        /// <remarks>
        /// Свойство потокобезопасно. Потокобезопасность обеспечивает объект <see cref="_commonLocker"/>.
        /// Значение хранит переменная <see cref="_recognizer"/>.
        /// Следует учесть, что присвоить новое значение возможно только в случае, если значение свойства равно <see langword="null"/>, иначе оно выбросит исключение <see cref="ArgumentException"/>.
        /// В случае присвоения нового значения, свойство <see cref="CurrentUndoRedoState"/> будет сброшено (<see cref="UndoRedoState.UNKNOWN"/>).
        /// </remarks>
        /// <exception cref="ArgumentException"/>
        /// <seealso cref="DynamicReflex"/>
        /// <seealso cref="_commonLocker"/>
        /// <seealso cref="_recognizer"/>
        /// <seealso cref="CurrentUndoRedoState"/>
        /// <seealso cref="UndoRedoState.UNKNOWN"/>
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

        /// <summary>
        /// Предназначено для того, чтобы указать, была ли произведена процедура освобождения используемых ресурсов перед закрытием формы или нет.
        /// </summary>
        /// <remarks>
        /// Для хранения значения, свойство использует поле <see cref="_isExited"/>.
        /// Потокобезопасность осуществляет объект <see cref="_commonLocker"/>.
        /// Свойство потокобезопасно.
        /// </remarks>
        /// <seealso cref="_isExited"/>
        /// <seealso cref="_commonLocker"/>
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

        /// <summary>
        /// Показывает необходимость завершения всех фоновых процессов.
        /// В случае наличия таковой необходимости возвращает значение <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// Свойство потокобезопасно.
        /// </remarks>
        bool NeedStopBackground => _stopBackground.WaitOne(0);

        /// <summary>
        /// Позволяет считывать и записывать параметры выбранного пользователем исторического события.
        /// </summary>
        /// <remarks>
        /// Может быть использован только в том потоке, в котором был создан <see cref="FrmExample"/>.
        /// </remarks>
        (Processor[] processors, int reflexMapIndex, string comment) SelectedResult
        {
            get => _recognizeResults[lstHistory.SelectedIndex];
            set => _recognizeResults[lstHistory.SelectedIndex] = value;
        }

        /// <summary>
        /// Поток, выполняющий текущий поисковый запрос.
        /// </summary>
        /// <remarks>
        /// Если никакой запрос не выполняется, значение свойства будет равно <see langword="null"/>.
        /// Свойство потокобезопасно как на чтение, так и на запись.
        /// Потокобезопасность обеспечивает поле <see cref="_commonLocker"/>.
        /// Значение свойства содержит поле <see cref="_recognizerThread"/>.
        /// </remarks>
        /// <seealso cref="_commonLocker"/>
        /// <seealso cref="_recognizerThread"/>
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

        /// <summary>
        /// Получает или задаёт поток, который останавливает процесс выполнения поискового запроса.
        /// </summary>
        /// <remarks>
        /// В случае, если поток не активен, в этом свойстве содержится значение <see langword="null"/>.
        /// Свойство потокобезопасно как на чтение, так и на запись.
        /// Потокобезопасность обеспечивает поле <see cref="_commonLocker"/>.
        /// Значение этого свойства содержит поле <see cref="_stoppingThread"/>.
        /// </remarks>
        /// <seealso cref="_commonLocker"/>
        /// <seealso cref="_stoppingThread"/>
        Thread StopperThread
        {
            get
            {
                lock (_commonLocker)
                {
                    return _stoppingThread;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _stoppingThread = value;
                }
            }
        }

        /// <summary>
        /// Искомое и распознаваемое изображения не должны быть прозрачными, т.к. платформа не позволяет установить параметр прозрачности.
        /// Этот параметр необходим для проверки значения прозрачности.
        /// </summary>
        public static byte DefaultOpacity => 0xFF;

        /// <summary>
        /// Недопустимые символы, которые не должен содержать путь.
        /// </summary>
        internal static HashSet<char> InvalidCharSet { get; }

        /// <summary>
        ///     Ширина искомого образа.
        /// </summary>
        internal static int ImageWidth { get; private set; }

        /// <summary>
        ///     Высота искомого образа.
        /// </summary>
        internal static int ImageHeight { get; private set; }

        /// <summary>
        ///     Название папки, содержащей изображения, интерпретируемые как карты <see cref="Processor" />, которые необходимо найти на распознаваемой(ых) карте(ах).
        /// </summary>
        internal static string ImagesFolder => "Images";

        /// <summary>
        ///     Название папки, содержащей изображения, интерпретируемые как карты <see cref="Processor" />, на которых необходимо выполнять поисковые запросы.
        /// </summary>
        internal static string RecognizeFolder => "Recognize";

        /// <summary>
        ///     Рабочий каталог приложения (<see cref="Application.StartupPath"/>).
        ///     Содержит хранилища (<see cref="SourceChanged.WORKDIR"/>, <see cref="SearchImagesPath"/>, <see cref="RecognizeImagesPath"/>) и лог программы <see cref="LogPath"/>.
        /// </summary>
        /// <seealso cref="SourceChanged.WORKDIR"/>
        /// <seealso cref="SearchImagesPath"/>
        /// <seealso cref="RecognizeImagesPath"/>
        /// <seealso cref="LogPath"/>
        /// <seealso cref="Application.StartupPath"/>
        public static string WorkingDirectory { get; } = Application.StartupPath;

        /// <summary>
        /// Указывает путь к файлу лога приложения (log.log), который находится в рабочем каталоге программы (<see cref="WorkingDirectory"/>).
        /// </summary>
        /// <seealso cref="WorkingDirectory"/>
        public string LogPath { get; }

        /// <summary>
        ///     Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor" />, которые необходимо найти на распознаваемой(ых) карте(ах).
        /// </summary>
        internal static string SearchImagesPath { get; } = Path.Combine(WorkingDirectory, ImagesFolder);

        /// <summary>
        ///     Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor" />, на которых необходимо выполнять поисковые запросы.
        /// </summary>
        internal static string RecognizeImagesPath { get; } = Path.Combine(WorkingDirectory, RecognizeFolder);

        /// <summary>
        ///     Расширение изображений, которые интерпретируются как карты <see cref="Processor" />.
        /// </summary>
        internal static string ExtImg => "bmp";

        /// <summary>
        ///     Получает состояние или задаёт его для активации или деактивации пользовательского интерфейса на время выполнения длительной операции.
        /// </summary>
        /// <remarks>
        /// Свойство потокобезопасно.
        /// Это свойство можно использовать в любом потоке.
        /// Потокобезопасность обеспечивает поле <see cref="_commonLocker"/>.
        /// Значение хранит поле <see cref="_isButtonsEnabled"/>.
        /// </remarks>
        /// <seealso cref="_isButtonsEnabled"/>
        bool IsButtonsEnabled
        {
            get
            {
                lock (_commonLocker)
                {
                    return _isButtonsEnabled;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    if (value == _isButtonsEnabled)
                        return;

                    SafeExecute(() =>
                    {
                        pbRecognizeImageDraw.Enabled = value;
                        txtRecogQueryWord.ReadOnly = !value;
                        btnLoadRecognizeImage.Enabled = value;
                        btnClearRecogImage.Enabled = value && IsPainting;
                        btnDeleteRecognizeImage.Enabled = value;

                        btnImageUpToQueries.Enabled = value;
                        btnImageCreate.Enabled = value;
                        btnImageDelete.Enabled = value;

                        btnConSaveAllImages.Enabled = value;
                        btnConSaveImage.Enabled = value;

                        btnWide.Enabled = value && pbRecognizeImageDraw.Width < pbRecognizeImageDraw.MaximumSize.Width;
                        btnNarrow.Enabled = value && pbRecognizeImageDraw.Width > pbRecognizeImageDraw.MinimumSize.Width;
                    }, true);

                    _isButtonsEnabled = value;
                }
            }
        }

        /// <summary>
        ///     Возвращает значение <see langword="true" /> в случае, если пользователь нарисовал что-либо в окне для создания
        ///     исходного изображения.
        /// </summary>
        /// <remarks>
        /// Если все пиксели изображения <see cref="_btmRecognizeImage"/> цвета <see cref="DefaultColor"/>, то изображение считается пустым.
        /// </remarks>
        /// <seealso cref="DefaultColor"/>
        /// <seealso cref="_btmRecognizeImage"/>
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
        ///     Служит либо для чтения значения последнего сохранённого поискового запроса, либо для актуализации состояния пользовательского интерфейса в случае его изменения.
        /// </summary>
        /// <remarks>
        /// Для записи значения этого свойства, необходимо записывать значение в переменную <see cref="_currentUndoRedoWord"/>.
        /// Чтение значения свойства производится из этой переменной.
        /// Записать значение свойства возможно только в случае, когда <see cref="CurrentUndoRedoState"/> не равно <see cref="UndoRedoState.UNKNOWN"/>, иначе попытка записи будет игнорирована.
        /// В случае успешной попытки записи, входное значение не сохраняется в переменной <see cref="_currentUndoRedoWord"/>.
        /// Вместо этого, производится актуализация статуса текущего поискового запроса в интерфейсе пользователя, с помощью <see cref="pbSuccess"/>, в зависимости от содержимого входного поискового запроса и состояния <see cref="CurrentUndoRedoState"/>.
        /// </remarks>
        /// <seealso cref="_currentUndoRedoWord"/>
        /// <seealso cref="CurrentUndoRedoState"/>
        /// <seealso cref="UndoRedoState"/>
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
        ///     Получает или задаёт результат завершения последнего выполненного поискового запроса, отображая его на форме.
        /// </summary>
        /// <remarks>
        /// Значение свойства содержит поле <see cref="_currentUndoRedoState"/>.
        /// Свойство можно использовать только в том потоке, в котором создана форма <see cref="FrmExample"/>.
        /// </remarks>
        /// <seealso cref="_currentUndoRedoState"/>
        /// <seealso cref="UndoRedoState"/>
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
        /// Служит для проверки содержимого поля ввода поискового запроса.
        /// В случае, если введённое слово превышает максимально допустимую длину, оно будет обрезано.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtRecogQueryWordTextCheck(object sender, EventArgs e)
        {
            if (_txtRecogQueryWordTextChecking)
                return;

            if (txtRecogQueryWord.Text.Length <= txtRecogQueryWord.MaxLength)
                return;

            _txtRecogQueryWordTextChecking = true;
            _savedRecognizeQuery = txtRecogQueryWord.Text = txtRecogQueryWord.Text.Remove(txtRecogQueryWord.MaxLength);
            _txtRecogQueryWordTextChecking = false;
        }

        /// <summary>
        /// Выполняет проверку значения <see cref="Color.A"/> указанного цвета.
        /// </summary>
        /// <param name="c">Проверяемый цвет.</param>
        /// <returns>Возвращает параметр <paramref name="c"/>.</returns>
        /// <remarks>
        /// В случае несоответствия значению <see cref="DefaultOpacity"/>, будет выброшено исключение <see cref="InvalidOperationException"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        /// <exception cref="InvalidOperationException"/>
        /// <seealso cref="Color.A"/>
        /// <seealso cref="DefaultOpacity"/>
        internal static Color CheckAlphaColor(Color c)
        {
            return c.A == DefaultOpacity
                ? c
                : throw new InvalidOperationException(
                    $@"Значение прозрачности не может быть задано 0x{c.A:X2}. Должно быть задано как 0x{DefaultOpacity:X2}.");
        }

        /// <summary>
        /// Сравнивает два изображения между собой.
        /// </summary>
        /// <param name="bitmap1">Первое изображение для сравнения.</param>
        /// <param name="bitmap2">Второе изображение для сравнения.</param>
        /// <returns>В случае равенства указанных изображений, метод возвращает значение <see langword="true"/>.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Ни одно из изображений не может быть равно <see langword="null"/>.
        /// В случае равенства ссылок, метод выбрасывает исключение <see cref="InvalidOperationException"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException"/>
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

        /// <summary>
        /// Освобождает ресурсы, занимаемые основным тестируемым экземпляром класса <see cref="DynamicReflex"/>.
        /// </summary>
        /// <remarks>
        /// Сбрасывает значение свойства <see cref="Recognizer"/>.
        /// </remarks>
        /// <seealso cref="Recognizer"/>
        void ResetRecognizer()
        {
            Recognizer = null;
        }

        /// <summary>
        ///     Останавливает поток (вызывая <see cref="Thread.Abort()"/>), который называется Recognizer, и выполняет текущий поисковый запрос.
        /// </summary>
        /// <returns>
        ///     Возвращает поток, отвечающий за остановку процесса поиска.
        ///     Если поисковый поток не был запущен, метод возвращает значение <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// Если процесс поиска не был запущен, вызов будет игнорирован.
        /// Этот поток находится в свойстве <see cref="RecognizerThread"/>, и, в случае его завершения, в этом свойстве будет содержаться значение <see langword="null"/>.
        /// Метод запускает поток (<see cref="StopperThread"/>), который останавливает процесс выполнения текущего поискового запроса, т.е. этот метод является асинхронным.
        /// Если вызвать этот метод во время обработки предыдущего запроса на остановку операции поиска, то будет возвращён экземпляр существующего потока вместо того, чтобы создать ещё один запрос на остановку.
        /// </remarks>
        /// <seealso cref="Thread.Abort()"/>
        /// <seealso cref="RecognizerThread"/>
        /// <seealso cref="StopperThread"/>
        Thread StopRecognize()
        {
            Thread result = null;

            SafeExecute(() =>
            {
                try
                {
                    ResetLogWriteMessage();

                    Thread rt = RecognizerThread;
                    result = StopperThread;

                    if (rt == null)
                        return;

                    if (result == null)
                        (StopperThread = result = new Thread(() => SafeExecute(() =>
                        {
                            try
                            {
                                rt.Abort();
                                rt.Join();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($@"{nameof(StopRecognize)}Internal: {SearchStopError}{Environment.NewLine}{ex.Message}");
                            }
                            finally
                            {
                                StopperThread = null;
                            }
                        }))
                        {
                            IsBackground = true,
                            Name = @"Stopper"
                        }).Start();
                }
                catch (Exception ex)
                {
                    throw new Exception($@"{nameof(StopRecognize)}: {SearchStopError}{Environment.NewLine}{ex.Message}");
                }
            });

            return result;
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
        ///     Создаёт и запускает новый поток (<see cref="_fileRefreshThread" />), отвечающий за обновление содержимого коллекций карт.
        ///     Созданный поток называется FileRefreshThread и находится в состоянии <see cref="ThreadState.Running" />, <see cref="ThreadState.Background" />.
        /// </summary>
        /// <remarks>
        /// В случае, если поток уже был создан, метод выбросит исключение <see cref="InvalidOperationException" />.
        /// </remarks>
        /// <exception cref="InvalidOperationException"/>
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
        /// </summary>
        /// <param name="logstr">Строка лога, которую надо записать.</param>
        /// <remarks>
        /// Метод потокобезопасен.
        /// К сообщению автоматически добавляются текущие дата и время в полном формате.
        /// </remarks>
        void WriteLogMessage(string logstr)
        {
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

            return;

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
        }

        /// <summary>
        ///     Отображает сообщение с указанным текстом, в другом потоке.
        /// </summary>
        /// <param name="message">Текст отображаемого сообщения.</param>
        void ErrorMessageInOtherThread(string message)
        {
            new Thread(() => SafeExecute(() => MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation), true))
            {
                IsBackground = true,
                Name = @"Message"
            }.Start();
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

        /// <summary>
        /// Отменяет <see cref="Thread.Abort()"/>, вызванный для текущего потока, если он находится в состоянии <see cref="ThreadState.AbortRequested"/>.
        /// Использует метод <see cref="Thread.ResetAbort()"/>.
        /// </summary>
        /// <seealso cref="Thread.Abort()"/>
        /// <seealso cref="ThreadState.AbortRequested"/>
        /// <seealso cref="Thread.ResetAbort()"/>
        static void ResetAbort()
        {
            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
                Thread.ResetAbort();
        }

        /// <summary>
        ///     Результат завершения последнего выполненного поискового запроса.
        /// </summary>
        internal enum UndoRedoState
        {
            /// <summary>
            ///     Неизвестно.
            ///     Этот статус означает, что процесс выполнения поискового запроса ещё не был запущен.
            /// </summary>
            UNKNOWN,

            /// <summary>
            ///     Поисковый запрос не был выполнен по какой-либо причине.
            /// </summary>
            ERROR,

            /// <summary>
            ///     Поисковый запрос был успешно выполнен.
            /// </summary>
            SUCCESS
        }
    }
}