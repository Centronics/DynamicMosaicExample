using System;
using System.Collections.Generic;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    internal sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        protected override Processor GetAddingProcessor(string fullPath) => ImageRect.GetProcessor(LoadBitmap(fullPath), GetProcessorTag(fullPath));

        /// <summary>
        ///     Преобразует название карты, заканчивающееся символами '0', в строку, содержащую имя и количество символов '0' в
        ///     конце названия карты <see cref="Processor" />.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag" /> карты <see cref="Processor" />.</param>
        /// <returns>Возвращает строку, содержащую имя и количество символов '0' в конце названия карты <see cref="Processor" />.</returns>
        protected override string GetProcessorTag(string fullPath)
        {
            (uint? count, string name) = ImageRect.GetFileNumberByName(Path.GetFileNameWithoutExtension(fullPath));
            return $@"{name}{count}";
        }

        protected override string ImagesPath => FrmExample.SearchImagesPath;
    }
}
