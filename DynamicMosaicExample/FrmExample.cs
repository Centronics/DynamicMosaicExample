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
    internal sealed partial class FrmExample : Form
    {
        enum ImageActualizeAction
        {
            NEXT,
            PREV,
            LOAD,
            REFRESH
        }

        /// <summary>
        ///     Предназначен для инициализации структур, отвечающих за вывод создаваемого изображения на экран.
        ///     Если предыдущее изображение присутствовало, то оно переносится на вновь созданное.
        ///     Если путь к файлу исходного изображения отсутствует, создаётся новое изображение.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="btmPath">Путь к файлу исходного изображения.</param>
        void ImageActualize(ImageActualizeAction action, string btmPath = null)
        {
            void CommonMethod()
            {
                _grRecognizeImage?.Dispose();
                _grRecognizeImage = Graphics.FromImage(_btmRecognizeImage);

                pbDraw.Image = _btmRecognizeImage;
                pbSuccess.Image = Resources.Result_Unknown;
            }

            string tag;
            Bitmap btmAddingProcessor;

            switch (action)
            {
                case ImageActualizeAction.NEXT:
                    {
                        _currentRecognizeProcIndex++;
                        (Processor processor, string path, int count) =
                            _recognizeProcessorStorage.GetFirstProcessor(ref _currentRecognizeProcIndex);
                        SavedPathInfoActualize(count, path);
                        if (processor == null || count < 1)
                            return;

                        if (count == 1)
                        {
                            btnRecogNext.Enabled = false;
                            txtWord.Select();
                        }

                        tag = processor.Tag;
                        btmAddingProcessor = ImageRect.GetBitmap(processor);
                    }
                    break;
                case ImageActualizeAction.PREV:
                    {
                        _currentRecognizeProcIndex--;
                        (Processor processor, string path, int count) =
                            _recognizeProcessorStorage.GetLatestProcessor(ref _currentRecognizeProcIndex);
                        SavedPathInfoActualize(count, path);
                        if (processor == null || count < 1)
                            return;

                        tag = processor.Tag;
                        btmAddingProcessor = ImageRect.GetBitmap(processor);
                    }
                    break;
                case ImageActualizeAction.LOAD:
                    {
                        Processor p = _recognizeProcessorStorage.GetAddingProcessor(btmPath);
                        tag = p.Tag;
                        if (!_recognizeProcessorStorage.IsWorkingPath(btmPath))
                            _recognizeProcessorStorage.SaveToFile(p, string.Empty);
                        btmAddingProcessor = ImageRect.GetBitmap(p);
                    }
                    break;
                case ImageActualizeAction.REFRESH:
                    {
                        Bitmap btm = new Bitmap(pbDraw.Width, pbDraw.Height);

                        bool needReset = false;

                        if (_btmRecognizeImage != null)
                        {
                            CopyBitmapByWidth(_btmRecognizeImage, btm);
                            _btmRecognizeImage.Dispose();
                        }
                        else
                            needReset = true;

                        _btmRecognizeImage = btm;

                        CommonMethod();

                        if (needReset)
                            _grRecognizeImage.Clear(DefaultColor);

                        btnSaveRecognizeImage.Enabled = IsQueryChanged;
                        btnClearImage.Enabled = IsPainting;
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, @"Указано некорректное действие при загрузке изображения.");
            }

            try
            {
                _btmRecognizeImage?.Dispose();

                _savedRecognizeQuery = tag;
                _btmRecognizeImage = btmAddingProcessor;
                _btmSavedRecognizeCopy = RecognizeBitmapCopy;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            pbDraw.Width = _btmRecognizeImage.Width;
            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
            btnDeleteRecognizeImage.Enabled = true;
            btnClearImage.Enabled = IsPainting;
            txtWord.Text = _savedRecognizeQuery;

            RedoRecognizeImage();

            if (!string.IsNullOrEmpty(txtWord.Text))
                txtWord.Select(txtWord.Text.Length, 0);

            CommonMethod();
        }

        bool IsQueryChanged
        {
            get
            {
                if (_btmSavedRecognizeCopy == null)
                    return false;

                return txtWord.Text != _savedRecognizeQuery || !CompareBitmaps(_btmRecognizeImage, _btmSavedRecognizeCopy);
            }
        }

        /// <summary>
        ///     Копирует изображение из <see cref="Bitmap" /> до тех пор, пока не дойдёт до максимального значения по
        ///     <see cref="Image.Width" />
        ///     какого-либо из них. <see cref="Image.Height" /> должна совпадать у обоих <see cref="Bitmap" />.
        /// </summary>
        /// <param name="from"><see cref="Bitmap" />, из которого необходимо скопировать содержимое.</param>
        /// <param name="to"><see cref="Bitmap" />, в который необходимо скопировать содержимое.</param>
        void CopyBitmapByWidth(Bitmap from, Bitmap to)
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
            using (Graphics gr = Graphics.FromImage(to))
                gr.Clear(DefaultColor);
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

            switch (e.Button)
            {
                case MouseButtons.Left:
                    _currentState.CriticalChange(sender, e);
                    btnClearImage.Enabled = true;
                    break;
                case MouseButtons.Right:
                    _currentState.CriticalChange(null, null);
                    break;
            }
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
            ImageActualize(ImageActualizeAction.REFRESH);
        }, () => btnClearImage.Enabled = IsPainting);

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
            ImageActualize(ImageActualizeAction.REFRESH);
        }, () => btnClearImage.Enabled = IsPainting);

        /// <summary>
        ///     Вызывается при отпускании клавиши мыши над полем создания исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseUp(object sender, MouseEventArgs e) => DrawStop();

        /// <summary>
        ///     Вызывается по нажатию кнопки "Следующий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageNext_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            _currentImageIndex++;
            ShowCurrentImage(_imagesProcessorStorage.GetFirstProcessor(ref _currentImageIndex));
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Предыдущий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImagePrev_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            _currentImageIndex--;
            ShowCurrentImage(_imagesProcessorStorage.GetLatestProcessor(ref _currentImageIndex));
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

            DeleteFile(txtSymbolPath.Text);
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

        void SavedPathInfoActualize(int count, string recogPath)
        {
            if (count > 0)
            {
                txtRecogNumber.Text = unchecked(_currentRecognizeProcIndex + 1).ToString();
                txtRecogCount.Text = count.ToString();
            }
            else
            {
                txtRecogNumber.Text = string.Empty;
                txtRecogCount.Text = string.Empty;
            }

            txtRecogPath.Text = recogPath;

            bool isExists = count > 0 && ConcurrentProcessorStorage.IsFileExists(recogPath);

            btnDeleteRecognizeImage.Enabled = isExists;
            btnSaveRecognizeImage.Enabled = !isExists || IsQueryChanged;
        }

        int ShowCurrentImage((Processor imageProcessor, string imagePath, int imageCount) t)
        {
            if (t.imageCount > 0)
            {
                txtImagesNumber.Text = checked(_currentImageIndex + 1).ToString();
                txtImagesCount.Text = t.imageCount.ToString();
            }
            else
            {
                txtImagesNumber.Text = string.Empty;
                txtImagesCount.Text = string.Empty;
            }

            if (t.imageProcessor != null && t.imageCount > 0)
            {
                pbBrowse.Image = ImageRect.GetBitmap(t.imageProcessor);
                txtSymbolPath.Text = t.imagePath;
            }
            else
            {
                pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
                txtSymbolPath.Text = _unknownSymbolName;
            }

            return t.imageCount;
        }

        /// <summary>
        ///     Выполняет подсчёт количества изображений для поиска.
        ///     Обновляет состояния кнопок, связанных с изображениями.
        /// </summary>
        void RefreshImagesCount() => InvokeAction(() =>
        {
            int imageCount = ShowCurrentImage(_imagesProcessorStorage.GetFirstProcessor(ref _currentImageIndex, true));

            btnImageUpToQueries.Enabled = btnImageDelete.Enabled = ButtonsEnabled && imageCount > 0;
            btnImageNext.Enabled = imageCount > 1;
            btnImagePrev.Enabled = imageCount > 1;
            txtImagesNumber.Enabled = imageCount > 0;
            txtImagesCount.Enabled = imageCount > 0;
            txtSymbolPath.Enabled = imageCount > 0;
            pbBrowse.Enabled = imageCount > 0;

            (Processor recogProcessor, string recogPath, int recogCount) = _recognizeProcessorStorage.GetFirstProcessor(ref _currentRecognizeProcIndex, true);

            try
            {
                bool isQueryChanged = IsQueryChanged;

                switch (_needInitRecognizeImage)
                {
                    case true when recogCount > 0 && !isQueryChanged:
                        if (recogProcessor != null)
                            ImageActualize(ImageActualizeAction.LOAD, recogPath);

                        btnRecogNext.Enabled = recogCount > 1;
                        btnRecogPrev.Enabled = recogCount > 1;
                        txtRecogNumber.Enabled = true;
                        txtRecogCount.Enabled = true;
                        txtRecogPath.Enabled = true;

                        SavedPathInfoActualize(recogCount, recogPath);
                        return;

                    case false when recogCount < 1 && !isQueryChanged:
                        _savedRecognizeQuery = string.Empty;
                        txtWord.Text = string.Empty;

                        _currentState.CriticalChange(null, null);
                        ImageActualize(ImageActualizeAction.REFRESH);
                        _grRecognizeImage.Clear(DefaultColor);
                        pbDraw.Refresh();
                        _btmSavedRecognizeCopy = RecognizeBitmapCopy;
                        break;
                }

                SavedPathInfoActualize(recogCount, recogPath);

                btnRecogNext.Enabled = recogCount > 1 || isQueryChanged || txtWord.Text != _savedRecognizeQuery || (recogProcessor != null && !CompareBitmaps(_btmRecognizeImage, ImageRect.GetBitmap(recogProcessor)));
                btnRecogPrev.Enabled = recogCount > 1;
                txtRecogNumber.Enabled = recogCount > 1;
                txtRecogCount.Enabled = recogCount > 1;
                txtRecogPath.Enabled = recogCount > 1;
            }
            finally
            {
                _needInitRecognizeImage = recogCount <= 0;
            }
        });

        /// <summary>
        ///     Создаёт новый поток для отображения статуса фоновых операций, в случае, если поток (<see cref="_userInterfaceThread" />)
        ///     не существует.
        ///     Созданный поток находится в состояниях <see cref="ThreadState.Running" /> и <see cref="ThreadState.Background" />.
        ///     Возвращает экземпляр созданного потока или исключение <see cref="InvalidOperationException"/>, в случае, если этот поток уже был создан.
        /// </summary>
        /// <returns>Возвращает экземпляр созданного потока или исключение <see cref="InvalidOperationException"/>, в случае, если этот поток уже был создан.</returns>
        void InitializeUserInterface()
        {
            if (_userInterfaceThread != null)
                throw new InvalidOperationException($@"Попытка вызвать метод {nameof(InitializeUserInterface)} в то время, когда поток {nameof(_userInterfaceThread)} уже существует.");

            Thread thread = new Thread(() => SafetyExecute(() =>
            {
                WaitHandle[] waitHandles = { _stopBackground, _recognizerActivity, _recogPreparing, _fileActivity };

                Stopwatch stwRenew = new Stopwatch();
                bool initializeUserInterface = false;

                for (int k = 0; ; k++)
                {
                    int eventIndex = WaitHandle.WaitAny(waitHandles, 0);

                    switch (eventIndex)
                    {
                        case WaitHandle.WaitTimeout:
                            stwRenew.Reset();

                            InvokeAction(() =>
                            {
                                btnRecognizeImage.Image = _imgSearchDefault;
                                btnRecognizeImage.Text = string.Empty;
                                ButtonsEnabled = true;
                                RefreshImagesCount();
                                txtWord.Select();

                                if (initializeUserInterface)
                                    return;

                                initializeUserInterface = true;

                                CreateImageWatcher();
                                CreateRecognizeWatcher();
                                CreateWorkDirWatcher();

                                CreateFileRefreshThread();

                                ThreadPool.QueueUserWorkItem(state => SafetyExecute(() => ChangedThreadFunction(WatcherChangeTypes.Created, SearchImagesPath, _imagesProcessorStorage, SourceChanged.IMAGES)));
                                ThreadPool.QueueUserWorkItem(state => SafetyExecute(() => ChangedThreadFunction(WatcherChangeTypes.Created, RecognizeImagesPath, _recognizeProcessorStorage, SourceChanged.RECOGNIZE)));
                            });

                            eventIndex = WaitHandle.WaitAny(waitHandles);

                            if (eventIndex == 0)
                                return;

                            break;

                        case 0:
                            return;
                    }

                    stwRenew.Start();

                    if (stwRenew.ElapsedMilliseconds >= 2000)
                    {
                        RefreshImagesCount();
                        stwRenew.Restart();
                    }

                    void OutCurrentStatus(string strPreparing, string strLoading) => InvokeAction(() =>
                    {
                        string CreateTimeString(TimeSpan ts) => $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";

                        switch (eventIndex)
                        {
                            case 0:
                                throw new Exception($@"Этот ID произошедшего события недопустим ({eventIndex}).");

                            case 1:
                                btnRecognizeImage.Image = null;
                                btnRecognizeImage.Text = CreateTimeString(_stwRecognize.Elapsed);
                                return;

                            case 2:
                                btnRecognizeImage.Image = null;
                                btnRecognizeImage.Text = strPreparing;
                                ButtonsEnabled = true;
                                return;

                            case 3:
                                btnRecognizeImage.Image = null;
                                btnRecognizeImage.Text = strLoading;
                                ButtonsEnabled = true;
                                return;

                            default:
                                btnRecognizeImage.Text = string.Empty;
                                btnRecognizeImage.Image = _imgSearchDefault;
                                ButtonsEnabled = true;
                                return;
                        }
                    });

                    switch (k)
                    {
                        case 0:
                            OutCurrentStatus(StrPreparing0, StrLoading0);
                            Thread.Sleep(100);
                            break;
                        case 1:
                            OutCurrentStatus(StrPreparing1, StrLoading1);
                            Thread.Sleep(100);
                            break;
                        case 2:
                            OutCurrentStatus(StrPreparing2, StrLoading2);
                            Thread.Sleep(100);
                            break;
                        case 3:
                            OutCurrentStatus(StrPreparing3, StrLoading3);
                            Thread.Sleep(100);
                            k = -1;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(k), k, @"Некорректное значение индикатора для отображения статуса.");
                    }
                }
            }))
            {
                IsBackground = true,
                Name = "WaitThread"
            };

            thread.Start();

            _userInterfaceThread = thread;
        }

        Bitmap RecognizeBitmapCopy
        {
            get
            {
                Bitmap btm = new Bitmap(_btmRecognizeImage.Width, _btmRecognizeImage.Height);
                for (int y = 0; y < _btmRecognizeImage.Height; y++)
                    for (int x = 0; x < _btmRecognizeImage.Width; x++)
                        btm.SetPixel(x, y, _btmRecognizeImage.GetPixel(x, y));
                return btm;
            }
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Найти".
        ///     Находит заданные изображения и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnRecognizeImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (!StopRecognize())
            {
                WriteLogMessage($@"{nameof(BtnRecognizeImage_Click)}: Ошибка при остановке потока перед запуском новой сессии поиска.");
                return;
            }

            if (string.IsNullOrEmpty(txtWord.Text))
            {
                ErrorMessageInOtherThread(
                    @"Напишите какое-нибудь слово, которое можно составить из одного или нескольких образов.");
                return;
            }

            if (IsQueryChanged)
                SaveRecognizeImage(true);

            lock (ErrorMessageLocker)
                _errorMessageIsShowed = false;

            ButtonsEnabled = false;
            lstHistory.SelectedIndex = -1;

            _currentState.CriticalChange(sender, e);
            _currentState.WordChange(sender, e);

            (RecognizerThread = new Thread(() => SafetyExecute(() =>
            {
                _recogPreparing.Set();

                bool? result = null;
                bool resultShowed = false;

                void ShowResult(DynamicReflex reflex)
                {
                    if (resultShowed || !result.HasValue)
                        return;

                    resultShowed = true;

                    OutHistory(result, reflex.Processors.ToArray());

                    if (result.Value)
                    {
                        pbSuccess.Image = Resources.Result_OK;
                        _currentState.State = RecognizeState.SUCCESS;
                        return;
                    }

                    pbSuccess.Image = Resources.Result_Error;
                    _currentState.State = RecognizeState.ERROR;
                }

                DynamicReflex recognizer = Recognizer;

                try
                {
                    if (recognizer == null)
                    {
                        List<Processor> processors = _imagesProcessorStorage.UniqueElements.Select(t => t.processor).ToList();

                        if (!processors.Any())
                        {
                            ErrorMessageInOtherThread(@"Образы для поиска отсутствуют. Создайте хотя бы два.");
                            return;
                        }

                        recognizer = new DynamicReflex(new ProcessorContainer(processors));

                        Recognizer = recognizer;

                        OutHistory(null, recognizer.Processors.ToArray(), @"start");
                    }

                    (Processor, string)[] query = _recognizeProcessorStorage.Elements.Select(t => (t.processor, t.processor.Tag)).ToArray();

                    if (!query.Any())
                    {
                        ErrorMessageInOtherThread(@"Поисковые запросы отсутствуют. Создайте хотя бы один.");
                        return;
                    }

                    _recognizerActivity.Set();

                    _stwRecognize.Restart();
                    result = recognizer.FindRelation(query);
                    _stwRecognize.Stop();

                    ShowResult(recognizer);
                }
                catch (ThreadAbortException)
                {
                    _stwRecognize.Stop();
                    Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    _stwRecognize.Stop();
                    ErrorMessageInOtherThread(ex.Message);
                }
                finally
                {
                    _stwRecognize.Stop();
                    ShowResult(recognizer);
                }
            }, () =>
            {
                _recogPreparing.Reset();
                _recognizerActivity.Reset();

                RecognizerThread = null;
            }))
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true,
                Name = "Recognizer"
            }).Start();

            _recogPreparing.WaitOne();
        });

        void OutHistory(bool? result, Processor[] ps, string comment = null) => InvokeAction(() =>
        {
            string GetSystemName(int count)
            {
                string strCount = string.IsNullOrEmpty(comment)
                    ? lstHistory.Items.Count < 10 ? ps.Length > 99999 ? "∞" : ps.Length.ToString() :
                    ps.Length > 9999 ? "∞" : ps.Length.ToString()
                    : comment;

                char r = result != null ? result.Value ? 'T' : 'F' : ' ';

                return $@"№{count}{r}{DateTime.Now:HH:mm:ss} ({strCount})";
            }

            if (lstHistory.Items.Count < 100)
            {
                string sm = GetSystemName(lstHistory.Items.Count);

                _recognizeResults.Insert(0, (ps, 0, sm));
                lstHistory.Items.Insert(0, sm);
                lstHistory.SelectedIndex = 0;
                return;
            }

            if (_currentHistoryPos == 100)
                _currentHistoryPos = 0;

            int pos = 99 - _currentHistoryPos;

            _recognizeResults.RemoveAt(pos);
            lstHistory.Items.RemoveAt(pos);

            string si = GetSystemName(_currentHistoryPos);

            _recognizeResults.Insert(pos, (ps, 0, si));
            lstHistory.Items.Insert(pos, si);

            lstHistory.SelectedIndex = pos;

            _currentHistoryPos++;
        });

        /// <summary>
        ///     Осуществляет ввод искомого слова по нажатии клавиши Enter.
        ///     Предназначен для переопределения функции отката (CTRL + Z).
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_KeyDown(object sender, KeyEventArgs e) => SafetyExecute(() =>
        {
            switch (e.KeyCode)
            {
                case Keys.Enter when txtWord.Focused:
                    BtnRecognizeImage_Click(btnRecognizeImage, EventArgs.Empty);
                    break;
                case Keys.Z when e.Control && txtWord.Focused:
                    if (string.IsNullOrEmpty(_currentState.CurWord))
                        return;
                    txtWord.Text = _currentState.CurWord;
                    txtWord.Select(txtWord.Text.Length, 0);
                    return;
                case Keys.Escape:
                    Application.Exit();
                    return;
            }
        });

        /// <summary>
        ///     Предотвращает сигналы недопустимого ввода в текстовое поле ввода искомого слова.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtWord_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch ((Keys)e.KeyChar)
            {
                case Keys.Enter:
                case Keys.Tab:
                case Keys.Escape:
                case Keys.Pause:
                case Keys.XButton1:
                case Keys.RButton | Keys.Enter:
                    e.Handled = true;
                    return;
            }
        }

        /// <summary>
        ///     Отменяет отрисовку изображения для распознавания в случае ухода указателя мыши с поля рисования.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseLeave(object sender, EventArgs e) => DrawStop();

        void RedoRecognizeImage()
        {
            bool changed = IsQueryChanged;

            btnSaveRecognizeImage.Enabled = changed || !btnDeleteRecognizeImage.Enabled;

            int count = _recognizeProcessorStorage.Count;

            btnRecogNext.Enabled = (changed && count > 0) || count > 1;
        }

        void DrawStop() => SafetyExecute(() =>
        {
            _draw = false;

            RedoRecognizeImage();
        });

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
        ///     Рисует точку в указанном месте на <see cref="pbDraw" /> с применением <see cref="_grRecognizeImage" />.
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
                    _grRecognizeImage.DrawRectangle(BlackPen, new Rectangle(x, y, 1, 1));
                    break;
                case MouseButtons.Right:
                    _grRecognizeImage.DrawRectangle(WhitePen, new Rectangle(x, y, 1, 1));
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
            _grRecognizeImage.Clear(DefaultColor);
            btnClearImage.Enabled = IsPainting;
            btnSaveRecognizeImage.Enabled = IsQueryChanged;
        }, () => pbDraw.Refresh());

        /// <summary>
        ///     Обрабатывает событие нажатия кнопки сохранения созданного изображения для распознавания.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnSaveRecognizeImage_Click(object sender, EventArgs e) => SafetyExecute(() => SaveRecognizeImage(false));

        void SaveRecognizeImage(bool rewrite)
        {
            string tag = txtWord.Text;

            rewrite &= tag == _savedRecognizeQuery;

            string savedRecognizePath = _recognizeProcessorStorage.SavedRecognizePath;

            if ((!rewrite && string.IsNullOrEmpty(tag)) || (rewrite && string.IsNullOrEmpty(savedRecognizePath)))
            {
                MessageBox.Show(this, SaveImageQueryError, @"Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CreateFolder(_recognizeProcessorStorage.ImagesPath);

            string pathToSave = rewrite ? savedRecognizePath : string.Empty;
            string savedPath = _recognizeProcessorStorage.SaveToFile(new Processor(_btmRecognizeImage, tag), pathToSave);
            ImageActualize(ImageActualizeAction.LOAD, savedPath);
        }

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки загрузки созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnLoadRecognizeImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            if (dlgOpenImage.ShowDialog(this) == DialogResult.OK)
                ImageActualize(ImageActualizeAction.LOAD, dlgOpenImage.FileName);
        });

        /// <summary>
        ///     Выполняет метод с помощью метода <see cref="Control.Invoke(Delegate)" />.
        /// </summary>
        /// <param name="funcAction">Функция, которую необходимо выполнить.</param>
        /// <param name="catchAction">Функция, которая должна быть выполнена в блоке <see langword="catch" />.</param>
        void InvokeAction(Action funcAction, Action catchAction = null)
        {
            if (funcAction == null)
            {
                WriteLogMessage($@"{nameof(InvokeAction)}: funcAction = null.");

                return;
            }

            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
            {
                WriteLogMessage($@"{nameof(InvokeAction)}: AbortRequested.");

                return;
            }

            try
            {
                void Act()
                {
                    try
                    {
                        funcAction();
                    }
                    catch (ThreadAbortException ex)
                    {
                        Thread.ResetAbort();

                        WriteLogMessage($@"{nameof(InvokeAction)}(1): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            WriteLogMessage($@"{nameof(InvokeAction)}(2): {ex.Message}");

                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                            catchAction?.Invoke();
                        }
                        catch (Exception ex1)
                        {
                            WriteLogMessage($@"{nameof(InvokeAction)}(3): {ex1.Message}");

                            MessageBox.Show(this, ex1.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                }

                if (InvokeRequired)
                    Invoke((Action)Act);
                else
                    Act();
            }
            catch (ThreadAbortException ex)
            {
                Thread.ResetAbort();

                WriteLogMessage($@"{nameof(InvokeAction)}(4): {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteLogMessage($@"{nameof(InvokeAction)}(5): {ex.Message}");

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
            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
            {
                WriteLogMessage($@"{nameof(SafetyExecute)}: AbortRequested.");

                return;
            }

            try
            {
                funcAction?.Invoke();
            }
            catch (Exception ex)
            {
                try
                {
                    WriteLogMessage($@"{nameof(SafetyExecute)}(1): {ex.Message}");

                    InvokeAction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));

                    catchAction?.Invoke();
                }
                catch (Exception ex1)
                {
                    WriteLogMessage($@"{nameof(SafetyExecute)}(2): {ex1.Message}");

                    InvokeAction(() => MessageBox.Show(this, ex1.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
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
                    WriteLogMessage($@"{nameof(SafetyExecute)}(3): {ex.Message}");

                    InvokeAction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
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
            {
                WriteLogMessage($@"{nameof(ErrorMessageInOtherThread)}: {message}");

                return;
            }

            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
            {
                WriteLogMessage($@"{nameof(ErrorMessageInOtherThread)}: AbortRequested.");

                return;
            }

            SafetyExecute(() =>
                new Thread(() => InvokeAction(() => MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)))
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
        void LstResults_SelectedIndexChanged(object sender, EventArgs e) => SafetyExecute(ChangeSystemSelectedIndex);

        void ChangeSystemSelectedIndex()
        {
            if (lstHistory.SelectedIndex < 0)
            {
                txtConSymbolNumber.Text = string.Empty;
                txtConSymbolCount.Text = string.Empty;
                txtConSymbolTag.Text = string.Empty;

                pbConSymbol.Image = new Bitmap(pbConSymbol.Width, pbConSymbol.Height);

                txtConSymbolNumber.Enabled = false;
                txtConSymbolCount.Enabled = false;
                txtConSymbolTag.Enabled = false;
                pbConSymbol.Enabled = false;
                btnConNext.Enabled = false;
                btnConPrevious.Enabled = false;
                btnConSaveImage.Enabled = false;
                btnConSaveAllImages.Enabled = false;
                return;
            }

            (Processor[] processors, int reflexMapIndex, string _) = SelectedResult;

            Processor p = processors[reflexMapIndex];

            txtConSymbolNumber.Text = checked(reflexMapIndex + 1).ToString();
            txtConSymbolCount.Text = processors.Length.ToString();
            txtConSymbolTag.Text = p.Tag;

            pbConSymbol.Image = ImageRect.GetBitmap(p);

            txtConSymbolNumber.Enabled = true;
            txtConSymbolCount.Enabled = true;
            txtConSymbolTag.Enabled = true;
            pbConSymbol.Enabled = true;
            btnConNext.Enabled = true;
            btnConPrevious.Enabled = true;
            btnConSaveImage.Enabled = true;
            btnConSaveAllImages.Enabled = true;
        }

        /// <summary>
        ///     Обрабатывает событие выбора следующей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConNext_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            (Processor[] processors, int reflexMapIndex, string) q = SelectedResult;

            if (q.reflexMapIndex >= q.processors.Length - 1)
                q.reflexMapIndex = 0;
            else
                q.reflexMapIndex++;

            SelectedResult = q;

            ChangeSystemSelectedIndex();
        });

        /// <summary>
        ///     Обрабатывает событие выбора предыдущей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConPrevious_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            (Processor[] processors, int reflexMapIndex, string) q = SelectedResult;

            if (q.reflexMapIndex < 1)
                q.reflexMapIndex = q.processors.Length - 1;
            else
                q.reflexMapIndex--;

            SelectedResult = q;

            ChangeSystemSelectedIndex();
        });

        /// <summary>
        ///     Сохраняет выбранную карту <see cref="Processor" /> выбранной системы <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            CreateFolder(_imagesProcessorStorage.ImagesPath);

            _imagesProcessorStorage.SaveToFile(SelectedResult.processors[SelectedResult.reflexMapIndex], SelectedResult.systemName, SelectedResult.reflexMapIndex.ToString());
        });

        /// <summary>
        ///     Сохраняет все карты <see cref="Processor" /> выбранной системы <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveAllImages_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            CreateFolder(_imagesProcessorStorage.ImagesPath);

            for (int k = 0; k < SelectedResult.processors.Length; k++)
                _imagesProcessorStorage.SaveToFile(SelectedResult.processors[k], SelectedResult.systemName, k.ToString());
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
                DisposeWorkDirWatcher();
                DisposeRecognizeWatcher();
                DisposeImageWatcher();
                if (!StopRecognize())
                    WriteLogMessage($@"{nameof(FrmExample_FormClosing)}: Ошибка остановки поиска при завершении работы программы.");
                _stopBackground.Set();
                ConcurrentProcessorStorage.LongOperationsAllowed = false;
                _fileRefreshThread?.Join();
                _userInterfaceThread?.Join();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        });

        /// <summary>
        ///     Служит для отображения имени файла карты при изменении выбранной карты.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TextBoxTextChanged(object sender, EventArgs e) => SafetyExecute(() =>
        {
            TextBox tb = (TextBox)sender;

            if (tb.Text.Length < 4)
                return;

            int pos = tb.Text.Length - 3;
            tb.Select(pos, 0);
            for (int k = pos; k > -1; k--)
                if (ConcurrentProcessorStorage.IsDirectorySeparatorSymbol(tb.Text[k]))
                {
                    if (k < pos)
                        tb.Select(k + 1, 0);
                    return;
                }

            tb.Select(0, 0);
        });

        void LstResults_DrawItem(object sender, DrawItemEventArgs e) => SafetyExecute(() =>
        {
            if (e.Index < 0)
                return;

            TextRenderer.DrawText(e.Graphics, lstHistory.Items[e.Index].ToString(), e.Font,
                e.Bounds, e.ForeColor, e.BackColor, TextFormatFlags.HorizontalCenter);
        });

        void BtnNextRecogImage_Click(object sender, EventArgs e) => SafetyExecute(() => ImageActualize(ImageActualizeAction.NEXT));

        void BtnPrevRecogImage_Click(object sender, EventArgs e) => SafetyExecute(() => ImageActualize(ImageActualizeAction.PREV));

        void BtnImageUpToQueries_Click(object sender, EventArgs e) => SafetyExecute(() => ImageActualize(ImageActualizeAction.LOAD, txtSymbolPath.Text));

        void BtnDeleteRecognizeImage_Click(object sender, EventArgs e) => SafetyExecute(() =>
        {
            string recogPath = _recognizeProcessorStorage.SavedRecognizePath;

            if (string.IsNullOrEmpty(recogPath))
                return;

            DeleteFile(recogPath);

            btnSaveRecognizeImage.Enabled = true;
            btnDeleteRecognizeImage.Enabled = false;
        });

        void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (DirectoryNotFoundException ex)
            {
                WriteLogMessage($@"{nameof(DeleteFile)}: {ex.Message}");
            }
        }

        void TxtWord_TextChanged(object sender, EventArgs e) => SafetyExecute(RedoRecognizeImage);

        /// <summary>
        ///     Вызывается во время первого отображения формы.
        ///     Производит инициализацию.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_Shown(object sender, EventArgs e)
        {
            try
            {
                _savedRecognizeQuery = txtWord.Text;
                ImageActualize(ImageActualizeAction.REFRESH);
                pbDraw.Refresh();
                _btmSavedRecognizeCopy = RecognizeBitmapCopy;

                InitializeUserInterface();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}