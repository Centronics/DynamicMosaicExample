using System;
using System.Drawing;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    public sealed class RecognizeProcessorStorage : ConcurrentProcessorStorage
    {
        readonly int _minWidth;

        readonly int _maxWidth;

        readonly int _height;

        public RecognizeProcessorStorage(int minWidth, int maxWidth, int height, string extImg) : base(extImg)
        {
            if (string.IsNullOrWhiteSpace(extImg))
                throw new ArgumentNullException(nameof(extImg), $@"Расширение загружаемых изображений должно быть указано ({extImg ?? @"null"}).");

            _minWidth = minWidth;
            _maxWidth = maxWidth;
            _height = height;
        }

        protected override Processor GetAddingProcessor(string fullPath) => new Processor(LoadRecognizeBitmap(fullPath), GetProcessorTag(fullPath));

        public override string ImagesPath => FrmExample.RecognizeImagesPath;

        public override ProcessorStorageType StorageType => ProcessorStorageType.RECOGNIZE;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        /// <param name="relativeFolderPath"></param>
        public string SaveToFile(Processor processor, string relativeFolderPath)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                CreateFolder();
                (Processor p, string path) = GetUniqueProcessorWithMask(processor, relativeFolderPath);
                SaveToFile(ImageRect.GetBitmap(p), path);
                SavedRecognizePath = path;
                return path;
            }
        }

        static string GetProcessorTag(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException($@"{nameof(GetProcessorTag)}: Обнаружен пустой параметр, значение ({fullPath ?? @"<null>"}).", nameof(fullPath));

            return ParseName(Path.GetFileNameWithoutExtension(fullPath)).name;
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
                throw new Exception($@"Ошибка при загрузке изображения по пути: {fullPath}{Environment.NewLine}Текст ошибки: ""{ex.Message}"".", ex);
            }

            if (btm.Width < _minWidth || btm.Width > _maxWidth)
            {
                int w = btm.Width;
                btm.Dispose();
                throw new Exception($@"Загружаемое изображение не подходит по ширине: {w}. Она выходит за рамки допустимого ({_minWidth};{_maxWidth}). Путь: {fullPath}.");
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