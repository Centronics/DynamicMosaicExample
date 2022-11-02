﻿using System;
using System.Drawing;
using DynamicProcessor;
using Processor = DynamicParser.Processor;

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
                throw new ArgumentException($@"Данное изображение не является образом искомой карты, т.к. не подходит по ширине: {btm.Width}, необходимо {FrmExample.ImageWidth}.", nameof(btm));
            if (btm.Height != FrmExample.ImageHeight)
                throw new ArgumentException($@"Данное изображение не является образом искомой карты, т.к. не подходит по высоте: {btm.Height}, необходимо {FrmExample.ImageHeight}.", nameof(btm));
            return new Processor(ImageToMap(btm), tag);
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
        ///     Преобразует указанное изображение в массив знаков объектов карты.
        /// </summary>
        /// <param name="btm">Изображение для конвертации.</param>
        /// <returns>Возвращает текущее изображение в виде массива объектов карты.</returns>
        static SignValue[,] ImageToMap(Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm));
            SignValue[,] mas = new SignValue[btm.Width, btm.Height];
            for (int y = 0; y < btm.Height; y++)
                for (int x = 0; x < btm.Width; x++)
                    mas[x, y] = new SignValue(btm.GetPixel(x, y));
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
        public static (ulong? number, string strPart) NameParser(string tag)
        {
            for (int k = tag.Length - 1; k >= 0; k--)
                if (k == 0 || tag[k] == TagSeparatorChar || !char.IsDigit(tag[k]))
                    return k > 0 && ulong.TryParse(tag.Substring(k + 1), out ulong number)
                        ? (number, tag[k] == TagSeparatorChar ? tag.Substring(0, k) : tag.Substring(0, k + 1))
                        : ((ulong?)null, tag);

            throw new ArgumentException($@"{nameof(NameParser)}: Имя файла пустое.", nameof(tag));
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
        public static (uint? count, string name) GetFileNumberByName(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException(nameof(tag), nameof(GetFileNumberByName));
            int k = tag.Length - 1;
            if (k > 0 && tag[k] == TagSeparatorChar)
                if (tag[k - 1] == TagSeparatorChar)
                    return (null, tag);
                else
                    return (null, tag.Substring(0, k));
            uint c = 0;
            for (; k > 0; k--)
            {
                if (tag[k] != '0')
                    break;
                if (c == uint.MaxValue)
                    throw new Exception(
                        $@"{nameof(GetFileNumberByName)}: Счётчик количества файлов дошёл до максимума.");
                c++;
            }

            return (c == 0 ? (uint?)null : c, tag.Substring(0, k + 1));
        }
    }
}