using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using DynamicMosaic;
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

        protected override Processor GetAddingProcessor(string fullPath) => new Processor(LoadRecognizeBitmap(fullPath), GetProcessorTag(fullPath));

        protected override string GetProcessorTag(string fullPath) => $@"{GetQueryFromPath(Path.GetFileNameWithoutExtension(fullPath))}{ImageRect.TagSeparatorChar}";

        protected override string ImagesPath => FrmExample.RecognizeImagesPath;

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
                    for (int k = _minWidth; k <= _maxWidth; k += _widthStep)
                        yield return k;
                }
            }
        }

        internal Bitmap LoadRecognizeBitmap(string fullPath)
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
                btm.Dispose();
                throw new Exception($@"Загружаемое изображение не подходит по ширине: {btm.Width}. Она выходит за рамки допустимого. Попробуйте создать изображение и сохранить его заново. Путь: {fullPath}.");
            }

            if (btm.Height != _height)
            {
                btm.Dispose();
                throw new Exception($@"Загружаемое изображение не подходит по высоте: {btm.Height}; необходимо: {_height}. Путь: {fullPath}.");
            }

            btm.SetPixel(0, 0, btm.GetPixel(0, 0)); //Необходим для устранения "Ошибки общего вида в GDI+" при попытке сохранения загруженного файла.

            return btm;
        }
    }
}