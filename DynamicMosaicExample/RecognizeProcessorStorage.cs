using System;
using System.Drawing;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    public sealed class RecognizeProcessorStorage : ConcurrentProcessorStorage
    {
        readonly int _height;

        readonly int _maxWidth;
        readonly int _minWidth;

        public RecognizeProcessorStorage(int minWidth, int maxWidth, int height, string extImg) : base(extImg)
        {
            if (string.IsNullOrWhiteSpace(extImg))
                throw new ArgumentNullException(nameof(extImg),
                    $@"Расширение загружаемых изображений должно быть указано ({extImg ?? @"null"}).");

            _minWidth = minWidth;
            _maxWidth = maxWidth;
            _height = height;
        }

        public override string WorkingDirectory => FrmExample.RecognizeImagesPath;

        public override ProcessorStorageType StorageType => ProcessorStorageType.RECOGNIZE;

        protected override Processor GetAddingProcessor(string fullPath)
        {
            return new Processor(LoadRecognizeBitmap(fullPath), GetProcessorTag(fullPath));
        }

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        public string SaveToFile(Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor),
                    $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                CreateWorkingDirectory();
                string path = GetUniquePath(processor.Tag);
                SaveToFile(ImageRect.GetBitmap(processor), path);
                SelectedPath = path;
                return path;
            }
        }

        static string GetProcessorTag(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException(
                    $@"{nameof(GetProcessorTag)}: Обнаружен пустой параметр, значение ({fullPath ?? @"<null>"}).",
                    nameof(fullPath));

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
                throw new Exception(
                    $@"Ошибка при загрузке изображения по пути: {fullPath}{Environment.NewLine}Текст ошибки: ""{ex.Message}"".",
                    ex);
            }

            if (btm.Width < _minWidth || btm.Width > _maxWidth)
            {
                int w = btm.Width;
                btm.Dispose();
                throw new Exception(
                    $@"Загружаемое изображение не подходит по ширине: {w}. Она выходит за рамки допустимого ({_minWidth};{_maxWidth}). Путь: {fullPath}.");
            }

            if (btm.Height != _height)
            {
                int h = btm.Height;
                btm.Dispose();
                throw new Exception(
                    $@"Загружаемое изображение не подходит по высоте: {h}; необходимо: {_height}. Путь: {fullPath}.");
            }

            btm.SetPixel(0, 0,
                btm.GetPixel(0,
                    0)); // Необходим для устранения "Ошибки общего вида в GDI+" при попытке сохранения загруженного файла.

            return btm;
        }

        /// <summary>
        ///     Добавляет указанную карту <see cref="Processor" /> в <see cref="ConcurrentProcessorStorage" />.
        ///     Добавляет её в массив, содержащий хеши, и в массив, содержащий пути.
        ///     Хеш добавляемой карты может совпадать с хешами других карт.
        ///     Полный путь к добавляемой карте на достоверность не проверяется.
        ///     Если карта уже присутствовала в коллекции, то она будет перезагружена в неё.
        /// </summary>
        /// <param name="hashCode">Хеш добавляемой карты.</param>
        /// <param name="fullPath">Полный путь к добавляемой карте.</param>
        /// <param name="processor">Добавляемая карта <see cref="Processor" />.</param>
        protected override void ReplaceElement(int hashCode, string fullPath, Processor processor)
        {
            bool needReplace = RemoveProcessor(fullPath);

            BaseAddElement(hashCode, fullPath, processor);

            if (needReplace)
                SelectedPath = fullPath;
        }
    }
}