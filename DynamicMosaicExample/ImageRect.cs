﻿using DynamicProcessor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Processor = DynamicParser.Processor;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Предназначен для работы с образами искомых букв.
    /// </summary>
    public sealed class ImageRect
    {
        /// <summary>
        ///     Расширение изображения образа искомой буквы.
        /// </summary>
        const string ExtImg = "bmp";

        /// <summary>
        ///     Инициализирует экземпляр образа буквы для распознавания.
        /// </summary>
        /// <param name="btm">Изображение буквы.</param>
        /// <param name="tag">Название буквы.</param>
        /// <param name="imagePath">Полный путь к изображению буквы.</param>
        ImageRect(Bitmap btm, string tag, string imagePath)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(ImageRect)}: {nameof(btm)} = null.");
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentNullException(nameof(imagePath),
                    $@"{nameof(ImageRect)}: {nameof(imagePath)} = null.");
            if (!NameParser(out ulong? number, tag) || number == null)
                return;
            if (btm.Width != FrmExample.ImageWidth || btm.Height != FrmExample.ImageHeight)
                return;
            SymbolString = tag.Substring(1);
            Symbol = char.ToUpper(tag[1]);
            SymbolName = new string(Symbol, 1);
            Number = number.Value;
            Bitm = btm;
            ImagePath = imagePath;
            IsSymbol = true;
        }

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
        ///     Путь, по которому осуществляется поиск искомых образов букв.
        /// </summary>
        static string SearchPath { get; } = Application.StartupPath;

        /// <summary>
        ///     Получает изображения букв, поиск которых следует осуществить.
        /// </summary>
        static IEnumerable<string> BitmapFiles => Directory.GetFiles(SearchPath, $"*.{ExtImg}");

        /// <summary>
        ///     Содержит текущее изображение.
        /// </summary>
        internal Bitmap Bitm { get; }

        /// <summary>
        ///     Полный путь к текущему образу.
        /// </summary>
        internal string ImagePath { get; }

        /// <summary>
        ///     Определяет значение поля <see cref="DynamicParser.Processor.Tag" />.
        /// </summary>
        internal string SymbolString { get; }

        /// <summary>
        ///     Символ текущей буквы в виде строки.
        /// </summary>
        internal string SymbolName { get; }

        /// <summary>
        ///     Символьное обозначение текущей буквы.
        /// </summary>
        char Symbol { get; }

        /// <summary>
        ///     Номер текущего образа.
        /// </summary>
        ulong Number { get; }

        /// <summary>
        ///     Получает значение, является ли данный файл образом, предназначенным для распознавания.
        ///     Значение <see langword="true" /> означает, что данный файл является образом для распознавания, <see langword="false" /> - нет.
        /// </summary>
        bool IsSymbol { get; }

        /// <summary>
        ///     Получает текущее изображение в виде набора знаков объектов карты.
        /// </summary>
        internal SignValue[,] ImageMap
        {
            get
            {
                if (!IsSymbol)
                    return null;
                SignValue[,] mas = new SignValue[Bitm.Width, Bitm.Height];
                for (int y = 0; y < Bitm.Height; y++)
                    for (int x = 0; x < Bitm.Width; x++)
                        mas[x, y] = new SignValue(Bitm.GetPixel(x, y));
                return mas;
            }
        }

        /// <summary>
        ///     Получает все имеющиеся на данный момент образы букв для распознавания.
        /// </summary>
        internal static IEnumerable<ImageRect> Images
        {
            get
            {
                foreach (string fname in BitmapFiles)
                    using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        ImageRect ir = new ImageRect(new Bitmap(fs), Path.GetFileNameWithoutExtension(fname), fname);
                        if (ir.IsSymbol)
                            yield return ir;
                    }
            }
        }

        /// <summary>
        ///     Сохраняет указанный образ буквы с указанным названием.
        /// </summary>
        /// <param name="name">Название буквы.</param>
        /// <param name="btm">Изображение буквы.</param>
        /// <returns>Возвращает экмемпляр текущего класса образа буквы.</returns>
        internal static ImageRect Save(char name, Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(Save)}: Сохраняемое изображение не указано.");
            string path = NewFileName(name);
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                btm.Save(fs, ImageFormat.Bmp);
            ImageRect ir = new ImageRect(btm, Path.GetFileNameWithoutExtension(path), path);
            if (!ir.IsSymbol)
                throw new Exception($"{nameof(Save)}: Неизвестная ошибка при сохранении изображения.");
            return ir;
        }

        /// <summary>
        ///     Выполняет разбор имени файла с образом буквы, выделяя номер буквы.
        /// </summary>
        /// <param name="number">Возвращает номер текущей буквы.</param>
        /// <param name="tag">Имя файла без расширения.</param>
        /// <returns>Возвращает значение true в случае, если разбор имени файла прошёл успешно, в противном случае - false.</returns>
        static bool NameParser(out ulong? number, string tag)
        {
            number = null;
            if (string.IsNullOrWhiteSpace(tag) || tag.Length < 3)
                return false;
            char ch = char.ToUpper(tag[0]);
            if (ch != 'M' && ch != 'B')
                return false;
            if (!ulong.TryParse(tag.Substring(2), out ulong ul)) return false;
            number = ul;
            return true;
        }

        /// <summary>
        ///     Генерирует имя нового образа, увеличивая его номер.
        /// </summary>
        /// <param name="name">Имя образа, на основании которого требуется сгенерировать новое имя.</param>
        /// <returns>Возвращает строку полного пути к файлу нового образа.</returns>
        static string NewFileName(char name)
        {
            ImageRect imageRect = null;
            {
                char nm = char.ToUpper(name);
                ulong max = 0;
                foreach (ImageRect ir in Images.Where(i => i.Symbol == nm).Where(i => i.Number >= max))
                {
                    max = ir.Number;
                    imageRect = ir;
                }
            }
            char prefix = char.IsUpper(name) ? 'b' : 'm';
            return imageRect != null
                ? $@"{SearchPath}\{prefix}{name}{unchecked(imageRect.Number + 1)}.{ExtImg}"
                : $@"{SearchPath}\{prefix}{name}0.{ExtImg}";
        }
    }
}