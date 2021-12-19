using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DynamicMosaic;
using DynamicParser;
using ThreadState = System.Threading.ThreadState;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Класс основной формы приложения.
    /// </summary>
    internal sealed partial class FrmExample
    {
        /// <summary>
        ///     Предназначен для инициализации структур, отвечающих за вывод создаваемого изображения на экран.
        ///     Если предыдущее изображение присутствовало, то оно переносится на вновь созданное.
        ///     Если путь к файлу исходного изображения отсутствует, создаётся новое изображение.
        /// </summary>
        /// <param name="btmPath">Путь к файлу исходного изображения.</param>
        void ImageActualize(string btmPath = null)
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

                try
                {
                    btm = _recognizeProcessorStorage.LoadRecognizeBitmap(btmPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                pbDraw.Width = btm.Width;

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
            pbDraw.Width += WidthStep;
            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
            ImageActualize();
        }, () => btnSaveImage.Enabled = btnClearImage.Enabled = IsPainting);

        /// <summary>
        ///     Сужает область рисования распознаваемого изображения <see cref="pbDraw" /> до минимального размера по
        ///     <see cref="Control.Width" />.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnNarrow_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            pbDraw.Width -= WidthStep;
            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
            ImageActualize();
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
        ///     Возвращает окно просмотра <see cref="DynamicReflex" /> в исходное состояние.
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
            (Processor processor, string path, int count) = _imagesProcessorStorage.GetFirstProcessor(ref _currentImage);
            UpdateImagesCount(count);
            if (processor == null || count < 1)
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
            (Processor processor, string path, int count) = _imagesProcessorStorage.GetLastProcessor(ref _currentImage);
            UpdateImagesCount(count);
            if (processor == null || count < 1)
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
            if (_imagesProcessorStorage.Count < 1)
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
            using (FrmSymbol fs = new FrmSymbol(_imagesProcessorStorage))
                fs.ShowDialog();
        });

        /// <summary>
        ///     Отображает номер выбранной карты и количество карт в коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        /// <param name="count">Количество карт в коллекции <see cref="ConcurrentProcessorStorage" />.</param>
        void UpdateImagesCount(int count) => txtImagesCount.Text =
            count > 0 ? $@"{unchecked(_currentImage + 1)} / {count}" : string.Empty;

        /// <summary>
        ///     Выполняет подсчёт количества изображений для поиска.
        ///     Обновляет состояния кнопок, связанных с изображениями.
        /// </summary>
        void RefreshImagesCount() => InvokeAction(() =>
        {
            (Processor processor, string path, int count) = _imagesProcessorStorage[txtSymbolPath.Text];
            if (count > 0)
            {
                if (processor == null)
                {
                    (processor, path, count) = _imagesProcessorStorage.GetFirstProcessor(ref _currentImage);
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

            if (count < 1)
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
        ///     Создаёт новый поток для отображения статуса фоновой операции, в случае, если поток (<see cref="_workWaitThread" />)
        ///     не выполняется.
        ///     Созданный поток находится в состояниях <see cref="System.Threading.ThreadState.Unstarted" /> и
        ///     <see cref="System.Threading.ThreadState.Background" />.
        ///     Возвращает экземпляр созданного потока или <see langword="null" />, в случае, если этот поток выполняется.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или <see langword="null" />, в случае, если этот поток выполняется.</returns>
        Thread CreateWaitThread()
        {
            if ((_workWaitThread?.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0)
                return null;

            return new Thread(() => SafetyExecute(() =>
            {
                WaitHandle[] waitHandles = { _fileActivity, _recognizerActivity, _preparingActivity };
                Stopwatch stwRenew = new Stopwatch();
                for (int k = 0; k < 4; k++)
                {
                    if (WaitHandle.WaitAny(waitHandles, 0) == WaitHandle.WaitTimeout)
                    {
                        stwRenew.Reset();
                        InvokeAction(() =>
                        {
                            btnRecognizeImage.Text = _strRecog;
                            EnableButtons = true;
                            RefreshImagesCount();
                            txtWord.Select();
                        });
                        WaitHandle.WaitAny(waitHandles);
                    }

                    if (NeedStopBackground)
                        return;

                    if (!IsRecognizing)
                        EnableButtons = true;

                    stwRenew.Start();
                    if (stwRenew.ElapsedMilliseconds >= 2000 && IsFileActivity)
                    {
                        RefreshImagesCount();
                        stwRenew.Restart();
                    }

                    switch (k)
                    {
                        case 0:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text =
                                    IsRecognizing ? StrRecognize : IsPreparingActivity ? StrPreparing : IsFileActivity ? StrLoading : _strRecog;
                                TimeSpan ts = _stwRecognize.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            break;
                        case 1:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text =
                                    IsRecognizing ? StrRecognize1 : IsPreparingActivity ? StrPreparing1 : IsFileActivity ? StrLoading1 : _strRecog;
                                TimeSpan ts = _stwRecognize.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            break;
                        case 2:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text =
                                    IsRecognizing ? StrRecognize2 : IsPreparingActivity ? StrPreparing2 : IsFileActivity ? StrLoading2 : _strRecog;
                                TimeSpan ts = _stwRecognize.Elapsed;
                                lblElapsedTime.Text = $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            });
                            Thread.Sleep(100);
                            break;
                        case 3:
                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Text =
                                    IsRecognizing ? StrRecognize3 : IsPreparingActivity ? StrPreparing3 : IsFileActivity ? StrLoading3 : _strRecog;
                                TimeSpan ts = _stwRecognize.Elapsed;
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

        Bitmap RecognizeBitmapCopy
        {
            get
            {
                Bitmap btm = new Bitmap(_btmFront.Width, _btmFront.Height);
                for (int y = 0; y < _btmFront.Height; y++)
                    for (int x = 0; x < _btmFront.Width; x++)
                        btm.SetPixel(x, y, _btmFront.GetPixel(x, y));
                return btm;
            }
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Распознать".
        ///     Распознаёт изображение и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnRecognizeImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            IEnumerable<(Processor, string)> LoadRecognizingImages(DialogResult answer, Bitmap recognizeBitmap)
            {
                try
                {
                    _preparingActivity.Set();

                    switch (answer)
                    {
                        case DialogResult.Yes:
                            foreach ((Processor, string) t in RecognizeImages.Select(p => (p, p.Tag)))
                                yield return t;
                            break;
                        case DialogResult.No:
                            yield return (new Processor(recognizeBitmap, "Main"), _currentState.CurWord);//поддержать статус для распознавания множественного запроса
                            break;
                        case DialogResult.Cancel:
                            yield break;
                        default:
                            throw new Exception($@"Нажата неизвестная кнопка ({answer}).");
                    }
                }
                finally
                {
                    _recognizerActivity.Set();
                    _preparingActivity.Reset();
                }
            }

            void CreateReflex()
            {
                Processor[] processors = _imagesProcessorStorage.Elements.Select(t => t.processor).ToArray();

                if (!processors.Any())
                {
                    ErrorMessageInOtherThread(
                        @"Образы для распознавания отсутствуют. Создайте хотя бы один.");
                    return;
                }

                _currentState.CriticalChange(sender, e);
                _currentState.WordChange(sender, e);
                _stwRecognize.Restart();

                DynamicReflex recognizer;

                try
                {
                    recognizer = new DynamicReflex(new ProcessorContainer(processors));
                }
                finally
                {
                    _stwRecognize.Stop();
                }

                InvokeAction(() =>
                {
                    _recognizerReflexes.Insert(1, (recognizer, string.Empty, false, 0));
                    lstResults.Items.Insert(1, $@"({recognizer.Processors.Count()}) {DateTime.Now:HH:mm:ss}");
                    lstResults.SelectedIndex = 1;
                    grpResults.Text = $@"{_strGrpResults} ({lstResults.Items.Count - 1})";
                });
            }

            void UseSelectedReflex(DynamicReflex recognizer, (Processor, string)[] query)
            {
                _currentState.CriticalChange(sender, e);
                _currentState.WordChange(sender, e);
                _stwRecognize.Restart();

                bool result = false;

                try
                {
                    result = recognizer.FindRelation(query);

                    InvokeAction(() =>
                    {
                        SelectedReflex = (recognizer, string.Empty, result, 0);
                        lstResults.Items[lstResults.SelectedIndex] = $@"({recognizer.Processors.Count()}) {DateTime.Now:HH:mm:ss}";//сделать возможность просмотра истории выполненных запросов (в случае, если содержимое папки не успело измениться), общий результат по обработке запросов из папки (ВОЗМОЖНО, сделать автосохранение пользовательских запросов при старте распознавания)
                    });
                }
                finally
                {
                    _stwRecognize.Stop();
                    pbSuccess.Image = result ? Resources.OK_128 : Resources.Error_128;//поддержать статусы для распознавания нескольких карт
                    _currentState.State = result ? RecognizeState.SUCCESS : RecognizeState.ERROR;
                }
            }

            if (StopRecognize())
                return;

            DynamicReflex recognizerReflex = SelectedReflex.reflex;
            Bitmap recognizeBitmapCopy = RecognizeBitmapCopy;

            _errorMessageIsShowed = false;
            EnableButtons = false;

            DialogResult ans = MessageBox.Show(this, @"Обработать изображения из папки?", @"Обработка изображений",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

            (_recognizerThread = new Thread(() => SafetyExecute(() =>
            {
                try
                {
                    if (!IsPainting)
                    {
                        ErrorMessageInOtherThread(
                            @"Необходимо нарисовать какой-нибудь рисунок на рабочей поверхности.");
                        return;
                    }

                    if (string.IsNullOrEmpty(txtWord.Text))
                    {
                        ErrorMessageInOtherThread(
                            @"Напишите какое-нибудь слово, которое можно составить из одного или нескольких образов.");
                        return;
                    }

                    (Processor, string)[] query;

                    try
                    {
                        query = LoadRecognizingImages(ans, recognizeBitmapCopy).ToArray();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessageInOtherThread(ex.Message);
                        return;
                    }

                    if (recognizerReflex == null)
                        CreateReflex();
                    else
                        UseSelectedReflex(recognizerReflex, query);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                finally
                {
                    if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
                        Thread.ResetAbort();
                    _recognizerActivity.Reset();
                }
            }))
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true,
                Name = "Recognizer"
            }).Start();
            _preparingActivity.WaitOne();
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
        ///     Обрабатывает событие нажатия кнопки сохранения созданного изображения для распознавания.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnSaveImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (string.IsNullOrEmpty(txtWord.Text))
            {
                MessageBox.Show(this, SaveImageQueryError, @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _recognizeProcessorStorage.SaveToFile(new Processor(_btmFront, txtWord.Text));
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
            ImageActualize(dlgOpenImage.FileName);
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
        ///     Обрабатывает событие выбора исследуемой системы.
        ///     Отображает содержимое системы в окне "Содержимое Reflex".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void LstResults_SelectedIndexChanged(object sender, EventArgs e) => SafetyExecute(() =>
        {
            btnRecognizeImage.Text = lstResults.SelectedIndex == 0 ? @"Создать" : _strRecog;

            if (lstResults.SelectedIndex > 0)
            {
                (DynamicReflex reflex, string _, bool? _, int reflexMapIndex) = SelectedReflex;
                Processor p = reflex.Processors.ElementAt(reflexMapIndex);
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
        ///     Обрабатывает событие удаления системы из рассматриваемых.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnReflexRemove_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (lstResults.SelectedIndex < 1)
                return;
            if (lstResults.SelectedIndex == 1)
                _currentState.CriticalChange(sender, e);
            _recognizerReflexes.RemoveAt(lstResults.SelectedIndex);
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
            btnConSaveImage.Enabled = false;
            btnConSaveAllImages.Enabled = false;
        });

        /// <summary>
        ///     Обрабатывает событие выбора следующей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConNext_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            (DynamicReflex reflex, string, bool?, int reflexMapIndex) q = SelectedReflex;
            if (q.reflexMapIndex >= q.reflex.Processors.Count() - 1)
                q.reflexMapIndex = 0;
            else
                q.reflexMapIndex++;
            SelectedReflex = q;
            Processor p = q.reflex.Processors.ElementAt(q.reflexMapIndex);
            pbConSymbol.Image = ImageRect.GetBitmap(p);
            UpdateConSymbolName(p.Tag);
        });

        /// <summary>
        ///     Обрабатывает событие выбора предыдущей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConPrevious_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            (DynamicReflex reflex, string, bool?, int reflexMapIndex) q = SelectedReflex;
            if (q.reflexMapIndex < 1)
                q.reflexMapIndex = q.reflex.Processors.Count() - 1;
            else
                q.reflexMapIndex--;
            SelectedReflex = q;
            Processor p = q.reflex.Processors.ElementAt(q.reflexMapIndex);
            pbConSymbol.Image = ImageRect.GetBitmap(p);
            UpdateConSymbolName(p.Tag);
        });

        /// <summary>
        ///     Актуализирует информацию о выбранной карте рассматриваемой в данный момент системы <see cref="DynamicReflex" />.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag" />.</param>
        void UpdateConSymbolName(string tag) => txtConSymbol.Text =
            $@"№ {SelectedReflex.reflexMapIndex + 1} {tag}";

        /// <summary>
        ///     Сохраняет выбранную карту <see cref="Processor" /> выбранной системы <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
            _imagesProcessorStorage.SaveToFile(SelectedReflex.reflex.Processors.ElementAt(SelectedReflex.reflexMapIndex)));

        /// <summary>
        ///     Сохраняет все карты <see cref="Processor" /> выбранной системы <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveAllImages_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            foreach (Processor p in SelectedReflex.reflex.Processors)
                _imagesProcessorStorage.SaveToFile(p);
        });

        /// <summary>
        ///     Обрабатывает событие завершения работы программы.
        ///     Закрывает файлы, потоки.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_FormClosing(object sender, FormClosingEventArgs e) => SafetyExecute(() =>
        {
            try
            {
                StopRecognize();
                _stopBackgroundThreadEventFlag.Set();
                _needRefreshEvent.Set();
                _fileActivity.Set();
                _recognizerActivity.Set();
                _fileThread?.Join();
                _workWaitThread?.Join();
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
                BtnClearImage_Click(btnClearImage, EventArgs.Empty);
                _fileThread.Start();
                _workWaitThread.Start();

                lstResults.Items.AddRange(new object[] {

                    $@"(1) {DateTime.Now:HH:mm:ss}",
                    $@"(2) {DateTime.Now:HH:mm:ss}",
                    "Item 3, column 1",
                    $@"(3) {DateTime.Now:HH:mm:ss}",
                    "Item 5, column 1",
                    $@"(4) {DateTime.Now:HH:mm:ss}",
                    $@"(5) {DateTime.Now:HH:mm:ss}",
                    "Item 3, column 2",
                    $@"(6) {DateTime.Now:HH:mm:ss}",
                    $@"(7) {DateTime.Now:HH:mm:ss}",
                    "Item 4, column 3"});
            
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        ///     Служит для отображения имени файла карты при изменении выбранной карты.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtSymbolPath_TextChanged(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (txtSymbolPath.Text.Length < 4)
                return;
            int pos = txtSymbolPath.Text.Length - 3;
            txtSymbolPath.Select(pos, 0);
            for (int k = pos; k > -1; k--)
                if (txtSymbolPath.Text[k] == '\\' || txtSymbolPath.Text[k] == '/')
                {
                    if (k < pos)
                        txtSymbolPath.Select(k + 1, 0);
                    return;
                }

            txtSymbolPath.Select(0, 0);
        });

        /// <summary>
        ///     Предназначен для переопределения функции отката (CTRL + Z).
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

        void LstResults_DrawItem(object sender, DrawItemEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, lstResults.Items[e.Index].ToString(), e.Font,
                e.Bounds, e.ForeColor, e.BackColor, TextFormatFlags.HorizontalCenter);
        }
    }
}