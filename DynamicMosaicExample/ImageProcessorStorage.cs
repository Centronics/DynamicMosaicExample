﻿using System;
using System.Drawing;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    internal sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        public override Processor GetAddingProcessor(string fullPath) => ImageRect.GetProcessor(LoadBitmap(fullPath), GetProcessorTag(fullPath));

        /// <summary>
        ///     Преобразует название карты, заканчивающееся символами '0', в строку, содержащую имя и количество символов '0' в
        ///     конце названия карты <see cref="Processor" />.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag" /> карты <see cref="Processor" />.</param>
        /// <returns>Возвращает строку, содержащую имя и количество символов '0' в конце названия карты <see cref="Processor" />.</returns>
        public override string GetProcessorTag(string fullPath) => Path.GetFileNameWithoutExtension(fullPath);

        public override string ImagesPath => FrmExample.SearchImagesPath;
        public override ProcessorStorageType StorageType => ProcessorStorageType.IMAGE;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        internal override (Bitmap, string) SaveToFile(Processor processor, string folderName)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (_syncObject)
            {
                (uint? count, string name) = ImageRect.GetFileNumberByName(processor.Tag);
                (Processor proc, string resultPath) = AddTagToSet(NamesToSave, processor, (name, count), GetWorkingPath(folderName));
                Bitmap saveBtm = ImageRect.GetBitmap(proc);
                SaveToFile(saveBtm, resultPath);
                return (saveBtm, resultPath);
            }
        }
    }
}
