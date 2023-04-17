using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicParser;

namespace DynamicMosaicExample
{
    public sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        public ImageProcessorStorage(string extImg) : base(extImg)
        {
        }

        public override string WorkingDirectory => FrmExample.SearchImagesPath;

        public override ProcessorStorageType StorageType => ProcessorStorageType.IMAGE;

        public override bool IsSelectedOne
        {
            get
            {
                lock (SyncObject)
                {
                    if (IntSelectedIndex > -1)
                        return true;

                    string selectedPath = SelectedPath;

                    return !string.IsNullOrEmpty(selectedPath) &&
                           DictionaryByKey.ContainsKey(GetStringKey(selectedPath));
                }
            }
        }

        protected override Processor GetAddingProcessor(string fullPath)
        {
            return ImageRect.GetProcessor(ReadBitmap(fullPath), GetProcessorTag(fullPath));
        }

        /// <summary>
        ///     Преобразует название карты, заканчивающееся символами '0', в строку, содержащую имя и количество символов '0' в
        ///     конце названия карты <see cref="Processor" />.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns>Возвращает строку, содержащую имя и количество символов '0' в конце названия карты <see cref="Processor" />.</returns>
        public static string GetProcessorTag(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor" /> в рабочий каталог <see cref="WorkingDirectory"/>, в формате <see cref="ConcurrentProcessorStorage.ExtImg"/>.
        ///     Сохраняет под оригинальным названием, в случае необходимости, маскирует.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую требуется сохранить.</param>
        /// <remarks>
        /// Свойство <see cref="ConcurrentProcessorStorage.SelectedPath"/> будет содержать путь, по которому карта была сохранена.
        /// </remarks>
        void IntSaveToFile(Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor),
                    $@"{nameof(IntSaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                string path = GetUniquePath(processor.Tag);
                SaveToFile(ImageRect.GetBitmap(processor), path);
                SelectedPath = path;
            }
        }

        public void SaveToFile(Processor processor)
        {
            CreateWorkingDirectory();

            IntSaveToFile(processor);
        }

        public void SaveToFile(string folderName, IEnumerable<Processor> processors)
        {
            if (processors == null)
                throw new ArgumentNullException(nameof(processors),
                    $@"{nameof(SaveToFile)}: Необходимо указать карты, которые требуется сохранить.");

            string folder = CombinePaths(folderName);

            folder = AddEndingSlash(folder);

            CreateFolder(folder);

            List<Processor> lstProcs = processors.ToList();

            lock (SyncObject)
            {
                foreach ((Processor p, string path) in GetUniqueProcessor(lstProcs.Select(proc =>
                         {
                             (ulong? count, string name) = ImageRect.GetFileNumberByName(proc.Tag, 1);
                             return ((Processor)null, (count, ParseName(name).name), folder);
                         })).Select((pp, index) => (lstProcs[index], pp.path)))
                {
                    SaveToFile(ImageRect.GetBitmap(p), path);
                }
            }
        }
    }
}