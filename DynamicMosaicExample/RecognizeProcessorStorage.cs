using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using DynamicParser;

namespace DynamicMosaicExample
{
    internal sealed class RecognizeProcessorStorage : ConcurrentProcessorStorage
    {
        readonly int _minWidth;

        readonly int _maxWidth;

        readonly int _height;

        readonly int _widthStep;

        public RecognizeProcessorStorage(int minWidth, int maxWidth, int widthStep, int height)
        {
            _minWidth = minWidth;
            _maxWidth = maxWidth;
            _height = height;
            _widthStep = widthStep;
        }

        public override Processor GetAddingProcessor(string fullPath) => new Processor(LoadRecognizeBitmap(fullPath), GetProcessorTag(fullPath));

        public override string GetProcessorTag(string fullPath) => $@"{GetQueryFromPath(fullPath)}";

        protected override string ImagesPath => FrmExample.RecognizeImagesPath;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        internal override string SaveToFile(Processor processor, string folderName)
        {
            bool rewrite = folderName == "rewrite";

            if (!string.IsNullOrEmpty(folderName) && !rewrite)
                throw new InvalidOperationException($@"{nameof(SaveToFile)}: В классе {nameof(RecognizeProcessorStorage)} аргумент {nameof(folderName)} не может быть задан.");

            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (_syncObject)
            {
                (Processor proc, string path, string alias) = AddTagToSet(NamesToSave, new ProcPath(processor, GetImagePath(ImagesPath, processor.Tag + ImageRect.TagSeparatorChar)), processor.Tag, null);
                string result = rewrite ? path : alias;
                SaveToFile(ImageRect.GetBitmap(proc), result);
                return result;
            }
        }

        static string GetQueryFromPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException();

            fullPath = Path.GetFileNameWithoutExtension(fullPath);

            for (int k = fullPath.Length - 1; k >= 0; k--)
                if (fullPath[k] == ImageRect.TagSeparatorChar)
                    return fullPath.Substring(0, k);

            return fullPath;
        }

        /// <summary>
        ///     Перечисляет возможные значения ширины поля создания сканируемого изображения.
        /// </summary>
        IEnumerable<int> WidthSizes
        {
            get
            {
                unchecked
                {
                    for (int k = _minWidth; k < _maxWidth; k += _widthStep)
                        yield return k;
                    yield return _maxWidth;
                }
            }
        }

        Bitmap LoadRecognizeBitmap(string fullPath)
        {
            Bitmap btm;

            try
            {
                btm = LoadBitmap(fullPath);
            }
            catch (Exception ex)
            {
                throw new Exception($@"Ошибка при загрузке изображения по пути: {fullPath}{Environment.NewLine}Текст ошибки: {ex.Message}.", ex);
            }

            ImageFormat iformat = btm.RawFormat;
            if (!iformat.Equals(ImageFormat.Bmp))
            {
                btm.Dispose();
                throw new Exception($@"Загружаемое изображение не подходит по формату: {iformat}; необходимо: {ImageFormat.Bmp}. Путь: {fullPath}.");
            }

            if (WidthSizes.All(s => s != btm.Width))
            {
                int w = btm.Width;
                btm.Dispose();
                throw new Exception($@"Загружаемое изображение не подходит по ширине: {w}. Она выходит за рамки допустимого. Попробуйте создать изображение и сохранить его заново. Путь: {fullPath}.");
            }

            if (btm.Height != _height)
            {
                int h = btm.Height;
                btm.Dispose();
                throw new Exception($@"Загружаемое изображение не подходит по высоте: {h}; необходимо: {_height}. Путь: {fullPath}.");
            }

            btm.SetPixel(0, 0, btm.GetPixel(0, 0)); //Необходим для устранения "Ошибки общего вида в GDI+" при попытке сохранения загруженного файла.

            return btm;
        }

        internal override IEnumerable<(Processor processor, string path, string alias)> Elements
        {
            get
            {
                lock (_syncObject)
                {
                    foreach (ProcPath p in _dictionaryByPath.Values)
                        yield return (p.CurrentProcessor, p.CurrentPath, string.Empty);
                }
            }
        }
    }
}