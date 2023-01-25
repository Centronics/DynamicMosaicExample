using System;
using System.Collections.Generic;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    public sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        public ImageProcessorStorage(string extImg) : base(extImg) { }

        protected override Processor GetAddingProcessor(string fullPath) => ImageRect.GetProcessor(LoadBitmap(fullPath), GetProcessorTag(fullPath));

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
        public override string SaveToFile(Processor processor, string relativeFolderPath)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                (uint? count, string name) = ImageRect.GetFileNumberByName(processor.Tag);
                (Processor p, string path) = GetUniqueProcessor(NamesToSave, processor, (name, count ?? 0), relativeFolderPath);
                SaveToFile(ImageRect.GetBitmap(p), path);
                SavedRecognizePath = path;
                return path;
            }
        }

        public void SaveToFile(string folderName, IEnumerable<Processor> processors)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException(nameof(folderName), $@"{nameof(SaveToFile)}: Имя папки не указано.");

            if (processors == null)
                throw new ArgumentNullException(nameof(processors), $@"{nameof(SaveToFile)}: Необходимо указать карты, которые требуется сохранить.");

            string path = CombinePaths(folderName);

            CreateFolder(path);

            foreach (Processor p in processors)
                SaveToFile(p, CreateImagePath(path, p.Tag));
        }

        public void SaveToFile(string folderName, Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            SaveToFile(folderName, new[] { processor });
        }

        public void SaveToFile(Processor processor)
        {
            CreateFolder();

            SaveToFile(processor, string.Empty);
        }
    }
}
