using DynamicMosaic;
using DynamicParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
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
            public CurrentState(FrmExample frm)
            {
                _curForm = frm ?? throw new ArgumentNullException(nameof(frm));
            }

            /// <summary>
            /// Используется для подписи на событие изменения критических данных, относящихся к распознаванию.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            public void CriticalChange(object sender, EventArgs e)
            {
                _curForm.pbSuccess.Image = Resources.Unk_128;
                _state = RecognizeState.UNKNOWN;
            }

            /// <summary>
            /// Используется для подписи на событие изменения искомого слова.
            /// </summary>
            /// <param name="sender">Вызывающий объект.</param>
            /// <param name="e">Данные о событии.</param>
            public void WordChange(object sender, EventArgs e)
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
            public RecognizeState State
            {
                get => _state;
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
        ///     Конструктор основной формы приложения.
        /// </summary>
        internal FrmExample()
        {
            try
            {
                _whitePen = new Pen(_defaultColor, 2.0f);
                InitializeComponent();
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
                    tmrImagesCount.Enabled = value;
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
        ///     Возвращает значение true в случае, если пользователь нарисовал что-либо в окне создания исходного изображения.
        ///     В противном случае возвращает значение false.
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
    }
}
