using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static string GetProcessorTag(string fullPath) => Path.GetFileNameWithoutExtension(fullPath);

        public override string ImagesPath => FrmExample.SearchImagesPath;

        public override ProcessorStorageType StorageType => ProcessorStorageType.IMAGE;

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        /// <param name="relativeFolderPath"></param>
        void IntSaveToFile(Processor processor, string relativeFolderPath)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(IntSaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                (Processor p, string path) = GetUniqueProcessorWithMask(processor, relativeFolderPath);
                SaveToFile(ImageRect.GetBitmap(p), path);
                SavedRecognizePath = path;
            }
        }

        public void SaveToFile(string folderName, IEnumerable<Processor> processors)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException(nameof(folderName), $@"{nameof(SaveToFile)}: Имя папки не указано.");

            if (processors == null)
                throw new ArgumentNullException(nameof(processors), $@"{nameof(SaveToFile)}: Необходимо указать карты, которые требуется сохранить.");

            IEnumerable<Processor> procs = processors.ToList();

            if (procs.Any(p => p == null))
                throw new ArgumentNullException(nameof(processors), $@"{nameof(SaveToFile)}: Сохраняемая карта не может быть равна null.");

            string path = CombinePaths(folderName);

            CreateFolder(path);

            foreach (Processor p in procs)
                IntSaveToFile(p, CreateImagePath(path, p.Tag));
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

            IntSaveToFile(processor, string.Empty);
        }

        public void SaveSystemToFile(string folderName, IEnumerable<Processor> processors)
        {
            if (processors == null)
                throw new ArgumentNullException(nameof(processors), $@"{nameof(SaveToFile)}: Необходимо указать карты, которые требуется сохранить.");

            string folder = CombinePaths(folderName);

            folder = AddEndingSlash(folder);

            CreateFolder(folder);

            lock (SyncObject)
            {
                foreach ((Processor p, string path) in GetUniqueProcessor(processors.Select(proc =>
                         {
                             (ulong count, string name) = ImageRect.GetFileNumberByName(proc.Tag, 1);
                             return ((Processor, (ulong?, string), string)?)(proc, (count, ParseName(name).name), folder);
                         })))
                {
                    SaveToFile(ImageRect.GetBitmap(p), path);
                    SavedRecognizePath = path;
                }
            }
        }
    }
}
