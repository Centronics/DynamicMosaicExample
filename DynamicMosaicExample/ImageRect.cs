using System;
using System.Drawing;
using DynamicParser;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Предназначен для работы с образами искомых букв.
    /// </summary>
    public static class ImageRect
    {
        public const char TagSeparatorChar = '!';

        /// <summary>
        ///     Инициализирует экземпляр образа буквы для поиска.
        /// </summary>
        /// <param name="btm">Изображение буквы.</param>
        /// <param name="tag">Название буквы.</param>
        public static Processor GetProcessor(Bitmap btm, string tag)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(ImageRect)}: {nameof(btm)} = null.");
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException(@"Поле Tag карты не может быть пустым или белым полем.", nameof(tag));
            if (btm.Width != FrmExample.ImageWidth)
                throw new ArgumentException(
                    $@"Данное изображение не является образом искомой карты, т.к. не подходит по ширине: {btm.Width}, необходимо {FrmExample.ImageWidth}.",
                    nameof(btm));
            if (btm.Height != FrmExample.ImageHeight)
                throw new ArgumentException(
                    $@"Данное изображение не является образом искомой карты, т.к. не подходит по высоте: {btm.Height}, необходимо {FrmExample.ImageHeight}.",
                    nameof(btm));
            return new Processor(btm, tag);
        }

        /// <summary>
        ///     Преобразует <see cref="Processor" /> в <see cref="Bitmap" />.
        /// </summary>
        /// <param name="proc"><see cref="Processor" />, который требуется преобразовать.</param>
        /// <returns>Возвращает <see cref="Processor" />, преобразованный в <see cref="Bitmap" />.</returns>
        public static Bitmap GetBitmap(Processor proc)
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
        ///     Преобразует название карты, заканчивающееся символами '0', в параметры, включающие имя и количество символов '0' в
        ///     конце названия карты <see cref="Processor" />.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag" /> карты <see cref="Processor" />.</param>
        /// <param name="tolerant"></param>
        /// <returns>
        ///     Возвращает параметры, включающие имя и количество символов '0' в конце названия карты <see cref="Processor" />
        ///     .
        /// </returns>
        public static (ulong count, string name) GetFileNumberByName(string tag, uint tolerant)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException(nameof(tag), nameof(GetFileNumberByName));

            for (int start = tag.Length - 1, k = start; k >= 0; k--)
                if (tag[k] != '0' || k == 0)
                {
                    ulong count = Convert.ToUInt64(start - k);

                    if (count <= tolerant)
                        break;

                    return (count, tag.Substring(0, k + 1));
                }

            return (0, tag);
        }
    }
}