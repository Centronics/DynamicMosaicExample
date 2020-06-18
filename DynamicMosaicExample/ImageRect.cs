using DynamicProcessor;
using System;
using System.Drawing;
using Processor = DynamicParser.Processor;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Предназначен для работы с образами искомых букв.
    /// </summary>
    struct ImageRect
    {
        /// <summary>
        ///     Инициализирует экземпляр образа буквы для распознавания.
        /// </summary>
        /// <param name="btm">Изображение буквы.</param>
        /// <param name="tag">Название буквы.</param>
        internal ImageRect(Bitmap btm, string tag)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(ImageRect)}: {nameof(btm)} = null.");
            SymbolicName=string.Empty;
            Number = 0U;
            IsSymbol = false;
            CurrentProcessor = null;
            if (btm.Width != FrmExample.ImageWidth || btm.Height != FrmExample.ImageHeight)
                return;
            (bool result, uint number, bool isNumeric, string symbolicName) = NameParser(tag);
            if (!result)
                return;
            string symbolString = isNumeric ? $@"{tag}|" : tag;
            SymbolicName = symbolicName;
            Number = number;
            CurrentProcessor = new Processor(ImageMap(btm), symbolString);
            IsSymbol = true;
        }

        /// <summary>
        /// Преобразует <see cref="Processor"/> в <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="proc"><see cref="Processor"/>, который требуется преобразовать.</param>
        /// <returns>Возвращает <see cref="Processor"/>, преобразованный в <see cref="Bitmap"/>.</returns>
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
        /// Отражает название текущего образа без цифровой составляющей.
        /// </summary>
        internal string SymbolicName { get; }

        /// <summary>
        ///     Номер текущего образа.
        /// </summary>
        internal uint Number { get; }

        /// <summary>
        ///     Получает значение, является ли данный файл образом, предназначенным для распознавания.
        ///     Значение <see langword="true" /> означает, что данный файл является образом для распознавания, <see langword="false" /> - нет.
        /// </summary>
        internal bool IsSymbol { get; }

        /// <summary>
        /// Получает текущее изображение в виде карты <see cref="Processor"/>.
        /// </summary>
        internal Processor CurrentProcessor { get; }

        /// <summary>
        ///     Получает текущее изображение в виде набора знаков объектов карты.
        /// </summary>
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
        /// <returns>Возвращает значение <see langword="true" /> в случае, если разбор имени файла прошёл успешно, в противном случае - <see langword="false" />.</returns>
        static (bool result, uint number, bool isNumeric, string symbolicName) NameParser(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tag.Length < 1)
                return (false, 0, false, string.Empty);
            int k = tag.Length - 1;
            for (; k > 0; k--)
                if (!char.IsDigit(tag[k]))
                    break;
            string symbName = tag.Substring(0, k + 1);
            if (k >= tag.Length - 1)
                return (true, 0, false, symbName);
            if (uint.TryParse(tag.Substring(k + 1), out uint number))
                return (true, number, true, symbName);
            return (true, 0, false, symbName);
        }
    }
}