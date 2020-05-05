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
            string _curWord = string.Empty;

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
                    if (string.Compare(_curWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) == 0)
                        return;
                    _curForm.pbSuccess.Image = Resources.Unk_128;
                    _state = _state == RecognizeState.ERROR
                        ? RecognizeState.ERRORWORD
                        : RecognizeState.SUCCESSWORD;
                    return;
                }
                if (_state == RecognizeState.ERRORWORD || _state == RecognizeState.SUCCESSWORD)
                {
                    if (string.Compare(_curWord, _curForm.txtWord.Text, StringComparison.OrdinalIgnoreCase) != 0)
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
                _curWord = _curForm.txtWord.Text;
            }

            /// <summary>
            /// Получает или устанавливает текущее состояние программы.
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
        /// Индекс <see cref="Processor"/>, выбранный в <see cref="Reflex"/>, содержимое которого отображается в текущий момент.
        /// </summary>
        int _currentReflexMapIndex;

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
        ///     Содержит количество искомых образов, которое было на момент последней проверки.
        ///     Предназначено для того, чтобы в случае уменьшения их количества (путём удаления файлов)
        ///     обновить отображаемый образ.
        /// </summary>
        long _imageLastCount = -1;

        /// <summary>
        /// Коллекция задействованных на данный момент элементов <see cref="Reflex" />.
        /// </summary>
        readonly List<Reflex> _workReflexes = new List<Reflex>();

        /// <summary>
        ///     Поток, отвечающий за выполнение процедуры распознавания.
        /// </summary>
        Thread _workThread;

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
        readonly ConcurrentQueue<FileTask> _concurrentFileTasks;

        /// <summary>
        /// Хранит загруженные карты, которые требуется искать на основной карте.
        /// Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ConcurrentProcessorStorage _processorStorage = new ConcurrentProcessorStorage();

        /// <summary>
        /// Содержит данные о задаче, связанной с изменениями в файловой системе.
        /// </summary>
        struct FileTask
        {
            /// <summary>
            /// Изменения, возникшие в файле или папке.
            /// </summary>
            public WatcherChangeTypes? TaskType { get; private set; }

            /// <summary>
            /// Путь к файлу или папке, в которой произошли изменения.
            /// </summary>
            public string FilePath { get; private set; }

            /// <summary>
            /// Инициализирует новый экземпляр параметрами добавляемой задачи.
            /// Параметры предназначены только для чтения.
            /// </summary>
            /// <param name="changes">Изменения, возникшие в файле или папке.</param>
            /// <param name="filePath">Путь к файлу или папке, в которой произошли изменения.</param>
            public FileTask(WatcherChangeTypes changes, string filePath)
            {
                TaskType = changes;
                FilePath = filePath;
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
                _unknownSymbolName = txtSymbolName.Text;
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
                fswImageChanged.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                fswImageChanged.Filter = $"*.{ExtImg}";

                void OnChanged(object source, FileSystemEventArgs e) => InvokeAction(() =>
                {
                    _concurrentFileTasks.Enqueue(new FileTask(e.ChangeType, e.FullPath));
                    _needRefreshEvent.Set();
                });

                fswImageChanged.Changed += OnChanged;
                fswImageChanged.Created += OnChanged;
                fswImageChanged.Deleted += OnChanged;
                fswImageChanged.Renamed += OnChanged;
                fswImageChanged.EnableRaisingEvents = true;
                _fileThread = FileRefreshThread();
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
        bool IsWorking => _workThread?.IsAlive == true;

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
        /// Поток, отвечающий за актуализацию содержимого карт <see cref="Processor"/>, загруженных в программу.
        /// </summary>
        readonly Thread _fileThread;

        /// <summary>
        /// Сигнал остановки потоку, актуализирующему содержимое карт <see cref="Processor"/>.
        /// </summary>
        bool _stopFileThreadFlag;

        /// <summary>
        ///     Отключает или включает доступность кнопок на время выполнения операции.
        /// </summary>
        bool EnableButtons
        {
            set
            {
                InvokeAction(() =>
                {
                    pbDraw.Enabled = value;
                    btnImageCreate.Enabled = value;
                    btnImageDelete.Enabled = value;
                    txtImagesCount.Enabled = value;
                    txtWord.Enabled = value;
                    btnSaveImage.Enabled = value;
                    btnLoadImage.Enabled = value;
                    btnClearImage.Enabled = value && IsPainting;
                    btnReflexClear.Enabled = value;

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
        /// Создаёт новый поток для обновления списка файлов изображений, в случае, если переменная <see cref="_fileThread"/> равна <see langword="null"/>.
        /// Поток находится в запущенном состоянии.
        /// Поток служит для получения всех имеющихся на данный момент образов букв для распознавания, в том числе, для обновления этого списка с диска.
        /// Возвращает экземпляр созданного потока или <see langword="null"/>, в случае, если переменная <see cref="_fileThread"/> не равна <see langword="null"/>.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null"/>, в случае, если переменная <see cref="_fileThread"/> не равна <see langword="null"/>.</returns>
        Thread FileRefreshThread()
        {
            if (_fileThread?.IsAlive == true)
                return null;

            Thread t = new Thread(() => SafetyExecute(() =>
            {
                ThreadPool.SetMinThreads(20, 20);//РАЗОБРАТЬСЯ
                ThreadPool.SetMaxThreads(20, 20);//РАЗОБРАТЬСЯ
                ParallelOptions options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 2
                };//ИСПОЛЬЗОВАТЬ
                ParallelLoopResult result = Parallel.ForEach(Directory.EnumerateFiles(SearchPath, $"*.{ExtImg}", SearchOption.AllDirectories), options, (fName, state) => SafetyExecute(() =>
                {
                    if (!string.IsNullOrWhiteSpace(fName) && string.Compare(Path.GetExtension(fName), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0)
                        _processorStorage.AddProcessor(fName);
                }));
                if (!result.IsCompleted)
                    MessageInOtherThread(@"Не все изображения (карты) были загружены.");
                while (!_stopFileThreadFlag)
                {
                    _needRefreshEvent.WaitOne();
                    while (!_stopFileThreadFlag && _concurrentFileTasks.TryDequeue(out FileTask task)) SafetyExecute(() =>
                    {
                        switch (task.TaskType.Value)
                        {
                            case WatcherChangeTypes.Created:
                                _processorStorage.AddProcessor(task.FilePath);
                                break;
                            case WatcherChangeTypes.Deleted:
                                _processorStorage.RemoveProcessor(task.FilePath);
                                break;
                            case WatcherChangeTypes.Changed:
                            case WatcherChangeTypes.All:
                                _processorStorage.RemoveProcessor(task.FilePath);//УСЛОВНО
                                _processorStorage.AddProcessor(task.FilePath);
                                break;
                            case WatcherChangeTypes.Renamed:
                                _processorStorage.RemoveProcessor(task.FilePath);//УСЛОВНО
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(task));
                        }
                    });
                }

            }))
            {
                IsBackground = true,
                Name = nameof(FileRefreshThread)
            };
            t.Start();
            return t;
        }
    }
}
