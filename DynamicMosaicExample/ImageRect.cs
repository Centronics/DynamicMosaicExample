using System;
using System.Drawing;
using DynamicParser;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Предназначен для работы с искомыми картами.
    /// </summary>
    public static class ImageRect
    {
        /// <summary>
        ///     Применяется для "маскировки" названий карт, на которых производится поиск различных данных.
        /// </summary>
        /// <remarks>
        ///     Под "маскировкой" понимается способ отличить карты, названия которых совпадают.
        ///     Под "названиями" понимается запрос, который требуется выполнить на карте.
        ///     Этот символ используется для разделения названия карты и её номера.
        ///     Используется, в том числе, для случая, когда название карты синтаксически совпадает с маскировкой.
        ///     В этом случае оно будет повторно замаскировано, чтобы не было путаницы при его разборе.
        ///     Маскировка выглядит следующим образом: {<see cref="Processor.Tag" />}{<see cref="TagSeparatorChar" />}{<see cref="ulong" />}.
        /// </remarks>
        public const char TagSeparatorChar = '!';

        /// <summary>
        ///     Инициализирует искомый образ (карту), проверяя его на соответствие определённым параметрам.
        /// </summary>
        /// <param name="btm">Изображение.</param>
        /// <param name="tag">Название.</param>
        /// <returns>Возвращает изображение в виде <see cref="Processor" />.</returns>
        /// <remarks>
        ///     Изображение по ширине (<see cref="Image.Width" />) должно соответствовать <see cref="FrmExample.ImageWidth" /> и
        ///     высоте (<see cref="Image.Height" />) должно соответствовать <see cref="FrmExample.ImageHeight" />, иначе
        ///     будет выброшено исключение <see cref="ArgumentException" />.
        ///     Параметр <paramref name="btm" /> не может быть равным <see langword="null" />, иначе будет выброшено исключение <see cref="ArgumentNullException" />.
        ///     Параметр <paramref name="tag" /> не может быть пустым (<see langword="null" />, <see cref="string.Empty" /> или состоять из пробелов),
        ///     иначе будет выброшено исключение <see cref="ArgumentException" />.
        ///     Метод потокобезопасен.
        /// </remarks>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentException" />
        /// <seealso cref="FrmExample.ImageWidth" />
        /// <seealso cref="FrmExample.ImageHeight" />
        public static Processor GetProcessor(Bitmap btm, string tag)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(GetProcessor)}: {nameof(btm)} = null.");
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException($@"{nameof(GetProcessor)}: Поле {nameof(Processor.Tag)} карты не может быть пустым или белым полем.",
                    nameof(tag));
            if (btm.Width != FrmExample.ImageWidth)
                throw new ArgumentException(
                    $@"{nameof(GetProcessor)}: Данное изображение не является образом искомой карты, т.к. не подходит по ширине: {btm.Width}, необходимо {FrmExample.ImageWidth}.",
                    nameof(btm));
            if (btm.Height != FrmExample.ImageHeight)
                throw new ArgumentException(
                    $@"{nameof(GetProcessor)}: Данное изображение не является образом искомой карты, т.к. не подходит по высоте: {btm.Height}, необходимо {FrmExample.ImageHeight}.",
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
    }
}