using System;
using System.Drawing;
using DynamicProcessor;
using Processor = DynamicParser.Processor;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Предназначен для работы с образами искомых букв.
    /// </summary>
    internal static class ImageRect
    {
        internal const char TagSeparatorChar = '!';

        /// <summary>
        ///     Инициализирует экземпляр образа буквы для распознавания.
        /// </summary>
        /// <param name="btm">Изображение буквы.</param>
        /// <param name="tag">Название буквы.</param>
        internal static Processor GetProcessor(Bitmap btm, string tag)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(ImageRect)}: {nameof(btm)} = null.");
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException(@"Поле Tag карты не может быть пустым или белым полем.", nameof(tag));
            if (btm.Width != FrmExample.ImageWidth)
                throw new ArgumentException($@"Данное изображение не является образом распознающей карты, т.к. не подходит по ширине: {btm.Width}, необходимо {FrmExample.ImageWidth}.", nameof(btm));
            if (btm.Height != FrmExample.ImageHeight)
                throw new ArgumentException($@"Данное изображение не является образом распознающей карты, т.к. не подходит по высоте: {btm.Height}, необходимо {FrmExample.ImageHeight}.", nameof(btm));
            return new Processor(ImageMap(btm), tag);
        }

        /// <summary>
        ///     Преобразует <see cref="Processor" /> в <see cref="Bitmap" />.
        /// </summary>
        /// <param name="proc"><see cref="Processor" />, который требуется преобразовать.</param>
        /// <returns>Возвращает <see cref="Processor" />, преобразованный в <see cref="Bitmap" />.</returns>
        internal static Bitmap GetBitmap(Processor proc)
        {
            if (proc == null)
                throw new ArgumentNullException(nameof(proc), $@"Параметр {nameof(proc)} не может быть null.");
            Bitmap b = new Bitmap(proc.Width, proc.Height);
            for (int y = 0; y < proc.Height; y++)
                for (int x = 0; x < proc.Width; x++)
                    b.SetPixel(x, y, proc[x, y].ValueColor);
            return b;
        }

        /// <summary>
        ///     Преобразует указанное изображение в массив знаков объектов карты.
        /// </summary>
        /// <param name="bitm">Изображение для конвертации.</param>
        /// <returns>Возвращает текущее изображение в виде массива объектов карты.</returns>
        static SignValue[,] ImageMap(Bitmap bitm)
        {
            if (bitm == null)
                throw new ArgumentNullException(nameof(bitm));
            SignValue[,] mas = new SignValue[bitm.Width, bitm.Height];
            for (int y = 0; y < bitm.Height; y++)
                for (int x = 0; x < bitm.Width; x++)
                    mas[x, y] = new SignValue(bitm.GetPixel(x, y));
            return mas;
        }

        /// <summary>
        ///     Выполняет разбор имени файла с образом буквы, выделяя номер буквы.
        /// </summary>
        /// <param name="tag">Имя файла без расширения.</param>
        /// <returns>
        ///     Возвращает значение <see langword="true" /> в случае, если разбор имени файла прошёл успешно, в противном
        ///     случае - <see langword="false" />.
        /// </returns>
        internal static (ulong number, bool isNumeric, string strPart) NameParser(string tag)
        {
            int k = tag.Length - 1;
            for (; k > 0; k--)
                if (!char.IsDigit(tag[k]))
                    break;
            if (k == tag.Length - 1)
                return (0, false, tag);
            return ulong.TryParse(tag.Substring(k + 1), out ulong number) ? (number, true, tag.Substring(0, k)) : (0, false, $@"{tag}{TagSeparatorChar}");
        }

        /// <summary>
        ///     Преобразует название карты, заканчивающееся символами '0', в параметры, включающие имя и количество символов '0' в
        ///     конце названия карты <see cref="Processor" />.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag" /> карты <see cref="Processor" />.</param>
        /// <returns>
        ///     Возвращает параметры, включающие имя и количество символов '0' в конце названия карты <see cref="Processor" />
        ///     .
        /// </returns>
        internal static (uint count, string name) GetFileNumberByName(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException(nameof(tag), nameof(GetFileNumberByName));
            int k = tag.Length - 1;
            if (k > 0 && tag[k] == TagSeparatorChar)
                if (tag[k - 1] == TagSeparatorChar)
                    return (0, tag);
                else
                    return (0, tag.Substring(0, k));
            uint count = 0;
            for (; k > 0; k--)
            {
                if (tag[k] != '0')
                    break;
                if (count == uint.MaxValue)
                    throw new Exception(
                        $@"{nameof(GetFileNumberByName)}: Счётчик количества файлов дошёл до максимума.");
                count++;
            }

            return (count, tag.Substring(0, k + 1));
        }
    }
}