using DynamicMosaic;
using DynamicParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThreadState = System.Threading.ThreadState;

namespace DynamicMosaicExample
{
    sealed partial class FrmExample : Form
    {
        /// <summary>
        /// Состояние программы после распознавания карты.
        /// </summary>
        enum RecognizeState
        {
            /// <summary>
            /// Неизвестно.
            /// Этот статус означает, что распознавание ещё не было запущено.
            /// </summary>
            UNKNOWN,

            /// <summary>
            /// Ошибка, слово изменено.
            /// Вернуться из этого статуса в статус <see cref="ERROR"/> можно путём возвращения слова в предыдущее состояние, какое оно было при выполнении процедуры распознавания.
            /// Регистр не учитывается.
            /// </summary>
            ERRORWORD,

            /// <summary>
            /// Успех, слово изменено.
            /// Вернуться из этого статуса в статус <see cref="SUCCESS"/> можно путём возвращения слова в предыдущее состояние, какое оно было при выполнении процедуры распознавания.
            /// Регистр не учитывается.
            /// </summary>
            SUCCESSWORD,

            /// <summary>
            /// Ошибка, условия не изменялись.
            /// </summary>
            ERROR,

            /// <summary>
            /// Успех, условия не изменялись.
            /// </summary>
            SUCCESS
        }

        /// <summary>
        /// Предназначен для отображения статуса программы.
        /// </summary>
        sealed class CurrentState
        {
            /// <summary>
            /// Текущее состояние программы.
            /// </summary>
            RecognizeState _state = RecognizeState.UNKNOWN;

            /// <summary>
            /// Форма, за которой производится наблюдение.
            /// </summary>
            readonly FrmExample _curForm;

            /// <summary>
            /// Искомое слово, написанное пользователем в момент запуска процедуры распознавания.
            /// </summary>
            internal string CurWord { get; private set; } = string.Empty;

            /// <summary>
            /// Инициализирует объект-наблюдатель с указанием формы, за которой требуется наблюдение.
            /// Форма не может быть равна <see langword="null"/>.
            /// </summary>
            /// <param name="frm">Форма, за которое требуется наблюдение.</param>
            internal CurrentState(FrmExample frm)
            {
                _curForm = frm ?? throw new ArgumentNullException(nameof(frm));
            }

            /// <summary>
            /// Используется для подписи на событие изменения критических данных, относящихся к распознаванию.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            internal void CriticalChange(object sender, EventArgs e)
            {
                _curForm.pbSuccess.Image = Resources.Unk_128;
                _state = RecognizeState.UNKNOWN;
            }

            /// <summary>
            /// Используется для подписи на событие изменения искомого слова.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            internal void WordChange(object sender, EventArgs e)
            {
                if (_state == RecognizeState.ERROR || _state == RecognizeState.SUCCESS)
                {
                    if (string.Compare(CurWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) == 0)
                        return;
                    _curForm.pbSuccess.Image = Resources.Unk_128;
                    _state = _state == RecognizeState.ERROR
                        ? RecognizeState.ERRORWORD
                        : RecognizeState.SUCCESSWORD;
                    return;
                }
                if (_state == RecognizeState.ERRORWORD || _state == RecognizeState.SUCCESSWORD)
                {
                    if (string.Compare(CurWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) != 0)
                        return;
                    if (_state == RecognizeState.ERRORWORD)
                    {
                        _curForm.pbSuccess.Image = Resources.Error_128;
                        _state = RecognizeState.ERROR;
                        return;
                    }
                    _curForm.pbSuccess.Image = Resources.OK_128;
                    _state = RecognizeState.SUCCESS;
                    return;
                }
                CurWord = _curForm.txtWord.Text;
            }

            /// <summary>
            /// Устанавливает текущее состояние программы.
            /// Оно может быть установлено только, если текущее состояние равно <see cref="RecognizeState.UNKNOWN"/>.
            /// В других случаях новое состояние будет игнорироваться.
            /// Установить можно либо <see cref="RecognizeState.ERROR"/>, либо <see cref="RecognizeState.SUCCESS"/>.
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
        }

        /// <summary>
        /// Объект, наблюдающий за состоянием основной формы приложения.
        /// </summary>
        readonly CurrentState _currentState;

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
        ///     Текст ошибки в случае, если отсутствуют образы для поиска (распознавания).
        /// </summary>
        const string ImagesNoExists =
            @"Образы отсутствуют. Для их добавления и распознавания необходимо создать искомые образы, нажав кнопку 'Создать образ', затем добавить искомое слово, которое так или иначе можно составить из названий искомых образов. Затем необходимо нарисовать его в поле исходного изображения. Далее нажать кнопку 'Распознать'.";

        /// <summary>
        ///     Определяет шаг (в пикселях), на который изменяется ширина сканируемого (создаваемого) изображения при нажатии
        ///     кнопок сужения или расширения.
        /// </summary>
        const int WidthCount = 20;

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        readonly Color _defaultColor = Color.White;

        /// <summary>
        ///     Таймер для измерения времени, затраченного на распознавание.
        /// </summary>
        readonly Stopwatch _stopwatch = new Stopwatch();

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
        ///     Содержит изначальное значение поля "Название" искомого образа буквы.
        /// </summary>
        readonly string _unknownSymbolName;

        /// <summary>
        /// Содержит изначальное значение поля "Название" искомого образа в <see cref="Reflex"/>.
        /// </summary>
        readonly string _unknownSystemName;

        /// <summary>
        /// Строка "Создать Reflex".
        /// </summary>
        readonly string _createReflexString;

        /// <summary>
        ///     Задаёт цвет и ширину для стирания в окне создания распознаваемого изображения.
        /// </summary>
        readonly Pen _whitePen;

        /// <summary>
        ///     Изображение, которое выводится в окне создания распознаваемого изображения.
        /// </summary>
        Bitmap _btmFront;

        /// <summary>
        ///     Индекс <see cref="ImageRect"/>, рассматриваемый в данный момент.
        /// </summary>
        int _currentImage;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае - <see langword="false" />.
        /// </summary>
        bool _draw;

        /// <summary>
        ///     Поверхность рисования в окне создания распознаваемого изображения.
        /// </summary>
        Graphics _grFront;

        /// <summary>
        /// Коллекция задействованных на данный момент элементов <see cref="Reflex" />.
        /// </summary>
        readonly List<Reflex> _workReflexes = new List<Reflex>();

        /// <summary>
        /// Содержит номера выбранных карт <see cref="Processor"/> в различных системах <see cref="Reflex"/>.
        /// </summary>
        readonly List<int> _workSelections = new List<int>();

        /// <summary>
        ///     Поток, отвечающий за выполнение процедуры распознавания.
        /// </summary>
        Thread _workThread;

        /// <summary>
        /// Поток, отвечающий за отображение процесса ожидания завершения операции.
        /// </summary>
        readonly Thread _workWaitThread;

        /// <summary>
        /// Текущий обрабатываемый объект <see cref="Reflex"/>.
        /// </summary>
        Reflex _workReflex;

        /// <summary>
        /// Уведомляет о необходимости запустить поток для обновления списка файлов изображений.
        /// </summary>
        readonly AutoResetEvent _needRefreshEvent = new AutoResetEvent(false);

        /// <summary>
        /// Предназначена для хранения задач, связанных с изменениями в файловой системе.
        /// </summary>
        readonly ConcurrentQueue<FileTask> _concurrentFileTasks = new ConcurrentQueue<FileTask>();

        /// <summary>
        /// Хранит загруженные карты, которые требуется искать на основной карте.
        /// Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ConcurrentProcessorStorage _processorStorage = new ConcurrentProcessorStorage();

        /// <summary>
        /// Синхронизирует потоки, пытающиеся записать сообщение в лог-файл.
        /// </summary>
        static readonly object _logLockerObject = new object();

        /// <summary>
        /// Служит для блокировки одновременного доступа к процедуре отображения сообщения об ошибке.
        /// </summary>
        static readonly object _logLocker = new object();

        /// <summary>
        /// Указывает, было ли просмотрено сообщение о том, что в процессе работы программы уже произошла ошибка.
        /// </summary>
        static volatile bool _errorMessageIsShowed;

        /// <summary>
        /// Содержит данные о задаче, связанной с изменениями в файловой системе.
        /// </summary>
        struct FileTask
        {
            /// <summary>
            /// Изменения, возникшие в файле или папке.
            /// </summary>
            public WatcherChangeTypes TaskType { get; private set; }

            /// <summary>
            /// Путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string FilePath { get; private set; }

            /// <summary>
            /// Исходный путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string OldFilePath { get; private set; }

            /// <summary>
            /// Указывает, был ли файл переименован в требуемуе для программы расширение.
            /// </summary>
            public bool RenamedTo { get; private set; }

            /// <summary>
            /// Указывает, был ли файл переименован из требуемуемого для программы расширения.
            /// </summary>
            public bool RenamedFrom { get; private set; }

            /// <summary>
            /// Инициализирует новый экземпляр параметрами добавляемой задачи.
            /// Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="filePath">Путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="oldFilePath">Исходный путь к файлу или папке, в которой произошли изменения.</param>
            /// <param name="renamedTo">Указывает, был ли файл переименован в требуемуе для программы расширение.</param>
            /// <param name="renamedFrom">Указывает, был ли файл переименован из требуемуемого для программы расширения.</param>
            public FileTask(WatcherChangeTypes changes, string filePath, string oldFilePath, bool renamedTo, bool renamedFrom)
            {
                TaskType = changes;
                FilePath = filePath;
                OldFilePath = oldFilePath;
                RenamedTo = renamedTo;
                RenamedFrom = renamedFrom;
            }
        }

        /// <summary>
        ///     Конструктор основной формы приложения.
        /// </summary>
        internal FrmExample()
        {
            try
            {
                InitializeComponent();
                _whitePen = new Pen(_defaultColor, 2.0f);
                Initialize();
                _strRecog = btnRecognizeImage.Text;
                _unknownSymbolName = txtSymbolPath.Text;
                _unknownSystemName = txtConSymbol.Text;
                _createReflexString = (string)lstResults.Items[0];
                _strGrpResults = grpResults.Text;
                ImageWidth = pbBrowse.Width;
                ImageHeight = pbBrowse.Height;
                lstResults.SelectedIndex = 0;
                btnConSaveImage.Image = Resources.SaveImage;
                btnConSaveAllImages.Image = Resources.SaveAllImages;
                _currentState = new CurrentState(this);
                btnNarrow.Click += _currentState.CriticalChange;
                btnWide.Click += _currentState.CriticalChange;
                btnClearImage.Click += _currentState.CriticalChange;
                btnImageDelete.Click += _currentState.CriticalChange;
                txtWord.TextChanged += _currentState.WordChange;
                fswImageChanged.Path = SearchPath;
                fswImageChanged.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                fswImageChanged.Filter = $"*.{ExtImg}";

                void OnChanged(object source, FileSystemEventArgs e) => SafetyExecute(() =>
                {
                    if (string.IsNullOrWhiteSpace(e.FullPath) || string.Compare(Path.GetExtension(e.FullPath), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) != 0)
                        return;

                    _concurrentFileTasks.Enqueue(new FileTask(e.ChangeType, e.FullPath, string.Empty, false, false));
                    _needRefreshEvent.Set();
                });

                void OnRenamed(object source, RenamedEventArgs e) => SafetyExecute(() =>
                {
                    bool renamedTo = !string.IsNullOrWhiteSpace(e.FullPath) && string.Compare(Path.GetExtension(e.FullPath), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0;
                    bool renamedFrom = !string.IsNullOrWhiteSpace(e.OldFullPath) && string.Compare(Path.GetExtension(e.OldFullPath), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0;
                    if (!renamedTo && !renamedFrom)
                        return;

                    _concurrentFileTasks.Enqueue(new FileTask(e.ChangeType, e.FullPath, e.OldFullPath, renamedTo, renamedFrom));
                    _needRefreshEvent.Set();
                });

                fswImageChanged.Changed += OnChanged;
                fswImageChanged.Created += OnChanged;
                fswImageChanged.Deleted += OnChanged;
                fswImageChanged.Renamed += OnRenamed;
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
        bool IsWorking => (_workThread?.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0;

        /// <summary>
        /// Останавливает процесс распознавания.
        /// Возвращает значение <see langword="true"/> в случае успешной остановки процесса распознавания, в противном случае возвращает значение <see langword="false"/>, в том числе, если распознавание не было запущено.
        /// </summary>
        /// <returns>Возвращает значение <see langword="true"/> в случае успешной остановки процесса распознавания, в противном случае возвращает значение <see langword="false"/>, в том числе, если распознавание не было запущено.</returns>
        bool StopRecognize()
        {
            try
            {
                if (!IsWorking)
                    return false;
                _workThread.Abort();
                if (!_workThread.Join(15000))
                    MessageBox.Show(this, @"Во время остановки распознавания произошла ошибка: поток, отвечающий за распознавание, завис. Рекомендуется перезапустить программу.", @"Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                _workThread = null;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $@"Во время остановки распознавания произошла ошибка:{Environment.NewLine}{ex.Message}", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }

        /// <summary>
        ///     Ширина образа для распознавания.
        /// </summary>
        internal static int ImageWidth { get; private set; }

        /// <summary>
        ///     Высота образа для распознавания.
        /// </summary>
        internal static int ImageHeight { get; private set; }

        /// <summary>
        /// Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor"/>, поиск которых будет осуществляться на основной карте.
        /// </summary>
        internal static string SearchPath { get; } = Application.StartupPath;

        /// <summary>
        /// Расширение изображений, которые интерпретируются как карты <see cref="Processor"/>.
        /// </summary>
        internal static string ExtImg { get; } = "bmp";

        /// <summary>
        /// Поток, отвечающий за актуализацию содержимого карт <see cref="Processor"/>.
        /// </summary>
        readonly Thread _fileThread;

        /// <summary>
        /// Отражает статус работы потока, который служит для получения всех имеющихся на данный момент образов букв для распознавания, в том числе, для актуализации их содержимого.
        /// </summary>
        readonly ManualResetEvent _imageActivity = new ManualResetEvent(false);

        /// <summary>
        /// Отражает статус работы потока распознавания изображения.
        /// </summary>
        readonly ManualResetEvent _workThreadActivity = new ManualResetEvent(false);

        /// <summary>
        /// Отражает статус процесса актуализации содержимого карт с жёсткого диска.
        /// </summary>
        bool IsFileActivity => (_fileThread?.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0;

        /// <summary>
        /// Сигнал остановки потокам, работающим на фоне.
        /// </summary>
        volatile bool _stopBackgroundThreadFlag;

        /// <summary>
        /// Отражает статус всех кнопок на данный момент.
        /// </summary>
        bool _buttonsEnabled = true;

        /// <summary>
        ///     Отключает или включает доступность кнопок на время выполнения операции.
        /// </summary>
        bool EnableButtons
        {
            set
            {
                if (value == _buttonsEnabled)
                    return;
                InvokeAction(() =>
                {
                    pbDraw.Enabled = value;
                    btnImageCreate.Enabled = value;
                    btnImageDelete.Enabled = value;
                    txtImagesCount.Enabled = value;
                    txtWord.Enabled = value;
                    btnSaveImage.Enabled = value;
                    btnLoadImage.Enabled = value;
                    btnSaveImage.Enabled = btnClearImage.Enabled = value && IsPainting;
                    btnReflexClear.Enabled = value && lstResults.Items.Count > 1;

                    if (value)
                    {
                        btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
                        btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
                        return;
                    }

                    btnWide.Enabled = false;
                    btnNarrow.Enabled = false;
                    grpResults.Text = _strGrpResults;
                });
                _buttonsEnabled = value;
            }
        }

        /// <summary>
        ///     Возвращает значение <see langword="true" /> в случае, если пользователь нарисовал что-либо в окне создания исходного изображения.
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
        ///     Перечисляет возможные значения ширины поля создания сканируемого изображения.
        ///     Используется значение шага, указанное в константе <see cref="WidthCount" />.
        /// </summary>
        IEnumerable<int> WidthSizes
        {
            get
            {
                for (int k = pbDraw.MinimumSize.Width, max = pbDraw.MaximumSize.Width; k <= max; k += WidthCount)
                    yield return k;
                for (int k = pbDraw.MaximumSize.Width, min = pbDraw.MinimumSize.Width; k >= min; k -= WidthCount)
                    yield return k;
            }
        }

        /// <summary>
        /// Представляет собой обёртку для метода, который может выдать исключение.
        /// Преобразует текст исключения, добавляя указанное имя метода, который его выдал.
        /// </summary>
        /// <param name="act">Метод, который необходимо выполнить. <see langword="null"/> игнорируется.</param>
        /// <param name="name">Имя метода, которое будет необходимо добавить к тексту исключения.</param>
        void ExceptionClause(Action act, string name)
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
        /// Создаёт новый поток для обновления списка файлов изображений, в случае, если поток (<see cref="_fileThread"/>) не выполняется.
        /// Созданный поток находится в состояниях <see cref="ThreadState.Unstarted"/> и <see cref="ThreadState.Background"/>.
        /// Поток служит для получения всех имеющихся на данный момент образов букв для распознавания, в том числе, для актуализации их содержимого.
        /// Возвращает экземпляр созданного потока или <see langword="null"/>, в случае, этот поток выполняется.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null"/>, в случае, этот поток выполняется.</returns>
        Thread CreateFileRefreshThread()
        {
            if (IsFileActivity)
                return null;

            return new Thread(() => SafetyExecute(() =>
            {
                try
                {
                    _imageActivity.Set();
                    try
                    {
                        ThreadPool.GetMinThreads(out _, out int comPortMin);
                        ThreadPool.SetMinThreads(Environment.ProcessorCount * 3, comPortMin);
                        ThreadPool.GetMaxThreads(out _, out int comPortMax);
                        ThreadPool.SetMaxThreads(Environment.ProcessorCount * 15, comPortMax);
                        Parallel.ForEach(
                            Directory.EnumerateFiles(SearchPath, $"*.{ExtImg}", SearchOption.AllDirectories),
                            (fName, state) => SafetyExecute(() =>
                            {
                                try
                                {
                                    if (_stopBackgroundThreadFlag || state.IsStopped)
                                    {
                                        state.Stop();
                                        return;
                                    }

                                    if (!string.IsNullOrWhiteSpace(fName) && string.Compare(Path.GetExtension(fName), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0)
                                        ExceptionClause(() => _processorStorage.AddProcessor(fName), $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> (addition) -> {fName}");
                                }
                                catch (Exception ex)
                                {
                                    WriteLogMessage(ex.Message);
                                }
                            }));
                    }
                    finally
                    {
                        _imageActivity.Reset();
                    }
                    while (!_stopBackgroundThreadFlag)
                    {
                        _needRefreshEvent.WaitOne();
                        _imageActivity.Set();
                        try
                        {
                            while (!_stopBackgroundThreadFlag && _concurrentFileTasks.TryDequeue(out FileTask task)) SafetyExecute(() =>
                            {
                                try
                                {
                                    switch (task.TaskType)
                                    {
                                        case WatcherChangeTypes.Deleted:
                                            if (!File.Exists(task.FilePath))
                                                ExceptionClause(() => _processorStorage.RemoveProcessor(task.FilePath), $"{nameof(ConcurrentProcessorStorage.RemoveProcessor)} -> {task.TaskType} -> {task.FilePath}");
                                            break;
                                        case WatcherChangeTypes.Renamed:
                                            if (task.RenamedFrom)
                                                ExceptionClause(() => _processorStorage.RemoveProcessor(task.OldFilePath), $"{nameof(ConcurrentProcessorStorage.RemoveProcessor)} -> {task.TaskType} -> {task.OldFilePath}");
                                            if (task.RenamedTo)
                                                ExceptionClause(() => _processorStorage.AddProcessor(task.FilePath), $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> {task.TaskType} -> {task.FilePath}");
                                            break;
                                        case WatcherChangeTypes.Created:
                                        case WatcherChangeTypes.Changed:
                                        case WatcherChangeTypes.All:
                                            ExceptionClause(() => _processorStorage.AddProcessor(task.FilePath), $"{nameof(ConcurrentProcessorStorage.AddProcessor)} -> {task.TaskType} -> {task.FilePath}");
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException($"{nameof(task)} -> {task.TaskType} -> {task.FilePath}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    WriteLogMessage(ex.Message);
                                }
                            });
                        }
                        finally
                        {
                            _imageActivity.Reset();
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
        /// Записывает сообщение в лог-файл, в синхронном режиме.
        /// Доступ к этому методу синхронизируется.
        /// К сообщению автоматически прибавляется текущая дата в полном формате.
        /// </summary>
        /// <param name="logstr">Строка лога, которую надо записать.</param>
        public void WriteLogMessage(string logstr)
        {
            void ShowMessage(string addMes)
            {
                if (_errorMessageIsShowed)
                    return;
                try
                {
                    if (!Monitor.TryEnter(_logLocker) || _errorMessageIsShowed)
                        return;
                    _errorMessageIsShowed = true;
                    ErrorMessageInOtherThread(string.IsNullOrWhiteSpace(addMes) ? @"Содержимое лог-файла обновлено. Есть новые сообщения." : addMes);
                }
                finally
                {
                    if (Monitor.IsEntered(_logLocker))
                        Monitor.Exit(_logLocker);
                }
            }

            try
            {
                logstr = $@"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {logstr}";
                string path = Path.Combine(SearchPath, "DynamicMosaicExampleLog.log");
                lock (_logLockerObject)
                {
                    using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                    using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                        sw.WriteLine(logstr);
                }
                ShowMessage(string.Empty);
            }
            catch (Exception ex)
            {
                ShowMessage($@"Ошибка при записи логов: {ex.Message}{Environment.NewLine}Сообщение лога: {logstr}");
            }
        }
    }
}
