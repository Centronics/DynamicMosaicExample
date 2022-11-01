using System;
using System.Drawing;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    public sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        public override Processor GetAddingProcessor(string fullPath) => ImageRect.GetProcessor(LoadBitmap(fullPath), GetProcessorTag(fullPath));

        /// <summary>
        ///     Преобразует название карты, заканчивающееся символами '0', в строку, содержащую имя и количество символов '0' в
        ///     конце названия карты <see cref="Processor" />.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns>Возвращает строку, содержащую имя и количество символов '0' в конце названия карты <see cref="Processor" />.</returns>
        public string GetProcessorTag(string fullPath) => Path.GetFileNameWithoutExtension(fullPath);

        public override string ImagesPath => FrmExample.SearchImagesPath;
        public override ProcessorStorageType StorageType => ProcessorStorageType.IMAGE;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        /// <param name="relativeFolderPath"></param>
        public override (Bitmap, string) SaveToFile(Processor processor, string relativeFolderPath)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                (uint? count, string name) = ImageRect.GetFileNumberByName(processor.Tag);
                (Processor p, string resultPath) = GetUniqueProcessor(NamesToSave, processor, (name, count), relativeFolderPath);
                Bitmap saveBtm = ImageRect.GetBitmap(p);
                SaveToFile(saveBtm, resultPath);
                return (saveBtm, resultPath);
            }
        }

        public void SaveToFile(Processor processor, string folderName, string fileName) => SaveToFile(processor, CombinePaths(folderName, fileName));

        public void SaveToFile(Processor processor) => SaveToFile(processor, string.Empty);
    }
}
