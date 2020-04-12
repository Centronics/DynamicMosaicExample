using DynamicMosaic;
using DynamicParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Класс основной формы приложения.
    /// </summary>
    sealed partial class FrmExample : Form
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

        /// <summary>
        ///     Предназначена для инициализации структур, отвечающих за вывод создаваемого изображения на экран.
        ///     Если предыдущее изображение присутствовало, то оно переносится на вновь созданное.
        ///     Если путь к файлу исходного изображения отсутствует, создаётся новое изображение.
        /// </summary>
        /// <param name="btmPath">Путь к файлу исходного изображения.</param>
        void Initialize(string btmPath = null)
        {
            if (string.IsNullOrEmpty(btmPath))
            {
                Bitmap btm = new Bitmap(pbDraw.Width, pbDraw.Height);
                if (_btmFront != null)
                {
                    CopyBitmapByWidth(_btmFront, btm, _defaultColor);
                    _btmFront.Dispose();
                }

                _btmFront = btm;
            }
            else
            {
                Bitmap btm;
                using (FileStream fs = new FileStream(btmPath, FileMode.Open, FileAccess.Read))
                    btm = new Bitmap(fs);
                ImageFormat iformat = btm.RawFormat;
                if (!iformat.Equals(ImageFormat.Bmp))
                {
                    MessageBox.Show(this,
                        $@"Загружаемое изображение не подходит по формату: {iformat}; необходимо: {ImageFormat.Bmp}",
                        @"Ошибка");
                    btm.Dispose();
                    return;
                }

                if (WidthSizes.All(s => s != btm.Width))
                {
                    MessageBox.Show(this,
                        $@"Загружаемое изображение не подходит по ширине: {
                                btm.Width
                            }. Она выходит за рамки допустимого. Попробуйте создать изображение и сохранить его заново.",
                        @"Ошибка");
                    btm.Dispose();
                    return;
                }

                if (btm.Height != pbDraw.Height)
                {
                    MessageBox.Show(this,
                        $@"Загружаемое изображение не подходит по высоте: {btm.Height}; необходимо: {pbDraw.Height}",
                        @"Ошибка");
                    btm.Dispose();
                    return;
                }

                pbDraw.Width = btm.Width;
                btm.SetPixel(0, 0,
                    btm.GetPixel(0,
                        0)); //Необходим для устранения "Ошибки общего вида в GDI+" при попытке сохранения загруженного файла.
                _btmFront?.Dispose();
                _btmFront = btm;
                btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
                btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
            }

            _grFront?.Dispose();
            _grFront = Graphics.FromImage(_btmFront);
            pbDraw.Image = _btmFront;
        }

        /// <summary>
        ///     Копирует изображение из <see cref="Bitmap" /> до тех пор, пока не дойдёт до максимального значения по
        ///     <see cref="Image.Width" />
        ///     какого-либо из них. <see cref="Image.Height" /> должна совпадать у обоих <see cref="Bitmap" />.
        /// </summary>
        /// <param name="from"><see cref="Bitmap" />, из которого необходимо скопировать содержимое.</param>
        /// <param name="to"><see cref="Bitmap" />, в который необходимо скопировать содержимое.</param>
        /// <param name="color">
        ///     Цвет, которым необходимо заполнить требуемую область перед копированием или <see langword="null" />,
        ///     если заполнение не требуется.
        /// </param>
        static void CopyBitmapByWidth(Bitmap from, Bitmap to, Color? color)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from), $@"{nameof(CopyBitmapByWidth)}: {nameof(from)} = null.");
            if (to == null)
                throw new ArgumentNullException(nameof(to), $@"{nameof(CopyBitmapByWidth)}: {nameof(to)} = null.");
            if (from.Height != to.Height)
                throw new ArgumentOutOfRangeException(nameof(from),
                    $@"{nameof(CopyBitmapByWidth)}: Высота {nameof(from)} = ({
                            from.Height
                        }) должна быть равна той, куда осуществляется копирование {nameof(to)} = ({to.Height}).");
            if (color != null)
                using (Graphics gr = Graphics.FromImage(to))
                    gr.Clear(color.Value);
            for (int x = 0; x < from.Width && x < to.Width; x++)
                for (int y = 0; y < from.Height; y++)
                    to.SetPixel(x, y, from.GetPixel(x, y));
        }

        /// <summary>
        ///     Вызывается, когда пользователь начинает рисовать исходное изображение.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _draw = true;
            DrawPoint(e.X, e.Y, e.Button);
        }

        /// <summary>
        ///     Расширяет область рисования распознаваемого изображения <see cref="pbDraw" /> до максимального размера по
        ///     <see cref="Control.Width" />.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnWide_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            pbDraw.Width += WidthCount;
            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
            Initialize();
        }, () => btnClearImage.Enabled = IsPainting);

        /// <summary>
        ///     Сужает область рисования распознаваемого изображения <see cref="pbDraw" /> до минимального размера по
        ///     <see cref="Control.Width" />.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnNarrow_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            pbDraw.Width -= WidthCount;
            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
            Initialize();
        }, () => btnClearImage.Enabled = IsPainting);

        /// <summary>
        ///     Вызывается при отпускании клавиши мыши над полем создания исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseUp(object sender, MouseEventArgs e) => _draw = false;

        /// <summary>
        ///     Возвращает окно просмотра образов в исходное состояние.
        /// </summary>
        void SymbolBrowseClear()
        {
            txtSymbolName.Text = _unknownSymbolName;
            pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
        }

        /// <summary>
        ///     Возвращает окно просмотра <see cref="Reflex"/> в исходное состояние.
        /// </summary>
        void ReflexBrowseClear()
        {
            txtConSymbol.Text = _unknownSystemName;
            pbConSymbol.Image = new Bitmap(pbConSymbol.Width, pbConSymbol.Height);
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Следующий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageNext_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    SymbolBrowseClear();
                    MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (_currentImage >= lst.Count - 1)
                    _currentImage = 0;
                else
                    _currentImage++;
                ImageRect ir = lst[_currentImage];
                pbBrowse.Image = ir.Bitm;
                txtSymbolName.Text = ir.SymbolName;
            }, () => tmrImagesCount.Enabled = true);

        /// <summary>
        ///     Вызывается по нажатию кнопки "Предыдущий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImagePrev_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    SymbolBrowseClear();
                    MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (_currentImage <= 0)
                    _currentImage = lst.Count - 1;
                else
                    _currentImage--;
                ImageRect ir = lst[_currentImage];
                pbBrowse.Image = ir.Bitm;
                txtSymbolName.Text = ir.SymbolName;
            }, () => tmrImagesCount.Enabled = true);

        /// <summary>
        ///     Вызывается по нажатию кнопки "Удалить".
        ///     Удаляет выбранное изображение.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageDelete_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    SymbolBrowseClear();
                    return;
                }

                if (_currentImage >= lst.Count || _currentImage < 0) return;
                File.Delete(lst[_currentImage].ImagePath);
                BtnImagePrev_Click(null, null);
                BtnReflexClear_Click(btnReflexClear, new EventArgs());
            }, () =>
            {
                RefreshImagesCount();
                tmrImagesCount.Enabled = true;
            });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Создать образ".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageCreate_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                using (FrmSymbol fs = new FrmSymbol())
                    if (fs.ShowDialog() == DialogResult.OK)
                        BtnReflexClear_Click(btnReflexClear, new EventArgs());
            }, () =>
            {
                RefreshImagesCount();
                BtnImageNext_Click(null, null);
                tmrImagesCount.Enabled = true;
            });

        /// <summary>
        ///     Выполняет подсчёт количества изображений для поиска.
        ///     Обновляет состояния кнопок, связанных с изображениями.
        /// </summary>
        void RefreshImagesCount() =>
            InvokeAction(() =>
            {
                try
                {
                    long count = ImageRect.Images.LongCount();
                    txtImagesCount.Text = count.ToString();
                    if (count <= 0)
                    {
                        _imageLastCount = -1;
                        SymbolBrowseClear();
                        btnImageDelete.Enabled = false;
                        btnImageNext.Enabled = false;
                        btnImagePrev.Enabled = false;
                        return;
                    }

                    if (!btnImageNext.Enabled || !btnImagePrev.Enabled || _imageLastCount != count)
                        BtnImageNext_Click(null, null);
                    _imageLastCount = count;
                    btnImageDelete.Enabled = btnImageCreate.Enabled;
                    btnImageNext.Enabled = true;
                    btnImagePrev.Enabled = true;
                }
                catch
                {
                    tmrImagesCount.Enabled = false;
                    throw;
                }
            });

        /// <summary>
        ///     Запускает или останавливает таймер, выполняющий замер времени, затраченного на распознавание.
        /// </summary>
        void WaitableTimer() =>
            new Thread(() => SafetyExecute(() =>
            {
                _stopwatch.Restart();
                try
                {
                    #region Switcher

                    for (int k = 0; k < 4; k++)
                    {
                        switch (k)
                        {
                            case 0:
                                InvokeAction(() =>
                                {
                                    btnRecognizeImage.Text = StrRecognize;
                                    lblElapsedTime.Text =
                                        $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{
                                            _stopwatch
                                                .Elapsed.Seconds:00}";
                                });
                                Thread.Sleep(100);
                                break;
                            case 1:
                                InvokeAction(() =>
                                {
                                    btnRecognizeImage.Text = StrRecognize1;
                                    lblElapsedTime.Text =
                                        $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{
                                            _stopwatch
                                                .Elapsed.Seconds:00}";
                                });
                                Thread.Sleep(100);
                                break;
                            case 2:
                                InvokeAction(() =>
                                {
                                    btnRecognizeImage.Text = StrRecognize2;
                                    lblElapsedTime.Text =
                                        $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{
                                            _stopwatch
                                                .Elapsed.Seconds:00}";
                                });
                                Thread.Sleep(100);
                                break;
                            case 3:
                                InvokeAction(() =>
                                {
                                    btnRecognizeImage.Text = StrRecognize3;
                                    lblElapsedTime.Text =
                                        $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{
                                            _stopwatch
                                                .Elapsed.Seconds:00}";
                                });
                                k = -1;
                                Thread.Sleep(100);
                                break;
                            default:
                                k = -1;
                                break;
                        }

                        if (IsWorking)
                            continue;
                        InvokeAction(() =>
                        {
                            /*if (lstWords.Items.Count > 0)
                                lstWords.SelectedIndex = 0;*/
                        });
                        return;
                    }

                    #endregion
                }
                finally
                {
                    _stopwatch.Stop();
                    EnableButtons = true;
                }
            }, () => InvokeAction(() => btnRecognizeImage.Text = _strRecog)))
            {
                IsBackground = true,
                Name = nameof(WaitableTimer)
            }.Start();

        /// <summary>
        ///     Сбрасывает сведения, накопившиеся в процессе обучения программы при распознавании.
        ///     Иными словами, эта функция заставляет программу "забыть" предыдущий опыт, накопленный в процессе распознавания
        ///     изображений.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnReflexClear_Click(object sender, EventArgs e) => InvokeAction(() =>
        {
            _workReflexes.Clear();
            _workReflex = null;
            lstResults.Items.Clear();
            lstResults.SelectedIndex = -1;
            _currentReflexMapIndex = 0;
            lstResults.Items.Add(_createReflexString);
            btnConNext.Enabled = false;
            btnConPrevious.Enabled = false;
            btnConSaveImage.Enabled = false;
            btnConSaveAllImages.Enabled = false;
            btnReflexRemove.Enabled = false;
            btnReflexClear.Enabled = false;
            grpResults.Text = $@"{_strGrpResults} ({lstResults.Items.Count - 1})";
            ReflexBrowseClear();
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Распознать".
        ///     Распознаёт изображение и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnRecognizeImage_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                if (IsWorking)
                    return;
                EnableButtons = false;
                (_workThread = new Thread(() => SafetyExecute(() =>
                {
                    WaitableTimer();
                    List<ImageRect> images = new List<ImageRect>(ImageRect.Images);
                    if (images.Count < 2 && _workReflexes.Count <= 0)
                    {
                        MessageInOtherThread(@"Количество образов должно быть не меньше двух. Нарисуйте их.");
                        return;
                    }

                    if (txtWord.Text.Length <= 0)
                    {
                        MessageInOtherThread(
                            @"Слова отсутствуют. Добавьте какое-нибудь слово, которое можно составить из одного или нескольких образов.");
                        return;
                    }

                    if (!IsPainting)
                    {
                        MessageInOtherThread(@"Необходимо нарисовать какой-нибудь рисунок на рабочей поверхности.");
                        return;
                    }

                    Reflex workReflex = _workReflex ?? new Reflex(new ProcessorContainer((from ir in images
                                                                                          select new Processor(ir.ImageMap, ir.SymbolString)).ToArray()));
                    Reflex result = workReflex.FindRelation(new Processor(_btmFront, "Main"), txtWord.Text);
                    if (result != null)
                        InvokeAction(() =>
                        {
                            _workReflexes.Add(result);
                            _currentReflexMapIndex = 0;
                            lstResults.Items.Insert(1, DateTime.Now.ToString(@"HH:mm:ss"));
                            lstResults.SelectedIndex = 1;
                            btnReflexClear.Enabled = true;
                            grpResults.Text = $@"{_strGrpResults} ({lstResults.Items.Count - 1})";
                            pbSuccess.Image = Resources.OK_128;
                        });
                    else
                        pbSuccess.Image = Resources.Error_128;
                }))
                {
                    IsBackground = true,
                    Name = "Recognizer"
                }).Start();
            });

        /// <summary>
        ///     Осуществляет выход из программы по нажатию клавиши Escape.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Alt || e.Control || e.Shift)
                return;
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
        }

        /// <summary>
        ///     Осуществляет ввод искомого слова по нажатии клавиши Enter.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                BtnRecognizeImage_Click(null, null);
        }

        /// <summary>
        ///     Претотвращает сигналы недопустимого ввода в текстовое поле ввода искомого слова.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_KeyPress(object sender, KeyPressEventArgs e) =>
            e.Handled = ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab ||
                         (Keys)e.KeyChar == Keys.Pause ||
                         (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15);

        /// <summary>
        ///     Отменяет отрисовку изображения для распознавания в случае ухода указателя мыши с поля рисования.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseLeave(object sender, EventArgs e) => _draw = false;

        /// <summary>
        ///     Обновляет количество изображений для поиска.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TmrImagesCount_Tick(object sender, EventArgs e) => RefreshImagesCount();

        /// <summary>
        ///     Отвечает за отрисовку рисунка, создаваемого пользователем.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draw)
                DrawPoint(e.X, e.Y, e.Button);
        }

        /// <summary>
        ///     Рисует точку в указанном месте на <see cref="pbDraw" /> с применением <see cref="_grFront" />.
        /// </summary>
        /// <param name="x">Координата Х.</param>
        /// <param name="y">Координата Y.</param>
        /// <param name="button">Данные о нажатой кнопке мыши.</param>
        void DrawPoint(int x, int y, MouseButtons button) => SafetyExecute(() =>
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (button)
            {
                case MouseButtons.Left:
                    _grFront.DrawRectangle(_blackPen, new Rectangle(x, y, 1, 1));
                    btnClearImage.Enabled = true;
                    break;
                case MouseButtons.Right:
                    _grFront.DrawRectangle(_whitePen, new Rectangle(x, y, 1, 1));
                    break;
            }
        }, () => pbDraw.Refresh());

        /// <summary>
        ///     Вызывается во время первого отображения формы.
        ///     Производит инициализацию.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_Shown(object sender, EventArgs e)
        {
            BtnClearImage_Click(null, null);
            BtnImageNext_Click(null, null);
            RefreshImagesCount();
        }

        /// <summary>
        ///     Очищает поле рисования исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnClearImage_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                _grFront.Clear(_defaultColor);
                btnClearImage.Enabled = false;
            }, () => pbDraw.Refresh());

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки сохранения созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnSaveImage_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
            {
                if (dlgSaveImage.ShowDialog(this) != DialogResult.OK) return;
                using (FileStream fs = new FileStream(dlgSaveImage.FileName, FileMode.Create, FileAccess.Write))
                    _btmFront.Save(fs, ImageFormat.Bmp);
            });

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки загрузки созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnLoadImage_Click(object sender, EventArgs e) =>
            SafetyExecute(() =>
                {
                    if (dlgOpenImage.ShowDialog(this) != DialogResult.OK) return;
                    Initialize(dlgOpenImage.FileName);
                },
                () => btnClearImage.Enabled = IsPainting);

        /// <summary>
        ///     Выполняет метод с помощью метода <see cref="Control.Invoke(Delegate)" />.
        /// </summary>
        /// <param name="funcAction">Функция, которую необходимо выполнить.</param>
        /// <param name="catchAction">Функция, которая должна быть выполнена в блоке <see langword="catch" />.</param>
        void InvokeAction(Action funcAction, Action catchAction = null)
        {
            if (funcAction == null)
                return;
            try
            {
                void Act()
                {
                    try
                    {
                        funcAction();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                            catchAction?.Invoke();
                        }
                        catch (Exception ex1)
                        {
                            MessageBox.Show(this, ex1.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                        }
                    }
                }

                if (InvokeRequired)
                    Invoke((Action)Act);
                else
                    Act();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        ///     Представляет обёртку для выполнения функций с применением блоков <see langword="try" />-<see langword="catch" />,
        ///     а также выдачей сообщений обо всех
        ///     ошибках.
        /// </summary>
        /// <param name="funcAction">Функция, которая должна быть выполнена.</param>
        /// <param name="finallyAction">Функция, которая должна быть выполнена в блоке <see langword="finally" />.</param>
        /// <param name="catchAction">Функция, которая должна быть выполнена в блоке <see langword="catch" />.</param>
        void SafetyExecute(Action funcAction, Action finallyAction = null, Action catchAction = null)
        {
            try
            {
                funcAction?.Invoke();
            }
            catch (Exception ex)
            {
                try
                {
                    InvokeAction(
                        () =>
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation));
                    catchAction?.Invoke();
                }
                catch
                {
                    InvokeAction(
                        () =>
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation));
                }
            }
            finally
            {
                try
                {
                    finallyAction?.Invoke();
                }
                catch (Exception ex)
                {
                    InvokeAction(
                        () =>
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation));
                }
            }
        }

        /// <summary>
        ///     Отображает сообщение с указанным текстом в другом потоке.
        /// </summary>
        /// <param name="message">Текст отображаемого сообщения.</param>
        void MessageInOtherThread(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            SafetyExecute(() => new Thread(
                () => InvokeAction(
                    () =>
                        MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation)))
            {
                IsBackground = true,
                Name = @"Message"
            }.Start());
        }

        /// <summary>
        /// Обрабатывает событие выбора исследуемой системы.
        /// Отображает содержимое системы в окне "Содержимое <see cref="Reflex"/>".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void LstResults_SelectedIndexChanged(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (lstResults.SelectedIndex > 0)
            {
                _workReflex = _workReflexes[lstResults.SelectedIndex - 1];
                pbConSymbol.Image = ImageRect.GetBitmap(_workReflex[0]);
                btnConNext.Enabled = true;
                btnConPrevious.Enabled = true;
                btnReflexRemove.Enabled = true;
                btnConSaveImage.Enabled = true;
                btnConSaveAllImages.Enabled = true;
                _currentReflexMapIndex = 0;
                return;
            }
            _workReflex = null;
            btnConNext.Enabled = false;
            btnConPrevious.Enabled = false;
            btnReflexRemove.Enabled = false;
            btnConSaveImage.Enabled = false;
            btnConSaveAllImages.Enabled = false;
            ReflexBrowseClear();
        });

        /// <summary>
        /// Обрабатывает событие удаления системы из рассматриваемых.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnReflexRemove_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (lstResults.SelectedIndex <= 0)
                return;
            _workReflexes.RemoveAt(lstResults.SelectedIndex - 1);
            lstResults.Items.RemoveAt(lstResults.SelectedIndex);
            lstResults.SelectedIndex = 0;
            _workReflex = null;
            _currentReflexMapIndex = 0;
            grpResults.Text = $@"{_strGrpResults} ({lstResults.Items.Count - 1})";
            ReflexBrowseClear();
            if (lstResults.Items.Count > 1)
                return;
            btnConNext.Enabled = false;
            btnConPrevious.Enabled = false;
            btnReflexRemove.Enabled = false;
            btnReflexClear.Enabled = false;
            btnConSaveImage.Enabled = false;
            btnConSaveAllImages.Enabled = false;
        });

        /// <summary>
        /// Обрабатывает событие выбора следующей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConNext_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (lstResults.SelectedIndex < 0)
            {
                ReflexBrowseClear();
                return;
            }

            if (_currentReflexMapIndex >= _workReflex.Count - 1)
                _currentReflexMapIndex = 0;
            else
                _currentReflexMapIndex++;
            Processor p = _workReflex[_currentReflexMapIndex];
            pbConSymbol.Image = ImageRect.GetBitmap(p);
            txtConSymbol.Text = p.Tag;
        });

        /// <summary>
        /// Обрабатывает событие выбора предыдущей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConPrevious_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (lstResults.SelectedIndex <= 0)
            {
                ReflexBrowseClear();
                return;
            }

            if (_currentReflexMapIndex <= 0)
                _currentReflexMapIndex = _workReflex.Count - 1;
            else
                _currentReflexMapIndex--;
            Processor p = _workReflex[_currentReflexMapIndex];
            pbConSymbol.Image = ImageRect.GetBitmap(p);
            txtConSymbol.Text = p.Tag;
        });

        /// <summary>
        /// Обрабатывает событие изменения искомого слова, обнуляя результат распознавания.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_TextChanged(object sender, EventArgs e) => SafetyExecute(() => pbSuccess.Image = Resources.Unk_128);

        /// <summary>
        /// Сохраняет выбранную карту <see cref="Processor"/> выбранной системы <see cref="Reflex"/> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            Processor p = _workReflex[_currentReflexMapIndex];
            ImageRect.Save(p.Tag[0], ImageRect.GetBitmap(p));
        });

        /// <summary>
        /// Сохраняет все карты <see cref="Processor"/> выбранной системы <see cref="Reflex"/> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveAllImages_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            List<ImageRect> images = new List<ImageRect>(ImageRect.Images);
            ProcessorStorage ps = new ProcessorStorage();
            foreach (Processor p in from ir in images select new Processor(ir.ImageMap, ir.SymbolString))
                ps.AddProcessor(p);
            for (int k = 0; k < _workReflex.Count; k++)
            {
                Processor p = _workReflex[k];
                if (ps.Contains(p))
                    ImageRect.Save(p.Tag[0], ImageRect.GetBitmap(p));
            }
        });
    }
}