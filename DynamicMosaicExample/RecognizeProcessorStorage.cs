using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DynamicParser;

namespace DynamicMosaicExample
{
    public sealed class RecognizeProcessorStorage : ConcurrentProcessorStorage
    {
        readonly int _minWidth;

        readonly int _maxWidth;

        readonly int _height;

        readonly int _widthStep;

        public string ExtImg { get; }

        public RecognizeProcessorStorage(int minWidth, int maxWidth, int widthStep, int height, string extImg)
        {
            if (string.IsNullOrWhiteSpace(extImg))
                throw new ArgumentNullException(nameof(extImg), $@"Расширение загружаемых изображений должно быть указано ({extImg ?? @"null"}).");

            _minWidth = minWidth;
            _maxWidth = maxWidth;
            _height = height;
            _widthStep = widthStep;
            ExtImg = extImg;
        }

        public override Processor GetAddingProcessor(string fullPath) => new Processor(LoadRecognizeBitmap(fullPath), GetProcessorTag(fullPath));

        public string GetProcessorTag(string fullPath) => $@"{GetQueryFromPath(fullPath)}";

        public override string ImagesPath => FrmExample.RecognizeImagesPath;

        public override ProcessorStorageType StorageType => ProcessorStorageType.RECOGNIZE;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        /// <param name="relativeFolderPath"></param>
        public override string SaveToFile(Processor processor, string relativeFolderPath)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                (Processor p, string path) = GetUniqueProcessor(NamesToSave, processor, (processor.Tag, 0), relativeFolderPath);
                SaveToFile(ImageRect.GetBitmap(p), path);
                SavedRecognizePath = path;
                return path;
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

        /// <summary>
        ///     Получает список файлов изображений карт в указанной папке.
        ///     Это файлы с расширением <see cref="ExtImg" />.
        ///     В случае какой-либо ошибки возвращает пустой массив.
        /// </summary>
        /// <param name="path">Путь, по которому требуется получить список файлов изображений карт.</param>
        /// <returns>Возвращает список файлов изображений карт в указанной папке.</returns>
        protected override IEnumerable<string> GetFiles(string path)
        {
            try
            {
                return Directory.EnumerateFiles(path, $"*.{ExtImg}", SearchOption.AllDirectories).TakeWhile(_ => LongOperationsAllowed).Where(p =>
                    string.Compare(Path.GetExtension(p), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(GetFiles)}: {ex.Message}{Environment.NewLine}{nameof(path)}: {path}", ex);
            }
        }

        Bitmap LoadRecognizeBitmap(string fullPath)
        {
            Bitmap btm;

            try
            {
                btm = LoadBitmap(fullPath);
            }
            catch (FormatException fx)
            {
                throw new FormatException($@"{fx.Message} Путь: {fullPath}.");
            }
            catch (Exception ex)
            {
                throw new Exception($@"Ошибка при загрузке изображения по пути: {fullPath}{Environment.NewLine}Текст ошибки: {ex.Message}.", ex);
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

            btm.SetPixel(0, 0, btm.GetPixel(0, 0)); // Необходим для устранения "Ошибки общего вида в GDI+" при попытке сохранения загруженного файла.

            return btm;
        }
    }
}