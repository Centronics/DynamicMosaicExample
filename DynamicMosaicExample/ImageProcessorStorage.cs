using System;
using System.Collections.Generic;
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

        protected override string ImagesPath => FrmExample.SearchImagesPath;

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
                if (!string.IsNullOrEmpty(folderName))
                    Directory.CreateDirectory(Path.Combine(ImagesPath, folderName));

                (uint? count, string name) = ImageRect.GetFileNumberByName(processor.Tag);
                (Processor proc, string alias) = AddTagToSet(NamesToSave, processor, name, count, string.Empty);
                Bitmap saveBtm = ImageRect.GetBitmap(proc);
                SaveToFile(saveBtm, alias);
                return (saveBtm, alias);
            }
        }

        internal override IEnumerable<(Processor processor, string alias)> Elements
        {
            get
            {
                lock (_syncObject)
                {
                    HashSet<string> tagSet = new HashSet<string>();

                    foreach (ProcPath p in _dictionaryByPath.Values)
                    {
                        (ulong? number, string strPart) = ImageRect.NameParser(p.CurrentProcessor.Tag);
                        yield return AddTagToSet(tagSet, p.CurrentProcessor, strPart, number, string.Empty);
                    }
                }
            }
        }
    }
}
