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
using Microsoft.VisualBasic.FileIO;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Класс основной формы приложения.
    /// </summary>
    internal sealed partial class FrmExample : Form
    {
        /// <summary>
        /// Возвращает значение <see langword="true"/>, если поисковый запрос изменён.
        /// </summary>
        /// <remarks>
        /// Для того, чтобы запрос был сохранён, необходимо сохранить его в файл, а изображение на экране не подвергать изменению.
        /// Таким образом, изображение <see cref="_btmRecognizeImage"/> должно совпадать с <see cref="_btmSavedRecognizeCopy"/>, а текущий поисковый запрос <see cref="txtRecogQueryWord"/> должен быть равен <see cref="_savedRecognizeQuery"/>.
        /// Во всех остальных случаях запрос является изменённым.
        /// Свойство можно использовать только в том потоке, в котором была создана форма <see cref="FrmExample"/>.
        /// </remarks>
        /// <seealso cref="FrmExample"/>
        /// <seealso cref="_btmRecognizeImage"/>
        /// <seealso cref="_btmSavedRecognizeCopy"/>
        /// <seealso cref="_savedRecognizeQuery"/>
        bool IsQueryChanged
        {
            get
            {
                if (_btmSavedRecognizeCopy == null)
                    return false;

                return !_recognizeProcessorStorage.IsSelectedOne || txtRecogQueryWord.Text != _savedRecognizeQuery ||
                       !CompareBitmaps(_btmRecognizeImage, _btmSavedRecognizeCopy);
            }
        }

        /// <summary>
        /// Возвращает полную копию распознаваемого изображения на текущий момент времени.
        /// </summary>
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
        ///     Отображает указанную распознаваемую карту на экране.
        /// </summary>
        /// <param name="loadingProcessor">Карта, которую требуется отобразить на экране.</param>
        /// <remarks>
        /// Если <paramref name="loadingProcessor"/> равен <see langword="null"/>, вызов будет игнорирован.
        /// Метод использует константу <see cref="ImageActualizeAction.LOADDIRECTLY"/>.
        /// </remarks>
        /// <seealso cref="ImageActualizeAction.LOADDIRECTLY"/>
        void ImageActualize(Processor loadingProcessor)
        {
            if (loadingProcessor == null)
                return;

            ImageActualize(ImageActualizeAction.LOADDIRECTLY, null, loadingProcessor);
        }

        /// <summary>
        ///     Выполняет указанное действие над распознаваемой картой, используя аргументы, необходимые для его выполнения.
        ///     Актуализирует состояние пользовательского интерфейса, предназначенного для создания распознаваемого изображения.
        /// </summary>
        /// <param name="action">Действие, которое необходимо выполнить.</param>
        /// <param name="btmPath">Путь к файлу загружаемого распознаваемого изображения. Необходим только для выполнения действия <see cref="ImageActualizeAction.LOAD"/>.</param>
        /// <param name="loadingProcessor">Необходима для выполнения действия <see cref="ImageActualizeAction.LOADDIRECTLY"/>, иначе - <see langword="null"/>.</param>
        /// <remarks>
        /// Для получения сведений о выполняемых действиях см. <see cref="ImageActualizeAction"/>.
        /// </remarks>
        /// <seealso cref="ImageActualizeAction"/>
        void ImageActualize(ImageActualizeAction action, string btmPath = null, Processor loadingProcessor = null)
        {
            void CommonMethod()
            {
                _grRecognizeImageGraphics?.Dispose();
                _grRecognizeImageGraphics = Graphics.FromImage(_btmRecognizeImage);

                pbRecognizeImageDraw.Image = _btmRecognizeImage;
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
                            btnNextRecog.Enabled = false;
                            txtRecogQueryWord.Select();
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
                        Processor recogProcessor =
                            _recognizeProcessorStorage.AddProcessor(btmPath).Single(tp => tp != null);

                        tag = recogProcessor.Tag;

                        btmAddingProcessor = ImageRect.GetBitmap(recogProcessor);
                    }
                    break;
                case ImageActualizeAction.LOADDIRECTLY:
                    {
                        if (btmPath != null)
                            throw new ArgumentOutOfRangeException(nameof(btmPath), btmPath,
                                $@"В случае {ImageActualizeAction.LOADDIRECTLY} путь не должен быть указан.");

                        if (loadingProcessor == null)
                            throw new ArgumentNullException(nameof(loadingProcessor),
                                $@"{nameof(ImageActualize)}: {nameof(loadingProcessor)} = null.");

                        tag = loadingProcessor.Tag;

                        btmAddingProcessor = ImageRect.GetBitmap(loadingProcessor);
                    }
                    break;
                case ImageActualizeAction.REFRESH:
                    {
                        Bitmap btm = new Bitmap(pbRecognizeImageDraw.Width, pbRecognizeImageDraw.Height);

                        bool needReset = false;

                        if (_btmRecognizeImage != null)
                        {
                            CopyBitmapByWidth(_btmRecognizeImage, btm);
                            _btmRecognizeImage.Dispose();
                        }
                        else
                        {
                            needReset = true;
                        }

                        _btmRecognizeImage = btm;

                        CommonMethod();

                        if (needReset)
                            _grRecognizeImageGraphics.Clear(DefaultColor);

                        btnSaveRecognizeImage.Enabled = IsButtonsEnabled && !string.IsNullOrEmpty(txtRecogQueryWord.Text) && IsQueryChanged;
                        btnClearRecogImage.Enabled = IsButtonsEnabled && IsPainting;
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action,
                        @"Указано некорректное действие при загрузке изображения.");
            }

            try
            {
                _btmRecognizeImage?.Dispose();
                _btmRecognizeImage = btmAddingProcessor;

                if (action != ImageActualizeAction.LOAD || _recognizeProcessorStorage.IsWorkingDirectory(btmPath))
                {
                    _btmSavedRecognizeCopy = RecognizeBitmapCopy;
                    _savedRecognizeQuery = tag;
                }

                _currentUndoRedoWord = tag;
                txtRecogQueryWord.Text = tag;
            }
            catch (Exception ex)
            {
                WriteLogMessage(ex.Message);
                return;
            }

            DrawFieldFrame(pbRecognizeImageDraw, _grpSourceImageGraphics);

            pbRecognizeImageDraw.Width = _btmRecognizeImage.Width;
            btnWide.Enabled = IsButtonsEnabled && pbRecognizeImageDraw.Width < pbRecognizeImageDraw.MaximumSize.Width;
            btnNarrow.Enabled = IsButtonsEnabled && pbRecognizeImageDraw.Width > pbRecognizeImageDraw.MinimumSize.Width;

            if (countMain != null)
                SetRedoRecognizeImage(countMain.Value);
            else
                SetRedoRecognizeImage();

            if (!string.IsNullOrEmpty(txtRecogQueryWord.Text))
                txtRecogQueryWord.Select(txtRecogQueryWord.Text.Length, 0);

            CommonMethod();
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
            {
                gr.Clear(DefaultColor);
            }

            for (int x = 0; x < from.Width && x < to.Width; x++)
                for (int y = 0; y < from.Height; y++)
                    to.SetPixel(x, y, from.GetPixel(x, y));
        }

        /// <summary>
        ///     Вызывается, когда пользователь начинает рисовать распознаваемое изображение.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbRecognizeImageDraw_MouseDown(object sender, MouseEventArgs e)
        {
            SafeExecute(() =>
            {
                _drawAllowed = true;
                DrawPoint(e.X, e.Y, e.Button);

                switch (e.Button)
                {
                    case MouseButtons.Left:
                        btnClearRecogImage.Enabled = true;
                        break;
                }
            });
        }

        /// <summary>
        ///     Расширяет область создания распознаваемого изображения <see cref="pbRecognizeImageDraw" /> до максимального размера по
        ///     <see cref="Control.Width" />.
        ///     Изображение будет считаться изменённым.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnWide_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                pbRecognizeImageDraw.Width += _widthStep;
                btnWide.Enabled = pbRecognizeImageDraw.Width < pbRecognizeImageDraw.MaximumSize.Width;
                btnNarrow.Enabled = pbRecognizeImageDraw.Width > pbRecognizeImageDraw.MinimumSize.Width;
                ImageActualize(ImageActualizeAction.REFRESH);
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });

            SafeExecute(SetRedoRecognizeImage);
        }

        /// <summary>
        /// Рисует рамку (или удаляет её) вокруг указанного элемента управления.
        /// </summary>
        /// <param name="ctl">Необходим для считывания координат, ширины и высоты.</param>
        /// <param name="g">Поверхность для рисования.</param>
        /// <param name="draw">Значение <see langword="true"/> для рисования рамки, в противном случае - для стирания.</param>
        /// <remarks>
        /// Метод использует <see cref="ImageFramePen"/> и <see cref="_imageFrameResetPen"/> для рисования и стирания.
        /// </remarks>
        /// <seealso cref="ImageFramePen"/>
        /// <seealso cref="_imageFrameResetPen"/>
        void DrawFieldFrame(Control ctl, Graphics g, bool draw = false)
        {
            Pen pen = draw ? ImageFramePen : _imageFrameResetPen;
            float width = pen.Width;

            g.DrawRectangle(pen, ctl.Location.X - width, ctl.Location.Y - width, ctl.Width + width, ctl.Height + width);
        }

        /// <summary>
        ///     Сужает область создания распознаваемого изображения <see cref="pbRecognizeImageDraw" /> до минимального размера по
        ///     <see cref="Control.Width" />.
        ///     Изображение будет считаться изменённым.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnNarrow_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                DrawFieldFrame(pbRecognizeImageDraw, _grpSourceImageGraphics);
                pbRecognizeImageDraw.Width -= _widthStep;
                btnWide.Enabled = pbRecognizeImageDraw.Width < pbRecognizeImageDraw.MaximumSize.Width;
                btnNarrow.Enabled = pbRecognizeImageDraw.Width > pbRecognizeImageDraw.MinimumSize.Width;
                ImageActualize(ImageActualizeAction.REFRESH);
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });

            SafeExecute(SetRedoRecognizeImage);
        }

        /// <summary>
        ///     Вызывается при отпускании клавиши мыши над полем создания исходного изображения <see cref="pbRecognizeImageDraw"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbRecognizeImageDraw_MouseUp(object sender, MouseEventArgs e)
        {
            DrawStop();
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Следующий" в искомых образах.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageNext_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                _currentImageIndex++;
                ShowCurrentImage(_imagesProcessorStorage.GetFirstProcessor(ref _currentImageIndex));
            });
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Предыдущий" в искомых образах.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImagePrev_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                _currentImageIndex--;
                ShowCurrentImage(_imagesProcessorStorage.GetLatestProcessor(ref _currentImageIndex));
            });
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Удалить".
        ///     Удаляет выбранный искомый образ в корзину.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageDelete_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                (string path, bool isExists) = _imagesProcessorStorage.IsSelectedPathExists();

                if (isExists)
                    DeleteFile(path);
            });
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Создать образ".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnImageCreate_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                using (FrmSymbol fs = new FrmSymbol(_imagesProcessorStorage))
                {
                    fs.ShowDialog();
                }
            });
        }

        /// <summary>
        /// Актуализирует сведения о пути к выбранной распознаваемой карте и их количестве, в интерфейсе пользователя.
        /// </summary>
        /// <param name="count">Количество карт в коллекции <see cref="_recognizeProcessorStorage"/>.</param>
        /// <param name="recogPath">Путь к текущей (выбранной) распознаваемой карте.</param>
        /// <remarks>
        /// Учитывает случай, если выбранная карта была изменена.
        /// </remarks>
        void SavedPathInfoActualize(int count, string recogPath)
        {
            bool changed = IsQueryChanged;

            if (count > 0)
            {
                int lpi = _recognizeProcessorStorage.SelectedIndex;
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

            btnSaveRecognizeImage.Enabled = changed && IsButtonsEnabled && !string.IsNullOrEmpty(txtRecogQueryWord.Text);
        }

        /// <summary>
        /// Отображает указанную искомую карту на экране, указывая её индекс (начиная с единицы), общее количество карт в коллекции <see cref="_imagesProcessorStorage"/>, и путь к ней.
        /// </summary>
        /// <param name="t">
        /// 1) Карта, которую необходимо отобразить на экране.
        /// 2) Путь к этой карте.
        /// 3) Общее количество карт в коллекции <see cref="_imagesProcessorStorage"/>.
        /// </param>
        /// <returns>Возвращает количество карт из параметра <paramref name="t"/>.</returns>
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
                pbImage.Image = ImageRect.GetBitmap(t.imageProcessor);
                txtSymbolPath.Text = t.imagePath;
            }
            else
            {
                pbImage.Image = new Bitmap(pbImage.Width, pbImage.Height);
                txtSymbolPath.Text = _unknownSymbolName;
            }

            return t.imageCount;
        }

        /// <summary>
        ///    Актуализирует состояние пользовательского интерфейса.
        /// </summary>
        /// <remarks>
        ///     Предназначен для работы в фоновом потоке.
        /// </remarks>
        void RefreshImagesCount()
        {
            SafeExecute(() =>
            {
                bool needInitImage = _needInitImage;

                int imageCount = ShowCurrentImage(_imagesProcessorStorage.GetFirstProcessor(ref _currentImageIndex,
                    _imagesProcessorStorage.IsSelectedOne && !needInitImage));

                _needInitImage = imageCount <= 0;

                btnImageUpToQueries.Enabled = btnImageDelete.Enabled = IsButtonsEnabled && imageCount > 0;
                btnNextImage.Enabled = imageCount > 1;
                btnPrevImage.Enabled = imageCount > 1;
                txtImagesNumber.Enabled = imageCount > 0;
                txtImagesCount.Enabled = imageCount > 0;
                txtSymbolPath.Enabled = imageCount > 0;
                pbImage.Enabled = imageCount > 0;
                lblImagesCount.Enabled = imageCount > 0;

                bool needInitRecognizeImage = _needInitRecognizeImage;

                (Processor recogProcessor, string recogPath, int recogCount) =
                    _recognizeProcessorStorage.GetFirstProcessor(ref _currentRecognizeProcIndex,
                        !needInitRecognizeImage);

                _needInitRecognizeImage = recogCount <= 0;

                bool isQueryChanged = IsQueryChanged;

                switch (needInitRecognizeImage)
                {
                    case true when recogCount > 0 && IsButtonsEnabled && !isQueryChanged:

                        ImageActualize(recogProcessor);

                        btnNextRecog.Enabled = recogCount > 1;
                        btnPrevRecog.Enabled = recogCount > 1;
                        txtRecogNumber.Enabled = true;
                        txtRecogCount.Enabled = true;
                        txtRecogPath.Enabled = true;
                        lblSourceCount.Enabled = true;
                        btnDeleteRecognizeImage.Enabled = true;

                        SavedPathInfoActualize(recogCount, recogPath);

                        return;

                    case false when recogCount > 0 && IsButtonsEnabled && !isQueryChanged:

                        ImageActualize(recogProcessor);

                        break;
                }

                btnNextRecog.Enabled = recogCount > 1 || (recogCount > 0 && isQueryChanged);
                btnPrevRecog.Enabled = recogCount > 1;
                txtRecogNumber.Enabled = recogCount > 0;
                txtRecogCount.Enabled = recogCount > 0;
                txtRecogPath.Enabled = recogCount > 0;
                lblSourceCount.Enabled = recogCount > 0;
                btnDeleteRecognizeImage.Enabled = !isQueryChanged && IsButtonsEnabled && !_recognizeProcessorStorage.IsEmpty;

                SavedPathInfoActualize(recogCount, recogPath);
            }, true);
        }

        /// <summary>
        ///     Создаёт и запускает новый поток (<see cref="_userInterfaceActualizeThread" />), отвечающий за инициализацию и актуализацию состояния пользовательского интерфейса в реальном времени.
        ///     Созданный поток называется WaitThread и находится в состоянии <see cref="System.Threading.ThreadState.Running" />, <see cref="System.Threading.ThreadState.Background" />.
        /// </summary>
        /// <remarks>
        ///     В случае, если поток уже был создан, метод выбросит исключение <see cref="InvalidOperationException" />.
        /// </remarks>
        /// <exception cref="InvalidOperationException"/>
        void InitializeUserInterface()
        {
            if (_userInterfaceActualizeThread != null)
                throw new InvalidOperationException(
                    $@"Попытка вызвать метод {nameof(InitializeUserInterface)} в то время, когда поток {nameof(_userInterfaceActualizeThread)} уже существует.");

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
                                IsButtonsEnabled = true;
                                RefreshImagesCount();
                                txtRecogQueryWord.Select();

                                if (initializeUserInterface)
                                    return;

                                try
                                {
                                    initializeUserInterface = true;

                                    _imagesProcessorStorage.CreateWorkingDirectory();
                                    _recognizeProcessorStorage.CreateWorkingDirectory();

                                    CreateImageWatcher();
                                    CreateRecognizeWatcher();
                                    CreateWorkDirWatcher();

                                    CreateFileRefreshThread();

                                    ChangedThreadFunction(WatcherChangeTypes.Created, SearchImagesPath,
                                        _imagesProcessorStorage, SourceChanged.IMAGES);
                                    ChangedThreadFunction(WatcherChangeTypes.Created, RecognizeImagesPath,
                                        _recognizeProcessorStorage, SourceChanged.RECOGNIZE);
                                }
                                catch (Exception ex)
                                {
                                    WriteLogMessage($@"{ex.Message}{Environment.NewLine}Программа будет завершена.");
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

                    void OutCurrentStatus(string strPreparing, string strLoading, string strStopping)
                    {
                        SafeExecute(() =>
                        {
                            string CreateTimeString(TimeSpan ts)
                            {
                                return $@"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
                            }

                            switch (eventIndex)
                            {
                                case 1:
                                    btnRecognizeImage.Image = null;
                                    btnRecognizeImage.Text = StopperThread != null ? strStopping : CreateTimeString(_stwRecognize.Elapsed);
                                    return;

                                case 2:
                                    btnRecognizeImage.Image = null;
                                    btnRecognizeImage.Text = strPreparing;
                                    return;

                                case 3:
                                    btnRecognizeImage.Image = null;
                                    btnRecognizeImage.Text = strLoading;
                                    return;

                                default:
                                    throw new Exception($@"Этот ID произошедшего события недопустим ({eventIndex}).");
                            }
                        }, true);
                    }

                    switch (k)
                    {
                        case 0:
                            OutCurrentStatus(StrPreparing0, StrLoading0, StrStopping0);
                            Thread.Sleep(100);
                            break;
                        case 1:
                            OutCurrentStatus(StrPreparing1, StrLoading1, StrStopping1);
                            Thread.Sleep(100);
                            break;
                        case 2:
                            OutCurrentStatus(StrPreparing2, StrLoading2, StrStopping2);
                            Thread.Sleep(100);
                            break;
                        case 3:
                            OutCurrentStatus(StrPreparing3, StrLoading3, StrStopping3);
                            Thread.Sleep(100);
                            k = -1;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(k), k,
                                @"Некорректное значение индикатора для отображения статуса.");
                    }
                }
            }))
            {
                IsBackground = true,
                Name = "WaitThread"
            };

            _userInterfaceActualizeThread = thread;

            thread.Start();
        }

        /// <summary>
        /// Сбрасывает значение переменной <see cref="_errorMessageIsShowed"/>.
        /// </summary>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Синхронизирован с помощью <see cref="_commonLocker"/>.
        /// </remarks>
        void ResetLogWriteMessage()
        {
            lock (_commonLocker)
            {
                _errorMessageIsShowed = false;
            }
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Найти".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Выполняет (или останавливает, в случае, если запрос уже выполнялся) текущий поисковый запрос, и выводит результат на форму.
        /// Метод выполняет запрос в отдельном потоке (<see cref="RecognizerThread"/>), который называется Recognizer.
        /// </remarks>
        void BtnRecognizeImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                ResetLogWriteMessage();

                if (StopRecognize() != null)
                    return;

                if (IsQueryChanged)
                {
                    MessageBox.Show(this, NeedSaveQuery, @"Уведомление", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                IsButtonsEnabled = false;
                lstHistory.SelectedIndex = -1;

                CurrentUndoRedoState = UndoRedoState.UNKNOWN;

                GC.Collect();

                Thread t = new Thread(RecognizerFunction)
                {
                    Priority = ThreadPriority.Highest,
                    IsBackground = true,
                    Name = "Recognizer"
                };

                t.Start();

                RecognizerThread = t;
            });
        }

        /// <summary>
        /// Выполняет все требуемые операции для подготовки и выполнения поискового запроса: инициализацию и актвацию таймера для отсчёта затраченного времени на его выполнение.
        /// Выводит статус на экран, включая историю изменений экземпляра тестируемого класса (<see cref="Recognizer"/>).
        /// Значения статусов следующие:
        /// 1) "start" - экземпляр тестируемого класса (<see cref="Recognizer"/>) был создан.
        /// 2) В случае выполнения операции в штатном режиме, вместо текстовой пометки будет отображено количество элементов в тестируемой системе на текущий момент.
        /// 3) "EXCP" - во время выполнения поискового запроса произошла ошибка (нештатная ситуация).
        /// Функция предназначена для исполнения в ОТДЕЛЬНОМ потоке.
        /// </summary>
        /// <seealso cref="Recognizer"/>
        void RecognizerFunction()
        {
            SafeExecute(() =>
            {
                try
                {
                    _recogPreparing.Set();

                    (Processor, string)[] queries = _recognizeProcessorStorage.Elements.Select(t => (t, t.Tag)).ToArray();

                    if (!queries.Any())
                    {
                        ErrorMessageInOtherThread(@"Поисковые запросы отсутствуют. Создайте хотя бы один.");
                        return;
                    }

                    DynamicReflex recognizer = Recognizer;

                    if (recognizer == null)
                    {
                        List<Processor> processors = _imagesProcessorStorage.UniqueElements.ToList();

                        if (!processors.Any())
                        {
                            ErrorMessageInOtherThread(@"Образы для поиска отсутствуют. Создайте хотя бы один.");
                            return;
                        }

                        recognizer = new DynamicReflex(new ProcessorContainer(processors));

                        if (OutHistory(null, recognizer.Processors.ToArray(), @"start"))
                            Recognizer = recognizer;
                    }

                    _recognizerActivity.Set();

                    try
                    {
                        _stwRecognize.Restart();

                        bool? result;

                        try
                        {
                            result = recognizer.FindRelation(queries);
                        }
                        finally
                        {
                            _stwRecognize.Stop();
                        }

                        OutHistory(result, recognizer.Processors.ToArray());
                    }
                    catch (ThreadAbortException)
                    {
                        Recognizer = null;

                        throw;
                    }
                    catch
                    {
                        Recognizer = null;

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

        /// <summary>
        /// Осуществляет запись исторического события процесса тестирования <see cref="Recognizer"/>.
        /// </summary>
        /// <param name="result">Результат выполнения поискового запроса или <see langword="null"/>, в случае его отсутствия. Выводит 'T' = <see langword="true"/>, 'F' = <see langword="false"/> или 'пробел' в случае значения <see langword="null"/>.</param>
        /// <param name="ps">Содержимое экземпляра тестируемого класса на момент записи исторического события. Обязательно должно присутствовать, т.е. не может быть равно <see langword="null"/>.</param>
        /// <param name="comment">Комментарий события (если есть) или <see langword="null"/> (по умолчанию).</param>
        /// <returns>Возвращает значение <see langword="true"/> в случае, если операция завершилась без ошибок.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Может быть вызван из любого потока, не выбрасывает исключения.
        /// В случае наличия результата, устанавливает свойство <see cref="CurrentUndoRedoState"/> в соответствующее значение.
        /// Последнее событие всегда выделено, и находится в самом верху списка.
        /// В случае, если накопилось 100 и более событий, наиболее старые события будут затёрты более новыми.
        /// Нумерация событий начинается с ноля и заканчивается номером 99.
        /// Длина строки записи исторического события никогда не превышает ширину поля, содержащего эти записи.
        /// Таким образом, наличие горизонтальной прокрутки исключено.
        /// В случае, если количество карт в тестируемом объекте такое большое, что превысит ширину поля этих записей, оно будет заменено символом '∞'.
        /// Узнать его можно будет кликнув по событию, и посмотрев на строку снизу.
        /// </remarks>
        /// <seealso cref="Recognizer"/>
        /// <seealso cref="CurrentUndoRedoState"/>
        /// <example>
        /// 1) №10 T 13:45:30 (15) - поисковый запрос №10 выполнен успешно.
        /// 2) №99 F 15:35:31 (20) - поисковый запрос №99 не выполнен (требуемые данные не были найдены).
        /// 3) №5 T 14:15:50 (∞) - поисковый запрос №5 выполнен успешно, но для отображения количества карт в экземпляре тестируемого класса не хватило ширины поля для его отображения, т.к. оно превышает 9999.
        /// 4) №75 T 11:16:41 (∞) - поисковый запрос №75 выполнен успешно, но для отображения количества карт в экземпляре тестируемого класса не хватило ширины поля для его отображения, т.к. оно превышает 999.
        /// 5) №0   09:16:55 (start) - в случае события создания экземпляра тестируемого класса (поле результата заполнено пробелом, т.к. его на этом этапе быть не может).
        /// 6) №15   18:23:32 (EXCP) - в случае возникновения какой-либо ошибки (исключения) в процессе выполнения поискового запроса, сведения о ней можно узнать, посмотрев лог-файл в рабочей папке программы (поле результата заполнено пробелом, т.к. его на этом этапе быть не может).
        /// </example>
        bool OutHistory(bool? result, Processor[] ps, string comment = null)
        {
            bool globalResult = false;

            void Act()
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

                try
                {
                    if (result.HasValue)
                        CurrentUndoRedoState = result == true ? UndoRedoState.SUCCESS : UndoRedoState.ERROR;

                    if (lstHistory.Items.Count < 100)
                    {
                        string sm = GetSystemName(lstHistory.Items.Count);

                        _recognizeResults.Insert(0, (ps, 0, sm));
                        lstHistory.Items.Insert(0, sm);
                        lstHistory.SelectedIndex = 0;

                        globalResult = true;

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

                    globalResult = true;
                }
                catch (ThreadAbortException)
                {
                    ResetAbort();
                }
                catch (Exception ex)
                {
                    WriteLogMessage($@"{nameof(OutHistory)}: {ex.Message}.");
                }
            }

            if (InvokeRequired)
                Invoke((Action)Act);
            else
                Act();

            return globalResult;
        }

        /// <summary>
        ///     Глобальная функция для определения поведения при нажатии различных клавиш, при различных условиях.
        ///     Осуществляет запуск (или остановку) выполнения поискового запроса по нажатии клавиши <see cref="Keys.Enter"/> над полем ввода поискового запроса <see cref="txtRecogQueryWord"/>.
        ///     Переопределяет функцию отката изменений последнего выполненного поискового запроса (<see cref="CurrentUndoRedoWord"/>) посредством нажатия сочетания клавиш (<see cref="KeyEventArgs.Control"/> + <see cref="Keys.Z"/>) над полем ввода поискового запроса <see cref="txtRecogQueryWord"/>.
        ///     При нажатии клавиши <see cref="Keys.Escape"/> завершает работу приложения с помощью <see cref="Application.Exit()"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_KeyDown(object sender, KeyEventArgs e)
        {
            SafeExecute(() =>
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter when txtRecogQueryWord.Focused:
                        BtnRecognizeImage_Click(btnRecognizeImage, EventArgs.Empty);
                        return;
                    case Keys.Z when e.Control && txtRecogQueryWord.Focused:
                        if (txtRecogQueryWord.Text == CurrentUndoRedoWord)
                            return;

                        txtRecogQueryWord.Text = CurrentUndoRedoWord;
                        txtRecogQueryWord.Select(txtRecogQueryWord.Text.Length, 0);
                        return;
                    case Keys.Escape:
                        Application.Exit();
                        return;
                }
            });
        }

        /// <summary>
        ///     Предотвращает сигналы недопустимого ввода в текстовое поле, предназначенное для ввода поискового запроса.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtRecogQueryWord_KeyPress(object sender, KeyPressEventArgs e)
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
        ///     Отменяет отрисовку распознаваемого изображения в случае ухода указателя мыши с поля рисования.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbRecognizeImageDraw_MouseLeave(object sender, EventArgs e)
        {
            DrawStop();
        }

        /// <summary>
        /// Активирует кнопку сброса (<see cref="btnNextRecog"/>) распознаваемого изображения и связанные с ней кнопки, в случае, если изображение было изменено.
        /// </summary>
        void SetRedoRecognizeImage()
        {
            SetRedoRecognizeImage(_recognizeProcessorStorage.Count, true);
        }

        /// <summary>
        /// Активирует кнопку сброса (<see cref="btnNextRecog"/>) распознаваемого изображения и связанные с ней кнопки, в случае, если изображение было изменено.
        /// </summary>
        /// <param name="needPaintCheck">Значение <see langword="true"/> в случае, если требуется проверка наличия изображения на экране.</param>
        /// <remarks>
        /// В целях оптимизации быстродействия, принимается значение, которое необходимо для того, чтобы указать, требуется ли проверка наличия изображения на экране.
        /// Это необходимо для того, чтобы активировать кнопку его очистки.
        /// </remarks>
        void SetRedoRecognizeImage(bool needPaintCheck)
        {
            SetRedoRecognizeImage(_recognizeProcessorStorage.Count, needPaintCheck);
        }

        /// <summary>
        /// Активирует кнопку сброса (<see cref="btnNextRecog"/>) распознаваемого изображения и связанные с ней кнопки, в случае, если изображение было изменено.
        /// Обновляет количество карт в коллекции <see cref="_recognizeProcessorStorage"/> (переопределяя его) на момент вызова.
        /// </summary>
        /// <param name="count">Количество карт в коллекции <see cref="_recognizeProcessorStorage"/>.</param>
        /// <remarks>
        /// Применяется для обновления показателя количества карт в момент проведения определённой операции, не дожидаясь автоматического обновления интерфейса.
        /// </remarks>
        void SetRedoRecognizeImage(int count)
        {
            SetRedoRecognizeImage(count, true);
        }

        /// <summary>
        /// Активирует кнопку сброса (<see cref="btnNextRecog"/>) распознаваемого изображения и связанные с ней кнопки, в случае, если изображение было изменено.
        /// Обновляет количество карт в коллекции <see cref="_recognizeProcessorStorage"/> (переопределяя его) на момент вызова.
        /// </summary>
        /// <param name="count">Количество карт в коллекции <see cref="_recognizeProcessorStorage"/>.</param>
        /// <param name="needPaintCheck">Значение <see langword="true"/> в случае, если требуется проверка наличия изображения на экране.</param>
        /// <remarks>
        /// Применяется для обновления показателя количества карт в момент проведения определённой операции, не дожидаясь автоматического обновления интерфейса.
        /// В целях оптимизации быстродействия, принимается значение, которое необходимо для того, чтобы указать, требуется ли проверка наличия изображения на экране.
        /// Это необходимо для того, чтобы активировать кнопку его очистки.
        /// </remarks>
        void SetRedoRecognizeImage(int count, bool needPaintCheck)
        {
            bool changed = IsQueryChanged;

            btnSaveRecognizeImage.Enabled = changed && IsButtonsEnabled && !string.IsNullOrEmpty(txtRecogQueryWord.Text);

            btnNextRecog.Enabled = (changed && count > 0) || count > 1;
            btnDeleteRecognizeImage.Enabled = !changed && IsButtonsEnabled && count > 0;

            SavedPathInfoActualize(count, _recognizeProcessorStorage.SelectedPath);

            if (!needPaintCheck)
                return;

            btnClearRecogImage.Enabled = IsButtonsEnabled && IsPainting;
        }

        /// <summary>
        /// Запрещает (<see cref="_drawAllowed"/>) графический вывод в случае, если пользователь отпустил кнопку мыши или курсор покинул область рисования распознаваемого изображения.
        /// Актуализирует состояние кнопок, связанных с изображением.
        /// </summary>
        /// <remarks>
        /// Не выбрасывает исключения.
        /// Использует метод <see cref="SetRedoRecognizeImage()"/>.
        /// </remarks>
        /// <seealso cref="SetRedoRecognizeImage()"/>
        void DrawStop()
        {
            SafeExecute(() =>
            {
                _drawAllowed = false;

                SetRedoRecognizeImage();
            });
        }

        /// <summary>
        ///     Выводит создаваемый пользователем рисунок на экран, если это действие разрешено флагом <see cref="_drawAllowed"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbRecognizeImageDraw_MouseMove(object sender, MouseEventArgs e)
        {
            SafeExecute(() =>
            {
                if (_drawAllowed)
                    DrawPoint(e.X, e.Y, e.Button);
            });
        }

        /// <summary>
        ///     Рисует точку в указанном месте на <see cref="pbRecognizeImageDraw" /> с помощью <see cref="_grRecognizeImageGraphics" />.
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

            SafeExecute(() => pbRecognizeImageDraw.Refresh());
        }

        /// <summary>
        ///     Очищает поле рисования распознаваемого изображения.
        ///     Актуализирует состояние кнопок, связанных с распознаваемым изображением.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnClearRecogImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                _grRecognizeImageGraphics.Clear(DefaultColor);
                SetRedoRecognizeImage();
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });

            SafeExecute(() => pbRecognizeImageDraw.Refresh());
        }

        /// <summary>
        ///     Сохраняет поисковый запрос на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnSaveRecognizeImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                string tag = txtRecogQueryWord.Text;

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
        }

        /// <summary>
        ///     Обрабатывает событие нажатия кнопки загрузки распознаваемого изображения.
        ///     Загружает карту по указанному пути, используя диалог открытия файла.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Для выполнения операции используется метод <see cref="ImageActualize(ImageActualizeAction, string, Processor)"/> с параметром <see cref="ImageActualizeAction.LOAD"/>.
        /// Сбрасывает статус поиска (<see cref="CurrentUndoRedoState"/>) на (<see cref="UndoRedoState.UNKNOWN"/>).
        /// </remarks>
        /// <seealso cref="ImageActualize(ImageActualizeAction, string, Processor)"/>
        /// <seealso cref="ImageActualizeAction.LOAD"/>
        /// <seealso cref="CurrentUndoRedoState"/>
        /// <seealso cref="UndoRedoState"/>
        void BtnLoadRecognizeImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                if (dlgOpenImage.ShowDialog(this) != DialogResult.OK)
                    return;

                ImageActualize(ImageActualizeAction.LOAD, dlgOpenImage.FileName);
                CurrentUndoRedoState = UndoRedoState.UNKNOWN;
            });
        }

        /// <summary>
        ///     Обрабатывает событие выбора момента в истории исследуемого <see cref="DynamicReflex"/>.
        ///     Отображает состояние <see cref="DynamicReflex"/> в окне "История объекта".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void LstHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            SafeExecute(ChangeSystemSelectedIndex);
        }

        /// <summary>
        ///     Используется для обработки события выбора момента в истории исследуемого <see cref="DynamicReflex"/>.
        ///     Отображает состояние <see cref="DynamicReflex"/> в окне "История объекта".
        ///     Актуализирует состояние интерфейса пользователя.
        /// </summary>
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
                btnConSaveImage.Enabled = anyProcs && IsButtonsEnabled && p != null;
                btnConSaveAllImages.Enabled = anyProcs && IsButtonsEnabled && processors.Length > 1;
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
        ///     Обрабатывает событие выбора следующей карты, рассматриваемой в выбранном историческом моменте.
        ///     Актуализирует состояние интерфейса пользователя с помощью метода <see cref="ChangeSystemSelectedIndex()"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <seealso cref="ChangeSystemSelectedIndex()"/>
        void BtnConNext_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                (Processor[] processors, int reflexMapIndex, string) q = SelectedResult;

                if (q.reflexMapIndex >= q.processors.Length - 1)
                    q.reflexMapIndex = 0;
                else
                    q.reflexMapIndex++;

                SelectedResult = q;

                ChangeSystemSelectedIndex();
            });
        }

        /// <summary>
        ///     Обрабатывает событие выбора предыдущей карты, рассматриваемой в выбранном историческом моменте.
        ///     Актуализирует состояние интерфейса пользователя с помощью метода <see cref="ChangeSystemSelectedIndex()"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <seealso cref="ChangeSystemSelectedIndex()"/>
        void BtnConPrevious_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                (Processor[] processors, int reflexMapIndex, string) q = SelectedResult;

                if (q.reflexMapIndex < 1)
                    q.reflexMapIndex = q.processors.Length - 1;
                else
                    q.reflexMapIndex--;

                SelectedResult = q;

                ChangeSystemSelectedIndex();
            });
        }

        /// <summary>
        ///     Сохраняет выбранную карту <see cref="Processor" /> выбранного исторического момента исследуемого <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() => _imagesProcessorStorage.SaveToFile(SelectedResult.comment,
                new[] { SelectedResult.processors[SelectedResult.reflexMapIndex] }));
        }

        /// <summary>
        ///     Сохраняет все карты <see cref="Processor" /> выбранного исторического момента исследуемого <see cref="DynamicReflex" /> на жёсткий диск.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnConSaveAllImages_Click(object sender, EventArgs e)
        {
            SafeExecute(() => _imagesProcessorStorage.SaveToFile(SelectedResult.comment, SelectedResult.processors));
        }

        /// <summary>
        ///     Обрабатывает событие завершения работы программы.
        ///     Закрывает файлы, потоки.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (IsExited)
                {
                    _exitingThread.Join();

                    return;
                }

                e.Cancel = true;

                btnRecognizeImage.Enabled = false;
                txtRecogQueryWord.Enabled = false;

                DisposeWorkDirWatcher();
                DisposeRecognizeWatcher();
                DisposeImageWatcher();

                _exitingThread = new Thread(() => SafeExecute(() =>
                {
                    ConcurrentProcessorStorage.LongOperationsAllowed = false;

                    _stopBackground.Set();

                    StopRecognize()?.Join();

                    _fileRefreshThread?.Join();
                    _userInterfaceActualizeThread?.Join();

                    IsExited = true;

                    BeginInvoke(new Action(() => SafeExecute(Close)));
                }))
                {
                    IsBackground = true,
                    Name = "ExitingThread"
                };

                _exitingThread.Start();
            }
            catch (Exception ex)
            {
                WriteLogMessage(ex.Message);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Освобождает ресурсы, занимаемые изображением в указанном <see cref="PictureBox"/>.
        /// </summary>
        /// <param name="pb"><see cref="PictureBox"/>, <see cref="PictureBox.Image"/> которого требуется освободить.</param>
        /// <remarks>
        /// После освобождения <see cref="PictureBox.Image"/> = <see langword="null"/>.
        /// </remarks>
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
        void TextBoxTextChanged(object sender, EventArgs e)
        {
            SafeExecute(() =>
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
        }

        /// <summary>
        /// Изменяет формат текста в окне "История объекта".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Метод применяет параметр <see cref="TextFormatFlags.HorizontalCenter"/>.
        /// </remarks>
        /// <seealso cref="TextFormatFlags.HorizontalCenter"/>
        void LstHistory_DrawItem(object sender, DrawItemEventArgs e)
        {
            SafeExecute(() =>
            {
                if (e.Index < 0)
                    return;

                TextRenderer.DrawText(e.Graphics, lstHistory.Items[e.Index].ToString(), e.Font,
                    e.Bounds, e.ForeColor, e.BackColor, TextFormatFlags.HorizontalCenter);
            });
        }

        /// <summary>
        /// Выводит следующее распознаваемое изображение на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Если текущее распознаваемое изображение не было сохранено, будучи изменённым, оно будет потеряно.
        /// Операцию выполняет метод <see cref="ImageActualize(ImageActualizeAction, string, Processor)"/> с аргументом <see cref="ImageActualizeAction.NEXT"/>.
        /// </remarks>
        /// <seealso cref="ImageActualize(ImageActualizeAction, string, Processor)"/>
        /// <seealso cref="ImageActualizeAction.NEXT"/>
        void BtnNextRecogImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() => ImageActualize(ImageActualizeAction.NEXT));
        }

        /// <summary>
        /// Выводит предыдущее распознаваемое изображение на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Если текущее распознаваемое изображение не было сохранено, будучи изменённым, оно будет потеряно.
        /// Операцию выполняет метод <see cref="ImageActualize(ImageActualizeAction, string, Processor)"/> с аргументом <see cref="ImageActualizeAction.PREV"/>.
        /// </remarks>
        /// <seealso cref="ImageActualize(ImageActualizeAction, string, Processor)"/>
        /// <seealso cref="ImageActualizeAction.PREV"/>
        void BtnPrevRecogImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() => ImageActualize(ImageActualizeAction.PREV));
        }

        /// <summary>
        /// Необходим для того, чтобы можно было распознавать карты, которые получились после распознавания (т.н. "искать в найденном").
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Переносит не только тело карты, но и название, в виде запроса.
        /// Если текущее распознаваемое изображение не было сохранено, будучи изменённым, оно будет потеряно.
        /// Операцию выполняет метод <see cref="ImageActualize(ImageActualizeAction, string, Processor)"/> с аргументом <see cref="ImageActualizeAction.LOAD"/>.
        /// </remarks>
        /// <seealso cref="ImageActualize(ImageActualizeAction, string, Processor)"/>
        /// <seealso cref="ImageActualizeAction.LOAD"/>
        void BtnImageUpToQueries_Click(object sender, EventArgs e)
        {
            SafeExecute(() => ImageActualize(ImageActualizeAction.LOAD, txtSymbolPath.Text));
        }

        /// <summary>
        /// Удаляет выбранную распознаваемую карту (<see cref="ConcurrentProcessorStorage.IsSelectedPathExists()"/>).
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Использует метод <see cref="DeleteFile(string)"/>.
        /// </remarks>
        /// <seealso cref="DeleteFile(string)"/>
        void BtnDeleteRecognizeImage_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                (string path, bool isExists) = _recognizeProcessorStorage.IsSelectedPathExists();

                if (isExists)
                    DeleteFile(path);
            });
        }

        /// <summary>
        /// Удаляет указанный файл в корзину.
        /// </summary>
        /// <param name="path">Файл, который требуется удалить.</param>
        void DeleteFile(string path)
        {
            try
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin,
                    UICancelOption.ThrowException);
            }
            catch (DirectoryNotFoundException ex)
            {
                WriteLogMessage($@"{nameof(DeleteFile)}: {ex.Message}");
            }
        }

        /// <summary>
        /// Актуализирует состояние пользовательского интерфейса во время ввода поискового запроса.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtRecogQueryWord_TextChanged(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                CurrentUndoRedoWord = txtRecogQueryWord.Text;

                SetRedoRecognizeImage(false);
            });
        }

        /// <summary>
        ///     Вызывается во время первого отображения формы.
        ///     Выполняет инициализацию.
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
                WriteLogMessage($@"{ex.Message}{Environment.NewLine}Программа будет завершена.");
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Рисует рамку вокруг элемента <see cref="pbRecognizeImageDraw"/> на <see cref="grpSourceImage"/>, с помощью <see cref="_grpSourceImageGraphics"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Использует метод <see cref="DrawFieldFrame(Control, Graphics, bool)"/>.
        /// </remarks>
        /// <seealso cref="DrawFieldFrame(Control, Graphics, bool)"/>
        void PbRecognizeImageDraw_Paint(object sender, PaintEventArgs e)
        {
            SafeExecute(() => DrawFieldFrame(pbRecognizeImageDraw, _grpSourceImageGraphics, true));
        }

        /// <summary>
        /// Рисует рамку вокруг элемента <see cref="pbConSymbol"/> на <see cref="grpResults"/>, с помощью <see cref="_grpResultsGraphics"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Использует метод <see cref="DrawFieldFrame(Control, Graphics, bool)"/>.
        /// </remarks>
        /// <seealso cref="DrawFieldFrame(Control, Graphics, bool)"/>
        void PbConSymbol_Paint(object sender, PaintEventArgs e)
        {
            SafeExecute(() => DrawFieldFrame(pbConSymbol, _grpResultsGraphics, true));
        }

        /// <summary>
        /// Рисует рамку вокруг элемента <see cref="pbImage"/> на <see cref="grpImages"/>, с помощью <see cref="_grpImagesGraphics"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Использует метод <see cref="DrawFieldFrame(Control, Graphics, bool)"/>.
        /// </remarks>
        /// <seealso cref="DrawFieldFrame(Control, Graphics, bool)"/>
        void PbImage_Paint(object sender, PaintEventArgs e)
        {
            SafeExecute(() => DrawFieldFrame(pbImage, _grpImagesGraphics, true));
        }

        /// <summary>
        /// Освобождает все используемые ресурсы.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// В случае какой-либо ошибки программа будет принудительно завершена.
        /// </remarks>
        void FrmExample_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                _grRecognizeImageGraphics?.Dispose();
                _grpResultsGraphics?.Dispose();
                _grpImagesGraphics?.Dispose();
                _grpSourceImageGraphics?.Dispose();

                _imageFrameResetPen?.Dispose();

                DisposeImage(pbRecognizeImageDraw);
                DisposeImage(pbImage);
                DisposeImage(pbConSymbol);
            }
            catch (Exception ex)
            {
                WriteLogMessage(ex.Message);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Операции визуализации, выполняемые методом <see cref="ImageActualize(ImageActualizeAction, string, Processor)"/>.
        /// </summary>
        enum ImageActualizeAction
        {
            /// <summary>
            /// Выводит следующее распознаваемое изображение на экран.
            /// Если счётчик достигнет последнего изображения, следующим шагом, он переключится на первое.
            /// </summary>
            NEXT,

            /// <summary>
            /// Выводит предыдущее распознаваемое изображение на экран.
            /// Если счётчик достигнет первого изображения, следующим шагом, он переключится на последнее.
            /// </summary>
            PREV,

            /// <summary>
            /// Загружает распознаваемую карту по указанному пути.
            /// Если карта находится в рабочем каталоге хранилища (<see cref="RecognizeProcessorStorage.WorkingDirectory"/>), произойдёт "переключение" на неё, т.е. она будет перезагружена, затем будет актуализировано состояние пользовательского интерфейса.
            /// Если карта находится за пределами рабочего каталога хранилища, она будет отображена на экране, но не может быть добавлена в хранилище до того, как будет сохранена в рабочий каталог.
            /// </summary>
            LOAD,

            /// <summary>
            /// Необходим для отображения указанной распознаваемой карты на экране. В этом случае должна быть указана сама карта, но не путь к ней.
            /// Путь должен быть равен <see langword="null"/>.
            /// Применим в случаях, когда необходимо актуализировать распознаваемое изображение на экране, например, если был изменён файл с изображением карты, либо во время запуска программы.
            /// </summary>
            LOADDIRECTLY,

            /// <summary>
            /// Выполняет обновление состояния пользовательского интерфейса, предназначенного для создания распознаваемого изображения, учитывая изменения различных параметров (в т.ч. его ширины).
            /// </summary>
            REFRESH
        }

        /// <summary>
        /// Рисует рамки вокруг элементов <see cref="pbRecognizeImageDraw"/>, <see cref="pbConSymbol"/>, <see cref="pbImage"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <remarks>
        /// Использует метод <see cref="DrawFieldFrame(Control, Graphics, bool)"/>.
        /// </remarks>
        /// <seealso cref="DrawFieldFrame(Control, Graphics, bool)"/>
        /// <seealso cref="PbRecognizeImageDraw_Paint(object, PaintEventArgs)"/>
        /// <seealso cref="PbConSymbol_Paint(object, PaintEventArgs)"/>
        /// <seealso cref="PbImage_Paint(object, PaintEventArgs)"/>
        private void FrmExample_Paint(object sender, PaintEventArgs e)
        {
            DrawFieldFrame(pbConSymbol, _grpResultsGraphics, true);
            DrawFieldFrame(pbImage, _grpImagesGraphics, true);
            DrawFieldFrame(pbRecognizeImageDraw, _grpSourceImageGraphics, true);
        }
    }
}