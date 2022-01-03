using System;
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
        protected override string GetProcessorTag(string fullPath) => Path.GetFileNameWithoutExtension(fullPath);

        protected override string ImagesPath => FrmExample.SearchImagesPath;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        internal override void SaveToFile(Processor processor, string folderName)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (_syncObject)
            {
                string savePath = ImagesPath;

                if (!string.IsNullOrEmpty(folderName))
                    Directory.CreateDirectory(savePath = Path.Combine(ImagesPath, folderName));

                (uint? count, string name) = ImageRect.GetFileNumberByName(processor.Tag);
                (Processor proc, string _, string alias) = AddTagToSet(NamesToSave, new ProcPath(processor, GetImagePath(savePath, name)), name, count, true);
                SaveToFile(ImageRect.GetBitmap(proc), alias);
            }
        }
    }
}
