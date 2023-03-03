﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DynamicMosaic;
using DynamicParser;
using Microsoft.VisualBasic.FileIO;
using Processor = DynamicParser.Processor;
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
            LOADDIRECTLY,
            REFRESH
        }

        void ImageActualize(Processor loadingProcessor)
        {
            if (loadingProcessor == null)
                return;

            ImageActualize(ImageActualizeAction.LOADDIRECTLY, null, loadingProcessor);
        }

        /// <summary>
        ///     Предназначен для инициализации структур, отвечающих за вывод создаваемого изображения на экран.
        ///     Если предыдущее изображение присутствовало, то оно переносится на вновь созданное.
        ///     Если путь к файлу исходного изображения отсутствует, создаётся новое изображение.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="btmPath">Путь к файлу исходного изображения.</param>
        /// <param name="loadingProcessor"></param>
        void ImageActualize(ImageActualizeAction action, string btmPath = null, Processor loadingProcessor = null)
        {
            void CommonMethod()
            {
                _grRecognizeImageGraphics?.Dispose();
                _grRecognizeImageGraphics = Graphics.FromImage(_btmRecognizeImage);

                pbDraw.Image = _btmRecognizeImage;
            }

            string tag;
            Bitmap btmAddingProcessor;
            int? countMain = null;

            switch (action)
            {
                case ImageActualizeAction.NEXT:
                    {
                        if (!IsQueryChanged)
                            _currentRecognizeProcIndex++;

                        (Processor processor, string _, int count) =
                            _recognizeProcessorStorage.GetFirstProcessor(ref _currentRecognizeProcIndex);

                        countMain = count;

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
                        if (!IsQueryChanged)
                            _currentRecognizeProcIndex--;

                        (Processor processor, string _, int count) =
                            _recognizeProcessorStorage.GetLatestProcessor(ref _currentRecognizeProcIndex);

                        countMain = count;

                        if (processor == null || count < 1)
                            return;

                        tag = processor.Tag;
                        btmAddingProcessor = ImageRect.GetBitmap(processor);
                    }
                    break;
                case ImageActualizeAction.LOAD:
                    {
                        Processor recogProcessor = _recognizeProcessorStorage.AddProcessor(btmPath).Single(tp => tp != null);

                        tag = recogProcessor.Tag;

                        btmAddingProcessor = ImageRect.GetBitmap(recogProcessor);
                    }
                    break;
                case ImageActualizeAction.LOADDIRECTLY:
                    {
                        if (btmPath != null)
                            throw new ArgumentOutOfRangeException(nameof(btmPath), btmPath, @"В этом случае путь не должен быть указан.");

                        if (loadingProcessor == null)
                            throw new ArgumentNullException(nameof(loadingProcessor), $@"{nameof(ImageActualize)}: {nameof(loadingProcessor)} = null.");

                        tag = loadingProcessor.Tag;

                        btmAddingProcessor = ImageRect.GetBitmap(loadingProcessor);
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
                            _grRecognizeImageGraphics.Clear(DefaultColor);

                        btnSaveRecognizeImage.Enabled = !string.IsNullOrEmpty(txtWord.Text) && IsQueryChanged;
                        btnClearImage.Enabled = IsPainting;
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, @"Указано некорректное действие при загрузке изображения.");
            }

            try
            {
                _btmRecognizeImage?.Dispose();
                _btmRecognizeImage = btmAddingProcessor;

                if (action != ImageActualizeAction.LOAD || _recognizeProcessorStorage.IsWorkingPath(btmPath))
                {
                    _btmSavedRecognizeCopy = RecognizeBitmapCopy;
                    _savedRecognizeQuery = tag;
                }

                txtWord.Text = tag;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DrawFieldFrame(pbDraw, _grpSourceImageGraphics);
            pbDraw.Width = _btmRecognizeImage.Width;
            btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
            btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;

            if (countMain != null)
                SetRedoRecognizeImage(countMain.Value);
            else
                SetRedoRecognizeImage();

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

                return !_recognizeProcessorStorage.IsSelectedOne || txtWord.Text != _savedRecognizeQuery || !CompareBitmaps(_btmRecognizeImage, _btmSavedRecognizeCopy);
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
        void PbDraw_MouseDown(object sender, MouseEventArgs e) => SafeExecute(() =>
        {
            _draw = true;
            DrawPoint(e.X, e.Y, e.Button);

            switch (e.Button)
            {
                case MouseButtons.Left:
                    btnClearImage.Enabled = true;
                    break;
            }
        });

        /// <summary>
        ///     Расширяет область рисования распознаваемого изображения <see cref="pbDraw" /> до максимального размера по
        ///     <see cref="Control.Width" />.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnWide_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                pbDraw.Width += _widthStep;
                btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
                btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
                ImageActualize(ImageActualizeAction.REFRESH);
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });

            SafeExecute(SetRedoRecognizeImage);
        }

        void DrawFieldFrame(Control ctl, Graphics g, bool draw = false) => g.DrawRectangle(draw ? ImageFramePen : _imageFrameResetPen, ctl.Location.X - 1, ctl.Location.Y - 1, ctl.Width + 1, ctl.Height + 1);

        /// <summary>
        ///     Сужает область рисования распознаваемого изображения <see cref="pbDraw" /> до минимального размера по
        ///     <see cref="Control.Width" />.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnNarrow_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                DrawFieldFrame(pbDraw, _grpSourceImageGraphics);
                pbDraw.Width -= _widthStep;
                btnWide.Enabled = pbDraw.Width < pbDraw.MaximumSize.Width;
                btnNarrow.Enabled = pbDraw.Width > pbDraw.MinimumSize.Width;
                ImageActualize(ImageActualizeAction.REFRESH);
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });

            SafeExecute(SetRedoRecognizeImage);
        }

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
        void BtnImageNext_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            _currentImageIndex++;
            ShowCurrentImage(_imagesProcessorStorage.GetFirstProcessor(ref _currentImageIndex));
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Предыдущий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImagePrev_Click(object sender, EventArgs e) => SafeExecute(() =>
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
        void BtnImageDelete_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            (string path, bool isExists) = _imagesProcessorStorage.IsLastPathExists();

            if (isExists)
                DeleteFile(path);
        });

        /// <summary>
        ///     Вызывается по нажатию кнопки "Создать образ".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageCreate_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            using (FrmSymbol fs = new FrmSymbol(_imagesProcessorStorage))
                fs.ShowDialog();
        });

        void SavedPathInfoActualize(int count, string recogPath)
        {
            bool changed = IsQueryChanged;

            if (count > 0)
            {
                int lpi = _recognizeProcessorStorage.LastProcessorIndex;
                int lastProcessorIndex = lpi < 0 ? _currentRecognizeProcIndex : lpi;

                txtRecogNumber.Text = changed ? string.Empty : unchecked(lastProcessorIndex + 1).ToString();
                txtRecogCount.Text = count.ToString();

                _currentRecognizeProcIndex = lastProcessorIndex;
            }
            else
            {
                txtRecogNumber.Text = string.Empty;
                txtRecogCount.Text = string.Empty;
            }

            txtRecogPath.Text = changed ? string.Empty : recogPath;

            btnSaveRecognizeImage.Enabled = changed && !string.IsNullOrEmpty(txtWord.Text);
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
        void RefreshImagesCount() => SafeExecute(() =>
        {
            bool needInitImage = _needInitImage;

            int imageCount = ShowCurrentImage(_imagesProcessorStorage.GetFirstProcessor(ref _currentImageIndex, _imagesProcessorStorage.IsSelectedOne && !needInitImage));

            _needInitImage = imageCount <= 0;

            btnImageUpToQueries.Enabled = btnImageDelete.Enabled = ButtonsEnabled && imageCount > 0;
            btnImageNext.Enabled = imageCount > 1;
            btnImagePrev.Enabled = imageCount > 1;
            txtImagesNumber.Enabled = imageCount > 0;
            txtImagesCount.Enabled = imageCount > 0;
            txtSymbolPath.Enabled = imageCount > 0;
            pbBrowse.Enabled = imageCount > 0;
            lblImagesCount.Enabled = imageCount > 0;

            bool needInitRecognizeImage = _needInitRecognizeImage;

            (Processor recogProcessor, string recogPath, int recogCount) = _recognizeProcessorStorage.GetFirstProcessor(ref _currentRecognizeProcIndex, !needInitRecognizeImage);

            _needInitRecognizeImage = recogCount <= 0;

            bool isQueryChanged = IsQueryChanged;

            switch (needInitRecognizeImage)
            {
                case true when recogCount > 0 && !isQueryChanged:

                    ImageActualize(recogProcessor);

                    btnRecogNext.Enabled = recogCount > 1;
                    btnRecogPrev.Enabled = recogCount > 1;
                    txtRecogNumber.Enabled = true;
                    txtRecogCount.Enabled = true;
                    txtRecogPath.Enabled = true;
                    lblSourceCount.Enabled = true;
                    btnDeleteRecognizeImage.Enabled = true;

                    SavedPathInfoActualize(recogCount, recogPath);

                    return;

                case false when recogCount > 0 && !isQueryChanged:

                    ImageActualize(recogProcessor);

                    break;
            }

            btnRecogNext.Enabled = recogCount > 1 || (recogCount > 0 && isQueryChanged);
            btnRecogPrev.Enabled = recogCount > 1;
            txtRecogNumber.Enabled = recogCount > 0;
            txtRecogCount.Enabled = recogCount > 0;
            txtRecogPath.Enabled = recogCount > 0;
            lblSourceCount.Enabled = recogCount > 0;
            btnDeleteRecognizeImage.Enabled = !isQueryChanged && !_recognizeProcessorStorage.IsEmpty;

            SavedPathInfoActualize(recogCount, recogPath);
        }, true);

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

            Thread thread = new Thread(() => SafeExecute(() =>
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

                            SafeExecute(() =>
                            {
                                btnRecognizeImage.Image = _imgSearchDefault;
                                btnRecognizeImage.Text = string.Empty;
                                ButtonsEnabled = true;
                                RefreshImagesCount();
                                txtWord.Select();

                                if (initializeUserInterface)
                                    return;

                                try
                                {
                                    initializeUserInterface = true;

                                    _imagesProcessorStorage.CreateFolder();
                                    _recognizeProcessorStorage.CreateFolder();

                                    CreateImageWatcher();
                                    CreateRecognizeWatcher();
                                    CreateWorkDirWatcher();

                                    CreateFileRefreshThread();

                                    ChangedThreadFunction(WatcherChangeTypes.Created, SearchImagesPath, _imagesProcessorStorage, SourceChanged.IMAGES);
                                    ChangedThreadFunction(WatcherChangeTypes.Created, RecognizeImagesPath, _recognizeProcessorStorage, SourceChanged.RECOGNIZE);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    Process.GetCurrentProcess().Kill();
                                }
                            }, true);

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

                    void OutCurrentStatus(string strPreparing, string strLoading) => SafeExecute(() =>
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
                    }, true);

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

        static void ResetLogWriteMessage()
        {
            lock (ErrorMessageLocker)
                _errorMessageIsShowed = false;
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Найти".
        ///     Находит заданные изображения и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnRecognizeImage_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            ResetLogWriteMessage();

            if (!StopRecognize())
                throw new Exception(@"Ошибка при остановке потока перед запуском новой сессии поиска.");

            if (IsQueryChanged)
            {
                MessageBox.Show(this, NeedSaveQuery, @"Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ButtonsEnabled = false;
            lstHistory.SelectedIndex = -1;

            CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            CurrentUndoRedoWord = txtWord.Text;

            Thread t = new Thread(RecognizerFunction)
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true,
                Name = "Recognizer"
            };

            t.Start();

            RecognizerThread = t;
        });

        void RecognizerFunction()
        {
            SafeExecute(() =>
            {
                try
                {
                    _recogPreparing.Set();

                    DynamicReflex recognizer = Recognizer;

                    if (recognizer == null)
                    {
                        List<Processor> processors = _imagesProcessorStorage.UniqueElements.ToList();

                        if (!processors.Any())
                        {
                            ErrorMessageInOtherThread(@"Образы для поиска отсутствуют. Создайте хотя бы два.");
                            return;
                        }

                        recognizer = new DynamicReflex(new ProcessorContainer(processors));

                        Recognizer = recognizer;

                        OutHistory(null, recognizer.Processors.ToArray(), @"start");
                    }

                    (Processor, string)[] query = _recognizeProcessorStorage.Elements.Select(t => (t, t.Tag)).ToArray();

                    if (!query.Any())
                    {
                        ErrorMessageInOtherThread(@"Поисковые запросы отсутствуют. Создайте хотя бы один.");
                        return;
                    }

                    _recognizerActivity.Set();

                    try
                    {
                        _stwRecognize.Restart();

                        bool? result;

                        try
                        {
                            result = recognizer.FindRelation(query);
                        }
                        finally
                        {
                            _stwRecognize.Stop();
                        }

                        OutHistory(result, recognizer.Processors.ToArray());
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch
                    {
                        OutHistory(null, recognizer.Processors.ToArray(), @"EXCP");
                        throw;
                    }
                }
                catch (ThreadAbortException)
                {
                    ResetAbort();
                }
                finally
                {
                    _recogPreparing.Reset();
                    _recognizerActivity.Reset();

                    RecognizerThread = null;
                }
            });
        }

        void OutHistory(bool? result, Processor[] ps, string comment = null) => SafeExecute(() =>
        {
            string GetSystemName(int position)
            {
                string strCount = string.IsNullOrEmpty(comment)
                    ? position < 10 ? ps.Length > 9999 ? "∞" : ps.Length.ToString() :
                    ps.Length > 999 ? "∞" : ps.Length.ToString()
                    : comment;

                char r = result.HasValue ? result == true ? 'T' : 'F' : ' ';

                return $@"№{position} {r} {DateTime.Now:HH:mm:ss} ({strCount})";
            }

            if (result.HasValue)
                CurrentUndoRedoState = result == true ? UndoRedoState.SUCCESS : UndoRedoState.ERROR;

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
        }, true);

        /// <summary>
        ///     Осуществляет ввод искомого слова по нажатии клавиши Enter.
        ///     Предназначен для переопределения функции отката (CTRL + Z).
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_KeyDown(object sender, KeyEventArgs e) => SafeExecute(() =>
        {
            switch (e.KeyCode)
            {
                case Keys.Enter when txtWord.Focused:
                    BtnRecognizeImage_Click(btnRecognizeImage, EventArgs.Empty);
                    break;
                case Keys.Z when e.Control && txtWord.Focused:
                    if (CurrentUndoRedoState == UndoRedoState.UNKNOWN)
                        return;

                    txtWord.Text = CurrentUndoRedoWord;
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
                case Keys.RButton | Keys.FinalMode:
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

        void SetRedoRecognizeImage() => SetRedoRecognizeImage(_recognizeProcessorStorage.Count, true);

        void SetRedoRecognizeImage(bool needPaintCheck) => SetRedoRecognizeImage(_recognizeProcessorStorage.Count, needPaintCheck);

        void SetRedoRecognizeImage(int count) => SetRedoRecognizeImage(count, true);

        void SetRedoRecognizeImage(int count, bool needPaintCheck)
        {
            bool changed = IsQueryChanged;

            btnSaveRecognizeImage.Enabled = changed && !string.IsNullOrEmpty(txtWord.Text);

            btnRecogNext.Enabled = (changed && count > 0) || count > 1;
            btnDeleteRecognizeImage.Enabled = !changed && count > 0;

            SavedPathInfoActualize(count, _recognizeProcessorStorage.LastRecognizePath);

            if (!needPaintCheck)
                return;

            btnClearImage.Enabled = IsPainting;
        }

        void DrawStop() => SafeExecute(() =>
        {
            _draw = false;

            SetRedoRecognizeImage();
        });

        /// <summary>
        ///     Отвечает за отрисовку рисунка, создаваемого пользователем.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbDraw_MouseMove(object sender, MouseEventArgs e) => SafeExecute(() =>
        {
            if (_draw)
                DrawPoint(e.X, e.Y, e.Button);
        });

        /// <summary>
        ///     Рисует точку в указанном месте на <see cref="pbDraw" /> с применением <see cref="_grRecognizeImageGraphics" />.
        /// </summary>
        /// <param name="x">Координата Х.</param>
        /// <param name="y">Координата Y.</param>
        /// <param name="button">Данные о нажатой кнопке мыши.</param>
        void DrawPoint(int x, int y, MouseButtons button)
        {
            SafeExecute(() =>
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (button)
                {
                    case MouseButtons.Left:
                        _grRecognizeImageGraphics.DrawRectangle(BlackPen, new Rectangle(x, y, 1, 1));
                        break;
                    case MouseButtons.Right:
                        _grRecognizeImageGraphics.DrawRectangle(WhitePen, new Rectangle(x, y, 1, 1));
                        break;
                }
            });

            SafeExecute(() => pbDraw.Refresh());
        }

        /// <summary>
        ///     Очищает поле рисования исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnClearImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                _grRecognizeImageGraphics.Clear(DefaultColor);
                SetRedoRecognizeImage();
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });

            SafeExecute(() => pbDraw.Refresh());
        }

        /// <summary>
        ///     Обрабатывает событие нажатия кнопки сохранения созданного изображения для распознавания.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnSaveRecognizeImage_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            string tag = txtWord.Text;

            if (string.IsNullOrWhiteSpace(tag))
            {
                MessageBox.Show(this, SaveImageQueryError, @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (InvalidCharSet.Overlaps(tag))
            {
                MessageBox.Show(this, QueryErrorSymbols, @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            txtRecogPath.Text = _recognizeProcessorStorage.SaveToFile(new Processor(_btmRecognizeImage, tag));

            _btmSavedRecognizeCopy = RecognizeBitmapCopy;
            _savedRecognizeQuery = tag;
        });

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки загрузки созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnLoadRecognizeImage_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            if (dlgOpenImage.ShowDialog(this) != DialogResult.OK)
                return;

            ImageActualize(ImageActualizeAction.LOAD, dlgOpenImage.FileName);
            CurrentUndoRedoState = UndoRedoState.UNKNOWN;
        });

        /// <summary>
        ///     Обрабатывает событие выбора исследуемой системы.
        ///     Отображает содержимое системы в окне "Содержимое Reflex".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void LstResults_SelectedIndexChanged(object sender, EventArgs e) => SafeExecute(ChangeSystemSelectedIndex);

        void ChangeSystemSelectedIndex()
        {
            void ResetConSymbol()
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
                lblConSymbolCount.Enabled = false;
                lblConSymbolEqual.Enabled = false;
            }

            if (lstHistory.SelectedIndex < 0)
            {
                ResetConSymbol();
                return;
            }

            try
            {
                (Processor[] processors, int reflexMapIndex, string _) = SelectedResult;

                bool anyProcs = processors != null && processors.Any();

                Processor p = anyProcs ? processors[reflexMapIndex] : null;

                txtConSymbolNumber.Text = anyProcs ? checked(reflexMapIndex + 1).ToString() : string.Empty;
                txtConSymbolCount.Text = anyProcs ? processors.Length.ToString() : string.Empty;

                if (p != null)
                {
                    txtConSymbolTag.Text = p.Tag;
                    txtConSymbolTag.Enabled = true;

                    if (p.Width == pbConSymbol.Width && p.Height == pbConSymbol.Height)
                    {
                        pbConSymbol.Image = ImageRect.GetBitmap(p);
                        pbConSymbol.Enabled = true;
                    }
                    else
                    {
                        pbConSymbol.Image = Resources.IncorrSize;
                        pbConSymbol.Enabled = false;
                    }
                }
                else
                {
                    txtConSymbolTag.Text = string.Empty;
                    txtConSymbolTag.Enabled = false;

                    pbConSymbol.Image = new Bitmap(pbConSymbol.Width, pbConSymbol.Height);
                    pbConSymbol.Enabled = false;
                }

                txtConSymbolNumber.Enabled = anyProcs && p != null;
                txtConSymbolCount.Enabled = anyProcs;
                btnConNext.Enabled = anyProcs && processors.Length > 1;
                btnConPrevious.Enabled = anyProcs && processors.Length > 1;
                btnConSaveImage.Enabled = anyProcs && p != null;
                btnConSaveAllImages.Enabled = anyProcs && processors.Length > 1;
                lblConSymbolCount.Enabled = anyProcs;
                lblConSymbolEqual.Enabled = anyProcs;
            }
            catch
            {
                ResetConSymbol();
                throw;
            }
        }

        /// <summary>
        ///     Обрабатывает событие выбора следующей карты, рассматриваемой в выбранной системе.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConNext_Click(object sender, EventArgs e) => SafeExecute(() =>
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
        void BtnConPrevious_Click(object sender, EventArgs e) => SafeExecute(() =>
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
        void BtnConSaveImage_Click(object sender, EventArgs e) => SafeExecute(() => _imagesProcessorStorage.SaveToFile(SelectedResult.systemName, new[] { SelectedResult.processors[SelectedResult.reflexMapIndex] }));

        /// <summary>
        ///     Сохраняет все карты <see cref="Processor" /> выбранной системы <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveAllImages_Click(object sender, EventArgs e) => SafeExecute(() => _imagesProcessorStorage.SaveToFile(SelectedResult.systemName, SelectedResult.processors));

        /// <summary>
        ///     Обрабатывает событие завершения работы программы.
        ///     Закрывает файлы, потоки.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_FormClosing(object sender, FormClosingEventArgs e) => SafeExecute(() =>
        {
            try
            {
                DisposeWorkDirWatcher();
                DisposeRecognizeWatcher();
                DisposeImageWatcher();

                if (!StopRecognize())
                    WriteLogMessage($@"{nameof(FrmExample_FormClosing)}: Ошибка остановки поиска, в процессе завершения работы программы.");

                _stopBackground.Set();

                ConcurrentProcessorStorage.LongOperationsAllowed = false;

                _fileRefreshThread?.Join();
                _userInterfaceThread?.Join();

                _grRecognizeImageGraphics?.Dispose();
                _grpResultsGraphics?.Dispose();
                _grpImagesGraphics?.Dispose();
                _grpSourceImageGraphics?.Dispose();

                BlackPen.Dispose();
                ImageFramePen.Dispose();
                WhitePen.Dispose();
                _imageFrameResetPen?.Dispose();

                DisposeImage(pbDraw);
                DisposeImage(pbBrowse);
                DisposeImage(pbConSymbol);
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        });

        public static void DisposeImage(PictureBox pb)
        {
            if (pb == null)
                throw new ArgumentNullException(nameof(pb), $@"{nameof(DisposeImage)}: {nameof(pb)} = null.");

            Image image = pb.Image;

            if (image == null)
                return;

            pb.Image = null;

            image.Dispose();
        }

        /// <summary>
        ///     Служит для отображения имени файла карты при изменении выбранной карты.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TextBoxTextChanged(object sender, EventArgs e) => SafeExecute(() =>
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

        void LstResults_DrawItem(object sender, DrawItemEventArgs e) => SafeExecute(() =>
        {
            if (e.Index < 0)
                return;

            TextRenderer.DrawText(e.Graphics, lstHistory.Items[e.Index].ToString(), e.Font,
                e.Bounds, e.ForeColor, e.BackColor, TextFormatFlags.HorizontalCenter);
        });

        void BtnNextRecogImage_Click(object sender, EventArgs e) => SafeExecute(() => ImageActualize(ImageActualizeAction.NEXT));

        void BtnPrevRecogImage_Click(object sender, EventArgs e) => SafeExecute(() => ImageActualize(ImageActualizeAction.PREV));

        void BtnImageUpToQueries_Click(object sender, EventArgs e) => SafeExecute(() => ImageActualize(ImageActualizeAction.LOAD, txtSymbolPath.Text));

        void BtnDeleteRecognizeImage_Click(object sender, EventArgs e) => SafeExecute(() =>
        {
            (string path, bool isExists) = _recognizeProcessorStorage.IsLastPathExists();

            if (isExists)
                DeleteFile(path);
        });

        void DeleteFile(string path)
        {
            try
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
            catch (DirectoryNotFoundException ex)
            {
                WriteLogMessage($@"{nameof(DeleteFile)}: {ex.Message}");
            }
        }

        void TxtWord_TextChanged(object sender, EventArgs e) => SafeExecute(() =>
        {
            CurrentUndoRedoWord = txtWord.Text;

            SetRedoRecognizeImage(false);
        });

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
                _imageFrameResetPen = new Pen(grpSourceImage.BackColor);

                _grpResultsGraphics = Graphics.FromHwnd(grpResults.Handle);
                _grpImagesGraphics = Graphics.FromHwnd(grpImages.Handle);
                _grpSourceImageGraphics = Graphics.FromHwnd(grpSourceImage.Handle);

                ImageActualize(ImageActualizeAction.REFRESH);
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

        void PbDraw_Paint(object sender, PaintEventArgs e) => SafeExecute(() => DrawFieldFrame(pbDraw, _grpSourceImageGraphics, true));

        void PbConSymbol_Paint(object sender, PaintEventArgs e) => SafeExecute(() => DrawFieldFrame(pbConSymbol, _grpResultsGraphics, true));

        void PbBrowse_Paint(object sender, PaintEventArgs e) => SafeExecute(() => DrawFieldFrame(pbBrowse, _grpImagesGraphics, true));
    }
}