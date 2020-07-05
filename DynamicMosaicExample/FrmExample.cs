using DynamicMosaic;
using DynamicParser;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Processor = DynamicParser.Processor;
using ThreadState = System.Threading.ThreadState;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Класс основной формы приложения.
    /// </summary>
    sealed partial class FrmExample
    {
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
                using (FileStream fs = new FileStream(btmPath, FileMode.Open, FileAccess.Read, FileShare.Read))
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
            pbSuccess.Image = Resources.Unk_128;
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
        void PbDraw_MouseDown(object sender, MouseEventArgs e) => SafetyExecute(() =>
        {
            _draw = true;
            DrawPoint(e.X, e.Y, e.Button);
            _currentState.CriticalChange(sender, e);
        });

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
        }, () => btnSaveImage.Enabled = btnClearImage.Enabled = IsPainting);

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
        }, () => btnSaveImage.Enabled = btnClearImage.Enabled = IsPainting);

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
            txtSymbolPath.Text = _unknownSymbolName;
            pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
        }

        /// <summary>
        ///     Возвращает окно просмотра <see cref="Reflex"/> в исходное состояние.
        /// </summary>
        void ConSymbolBrowseClear()
        {
            txtConSymbol.Text = _unknownSystemName;
            pbConSymbol.Image = new Bitmap(pbConSymbol.Width, pbConSymbol.Height);
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Следующий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageNext_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            _currentImage++;
            (Processor processor, string path, int count) = _processorStorage.GetFirstProcessor(ref _currentImage);
            UpdateImagesCount(count);
            if (processor == null || count <= 0)
            {
                SymbolBrowseClear();
                MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            pbBrowse.Image = ImageRect.GetBitmap(processor);
            txtSymbolPath.Text = path;
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Предыдущий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImagePrev_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            _currentImage--;
            (Processor processor, string path, int count) = _processorStorage.GetLastProcessor(ref _currentImage);
            UpdateImagesCount(count);
            if (processor == null || count <= 0)
            {
                SymbolBrowseClear();
                MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            pbBrowse.Image = ImageRect.GetBitmap(processor);
            txtSymbolPath.Text = path;
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Удалить".
        ///     Удаляет выбранное изображение.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageDelete_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (_processorStorage.Count <= 0)
                return;

            File.Delete(txtSymbolPath.Text);
            BtnImagePrev_Click(null, null);
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Создать образ".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageCreate_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            using (FrmSymbol fs = new FrmSymbol())
                if (fs.ShowDialog() == DialogResult.OK)
                    _currentState.CriticalChange(sender, e);
        });

        /// <summary>
        /// Отображает номер выбранной карты и количество карт в коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        /// <param name="count">Количество карт в коллекции <see cref="ConcurrentProcessorStorage"/>.</param>
        void UpdateImagesCount(int count) => txtImagesCount.Text = count > 0 ? $@"{unchecked(_currentImage + 1)} / {count}" : string.Empty;

        /// <summary>
        ///     Выполняет подсчёт количества изображений для поиска.
        ///     Обновляет состояния кнопок, связанных с изображениями.
        /// </summary>
        void RefreshImagesCount() => InvokeAction(() =>
        {
            (Processor processor, string path, int count) = _processorStorage[txtSymbolPath.Text];
            if (count > 0)
            {
                if (processor == null)
                {
                    (processor, path, count) = _processorStorage.GetFirstProcessor(ref _currentImage);
                    if (processor != null && count > 0)
                    {
                        pbBrowse.Image = ImageRect.GetBitmap(processor);
                        txtSymbolPath.Text = path;
                    }
                    else
                        SymbolBrowseClear();
                }
                else
                {
                    pbBrowse.Image = ImageRect.GetBitmap(processor);
                    txtSymbolPath.Text = path;
                }
            }
            else
            {
                _currentImage = 0;
                SymbolBrowseClear();
            }

            UpdateImagesCount(count);

            if (count <= 0)
            {
                btnImageDelete.Enabled = false;
                txtImagesCount.Enabled = false;
                btnImageNext.Enabled = false;
                btnImagePrev.Enabled = false;
                txtSymbolPath.Enabled = false;
                pbBrowse.Enabled = false;
                return;
            }

            btnImageDelete.Enabled = btnImageCreate.Enabled;
            txtImagesCount.Enabled = true;
            btnImageNext.Enabled = true;
            btnImagePrev.Enabled = true;
            txtSymbolPath.Enabled = true;
            pbBrowse.Enabled = true;
        });

        /// <summary>
        /// Создаёт новый поток для отображения статуса фоновой операции, в случае, если поток (<see cref="_workWaitThread"/>) не выполняется.
        /// Созданный поток находится в состояниях <see cref="ThreadState.Unstarted"/> и <see cref="ThreadState.Background"/>.
        /// Возвращает экземпляр созданного потока или <see langword="null"/>, в случае, если этот поток выполняется.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null"/>, в случае, если этот поток выполняется.</returns>
        Thread CreateWaitThread()
        {
            if ((_workWaitThread?.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0)
                return null;

            return new Thread(() => SafetyExecute(() =>
            {
                WaitHandle[] waitHandles = { _imageActivity, _workThreadActivity };
                Stopwatch renewStopwatch = new Stopwatch();
                for (int k = 0; k < 4; k++)
                {
                    if (WaitHandle.WaitAny(waitHandles, 0) == WaitHandle.WaitTimeout)
                    {
                        renewStopwatch.Reset();
                        InvokeAction(() =>
                        {
                            btnRecognizeImage.Text = _strRecog;
                            EnableButtons = true;
                            RefreshImagesCount();
                            txtWord.Select();
                        });
                        WaitHandle.WaitAny(waitHandles);
                    }

                    if (_stopBackgroundThreadFlag)
                        return;

                    if (!IsWorking)
                        EnableButtons = true;

                    renewStopwatch.Start();
                    if (renewStopwatch.ElapsedMilliseconds >= 2000 && IsFileActivity)
                    {
                        RefreshImagesCount();
                        renewStopwatch.Restart();
                    }

                    switch (k)
                    {
                        case 0:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text = IsWorking ? StrRecognize : IsFileActivity ? StrLoading : _strRecog;
                                TimeSpan ts = _stopwatch.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            break;
                        case 1:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text = IsWorking ? StrRecognize1 : IsFileActivity ? StrLoading1 : _strRecog;
                                TimeSpan ts = _stopwatch.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            break;
                        case 2:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text = IsWorking ? StrRecognize2 : IsFileActivity ? StrLoading2 : _strRecog;
                                TimeSpan ts = _stopwatch.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            break;
                        case 3:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text = IsWorking ? StrRecognize3 : IsFileActivity ? StrLoading3 : _strRecog;
                                TimeSpan ts = _stopwatch.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            k = -1;
                            break;
                        default:
                            k = -1;
                            break;
                    }
                }
            }))
            {
                IsBackground = true,
                Name = "WaitThread"
            };
        }

        /// <summary>
        ///     Сбрасывает сведения, накопившиеся в процессе обучения программы при распознавании.
        ///     Иными словами, эта функция заставляет программу "забыть" предыдущий опыт, накопленный в процессе распознавания
        ///     изображений.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnReflexClear_Click(object sender, EventArgs e) => InvokeAction(() =>
        {
            _currentState.CriticalChange(sender, e);
            _workReflexes.Clear();
            _workReflex = null;
            _workSelections.Clear();
            lstResults.Items.Clear();
            lstResults.SelectedIndex = -1;
            lstResults.Items.Add(_createReflexString);
            txtConSymbol.Enabled = false;
            pbConSymbol.Enabled = false;
            btnConNext.Enabled = false;
            btnConPrevious.Enabled = false;
            btnConSaveImage.Enabled = false;
            btnConSaveAllImages.Enabled = false;
            btnReflexRemove.Enabled = false;
            btnReflexClear.Enabled = false;
            grpResults.Text = _strGrpResults;
            ConSymbolBrowseClear();
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Распознать".
        ///     Распознаёт изображение и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnRecognizeImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (StopRecognize())
                return;

            _errorMessageIsShowed = false;
            EnableButtons = false;
            (_workThread = new Thread(() => SafetyExecute(() =>
            {
                try
                {
                    _workThreadActivity.Set();

                    if (txtWord.Text.Length <= 0)
                    {
                        ErrorMessageInOtherThread(
                            @"Напишите какое-нибудь слово, которое можно составить из одного или нескольких образов.");
                        return;
                    }

                    if (!IsPainting)
                    {
                        ErrorMessageInOtherThread(@"Необходимо нарисовать какой-нибудь рисунок на рабочей поверхности.");
                        return;
                    }

                    Reflex workReflex;

                    if (_workReflex == null)
                    {
                        ProcessorContainer processors = null;

                        foreach ((Processor processor, string _) in _processorStorage.Elements)
                            if (processors == null)
                                processors = new ProcessorContainer(processor);
                            else
                                processors.Add(processor);

                        if (processors == null || processors.Count < 2)
                        {
                            ErrorMessageInOtherThread(
                                @"Количество образов для распознавания должно быть не меньше двух. Нарисуйте их.");
                            return;
                        }

                        workReflex = new Reflex(processors);
                    }
                    else
                        workReflex = _workReflex;

                    _currentState.CriticalChange(sender, e);
                    _currentState.WordChange(sender, e);

                    Reflex result = null;
                    try
                    {
                        _stopwatch.Restart();
                        result = workReflex.FindRelation(new Processor(_btmFront, "Main"), _currentState.CurWord);
                    }
                    finally
                    {
                        _stopwatch.Stop();
                        if (result != null) InvokeAction(() =>
                        {
                            _workReflexes.Insert(0, result);
                            _workSelections.Insert(0, 0);
                            lstResults.Items.Insert(1, $@"({result.Count}) {DateTime.Now:HH:mm:ss}");
                            lstResults.SelectedIndex = 1;
                            btnReflexClear.Enabled = true;
                            grpResults.Text = $@"{_strGrpResults} ({lstResults.Items.Count - 1})";
                            pbSuccess.Image = Resources.OK_128;
                            _currentState.State = RecognizeState.SUCCESS;
                        });
                        else
                        {
                            pbSuccess.Image = Resources.Error_128;
                            _currentState.State = RecognizeState.ERROR;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                finally
                {
                    if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
                        Thread.ResetAbort();
                    _workThreadActivity.Reset();
                }
            }))
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true,
                Name = "Recognizer"
            }).Start();
            while (!IsWorking)
                Thread.Sleep(10);
        });

        /// <summary>
        ///     Осуществляет выход из программы по нажатию клавиши Escape.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_KeyUp(object sender, KeyEventArgs e) => SafetyExecute(() =>
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
        });

        /// <summary>
        ///     Осуществляет ввод искомого слова по нажатии клавиши Enter.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_KeyUp(object sender, KeyEventArgs e) => SafetyExecute(() =>
        {
            if (e.KeyCode == Keys.Enter)
                BtnRecognizeImage_Click(null, null);
        });

        /// <summary>
        ///     Предотвращает сигналы недопустимого ввода в текстовое поле ввода искомого слова.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_KeyPress(object sender, KeyPressEventArgs e) =>
            e.Handled = (Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab ||
                         (Keys)e.KeyChar == Keys.Pause ||
                         (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15 || e.KeyChar == 27;

        /// <summary>
        ///     Отменяет отрисовку изображения для распознавания в случае ухода указателя мыши с поля рисования.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseLeave(object sender, EventArgs e) => _draw = false;

        /// <summary>
        ///     Отвечает за отрисовку рисунка, создаваемого пользователем.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseMove(object sender, MouseEventArgs e) => SafetyExecute(() =>
        {
            if (_draw)
                DrawPoint(e.X, e.Y, e.Button);
        });

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
                    _currentState.CriticalChange(null, null);
                    btnSaveImage.Enabled = btnClearImage.Enabled = true;
                    break;
                case MouseButtons.Right:
                    _grFront.DrawRectangle(_whitePen, new Rectangle(x, y, 1, 1));
                    _currentState.CriticalChange(null, null);
                    break;
            }
        }, () => pbDraw.Refresh());

        /// <summary>
        ///     Очищает поле рисования исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnClearImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            _grFront.Clear(_defaultColor);
            btnSaveImage.Enabled = btnClearImage.Enabled = false;
        }, () => pbDraw.Refresh());

        /// <summary>
        ///     Обрабатывает событие нажатия кнопки сохранения созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnSaveImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (dlgSaveImage.ShowDialog(this) == DialogResult.OK)
                ConcurrentProcessorStorage.SaveToFile(_btmFront, dlgSaveImage.FileName);
        });

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки загрузки созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnLoadImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (dlgOpenImage.ShowDialog(this) != DialogResult.OK)
                return;
            Initialize(dlgOpenImage.FileName);
        }, () => btnSaveImage.Enabled = btnClearImage.Enabled = IsPainting);

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
        void ErrorMessageInOtherThread(string message)
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
                Processor p = _workReflex[_workSelections[lstResults.SelectedIndex - 1]];
                pbConSymbol.Image = ImageRect.GetBitmap(p);
                UpdateConSymbolName(p.Tag);
                txtConSymbol.Enabled = true;
                pbConSymbol.Enabled = true;
                btnConNext.Enabled = true;
                btnConPrevious.Enabled = true;
                btnReflexRemove.Enabled = true;
                btnConSaveImage.Enabled = true;
                btnConSaveAllImages.Enabled = true;
                return;
            }
            _workReflex = null;
            txtConSymbol.Enabled = false;
            pbConSymbol.Enabled = false;
            btnConNext.Enabled = false;
            btnConPrevious.Enabled = false;
            btnReflexRemove.Enabled = false;
            btnConSaveImage.Enabled = false;
            btnConSaveAllImages.Enabled = false;
            ConSymbolBrowseClear();
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
            if (lstResults.SelectedIndex == 1)
                _currentState.CriticalChange(sender, e);
            _workReflexes.RemoveAt(lstResults.SelectedIndex - 1);
            _workSelections.RemoveAt(lstResults.SelectedIndex - 1);
            lstResults.Items.RemoveAt(lstResults.SelectedIndex);
            lstResults.SelectedIndex = 0;
            int count = lstResults.Items.Count - 1;
            grpResults.Text = count > 0 ? $@"{_strGrpResults} ({count})" : _strGrpResults;
            ConSymbolBrowseClear();
            if (lstResults.Items.Count > 1)
                return;
            txtConSymbol.Enabled = false;
            pbConSymbol.Enabled = false;
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
            int currentReflexMapIndex = _workSelections[lstResults.SelectedIndex - 1];
            if (currentReflexMapIndex >= _workReflex.Count - 1)
                currentReflexMapIndex = 0;
            else
                currentReflexMapIndex++;
            _workSelections[lstResults.SelectedIndex - 1] = currentReflexMapIndex;
            Processor p = _workReflex[currentReflexMapIndex];
            pbConSymbol.Image = ImageRect.GetBitmap(p);
            UpdateConSymbolName(p.Tag);
        });

        /// <summary>
        /// Обрабатывает событие выбора предыдущей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConPrevious_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            int currentReflexMapIndex = _workSelections[lstResults.SelectedIndex - 1];
            if (currentReflexMapIndex <= 0)
                currentReflexMapIndex = _workReflex.Count - 1;
            else
                currentReflexMapIndex--;
            _workSelections[lstResults.SelectedIndex - 1] = currentReflexMapIndex;
            Processor p = _workReflex[currentReflexMapIndex];
            pbConSymbol.Image = ImageRect.GetBitmap(p);
            UpdateConSymbolName(p.Tag);
        });

        /// <summary>
        /// Актуализирует информацию о выбранной карте рассматриваемой в данный момент системы <see cref="Reflex"/>.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag"/>.</param>
        void UpdateConSymbolName(string tag) => txtConSymbol.Text = $@"№ {_workSelections[lstResults.SelectedIndex - 1] + 1} {ConcurrentProcessorStorage.GetProcessorName(tag)}";

        /// <summary>
        /// Сохраняет выбранную карту <see cref="Processor"/> выбранной системы <see cref="Reflex"/> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveImage_Click(object sender, EventArgs e) => SafetyExecute(() => ConcurrentProcessorStorage.SaveToFile(_workReflex[_workSelections[lstResults.SelectedIndex - 1]]));

        /// <summary>
        /// Сохраняет все карты <see cref="Processor"/> выбранной системы <see cref="Reflex"/> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveAllImages_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            for (int k = 0; k < _workReflex.Count; k++)
                ConcurrentProcessorStorage.SaveToFile(_workReflex[k]);
        });

        /// <summary>
        /// Обрабатывает событие завершения работы программы.
        /// Закрывает файлы, потоки.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_FormClosing(object sender, FormClosingEventArgs e) => SafetyExecute(() =>
        {
            try
            {
                StopRecognize();
                _stopBackgroundThreadFlag = true;
                _needRefreshEvent.Set();
                _imageActivity.Set();
                _workThreadActivity.Set();
                if ((_fileThread?.ThreadState & ThreadState.Unstarted) != ThreadState.Unstarted)
                    _fileThread?.Join(1000);
                if ((_workWaitThread?.ThreadState & ThreadState.Unstarted) != ThreadState.Unstarted)
                    _workWaitThread?.Join(500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        });

        /// <summary>
        ///     Вызывается во время первого отображения формы.
        ///     Производит инициализацию.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_Load(object sender, EventArgs e)
        {
            try
            {
                BtnClearImage_Click(null, null);
                if ((_fileThread?.ThreadState & ThreadState.Unstarted) == ThreadState.Unstarted)
                    _fileThread.Start();
                while (!IsFileActivity)
                    Thread.Sleep(10);
                if ((_workWaitThread?.ThreadState & ThreadState.Unstarted) == ThreadState.Unstarted)
                    _workWaitThread.Start();
                while ((_workWaitThread?.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) != 0)
                    Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Служит для отображения имени файла карты при изменении выбранной карты.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtSymbolPath_TextChanged(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (txtSymbolPath.Text.Length <= 3)
                return;
            int pos = txtSymbolPath.Text.Length - 3;
            txtSymbolPath.Select(pos, 0);
            for (int k = pos; k >= 0; k--)
                if (txtSymbolPath.Text[k] == '\\' || txtSymbolPath.Text[k] == '/')
                {
                    if (k < pos)
                        txtSymbolPath.Select(k + 1, 0);
                    return;
                }

            txtSymbolPath.Select(0, 0);
        });

        /// <summary>
        /// Предназначен для переопределения функции отката (CTRL + Z).
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_KeyDown(object sender, KeyEventArgs e) => SafetyExecute(() =>
        {
            if (!e.Control || e.KeyCode != Keys.Z)
                return;
            e.SuppressKeyPress = true;
            e.Handled = true;
            if (!string.IsNullOrEmpty(_currentState.CurWord))
                txtWord.Text = _currentState.CurWord;
        });
    }
}