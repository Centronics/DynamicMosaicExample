﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using DynamicMosaic;
using DynamicParser;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Потокобезопасное хранилище карт <see cref="Processor" /> с поддержкой поиска с использованием хеш-таблицы.
    ///     Поддерживаются повторяющиеся ключи.
    /// </summary>
    internal abstract class ConcurrentProcessorStorage
    {
        public abstract Processor GetAddingProcessor(string fullPath);

        public abstract string GetProcessorTag(string fullPath);

        protected abstract string ImagesPath { get; }

        protected static string GetImagePath(string sourcePath, string name) => $@"{Path.Combine(sourcePath ?? throw new InvalidOperationException($@"{nameof(GetImagePath)}: Исходный путь образа не указан."), name)}.{FrmExample.ExtImg}";

        internal abstract (Bitmap, string) SaveToFile(Processor processor, string folderName);

        protected (Processor processor, string path) AddTagToSet(ISet<string> tagSet, Processor p, string tag, ulong? number, string savedPath)
        {
            unchecked
            {
                ulong k = number ?? 0, mk = k;

                do
                {
                    string t = $@"{tag}{ImageRect.TagSeparatorChar}{k}";
                    if (tagSet.Add(t))
                        return (ProcessorHandler.ChangeProcessorTag(p, t), string.IsNullOrEmpty(savedPath) ? GetImagePath(ImagesPath, t) : savedPath);
                } while (++k != mk);

                string n = number == null ? "<пусто>" : number.ToString();
                throw new Exception($@"Нет свободного места для добавления карты в коллекцию: {p.Tag} по пути {ImagesPath}, изначальное имя карты {tag}, номер {n}.");
            }
        }

        /// <summary>
        ///     Коллекция карт, идентифицируемых по хешу.
        /// </summary>
        readonly Dictionary<int, ProcHash> _dictionary = new Dictionary<int, ProcHash>();

        /// <summary>
        ///     Коллекция карт, идентифицируемых по путям.
        /// </summary>
        protected readonly Dictionary<string, ProcPath> _dictionaryByPath = new Dictionary<string, ProcPath>();

        /// <summary>
        ///     Объект для синхронизации доступа к экземпляру класса <see cref="ConcurrentProcessorStorage" />, с использованием
        ///     конструкции <see langword="lock" />.
        /// </summary>
        protected readonly object _syncObject = new object();

        /// <summary>
        ///     Получает все элементы, добавленные в коллекцию <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        internal abstract IEnumerable<(Processor processor, string alias)> Elements { get; }

        protected HashSet<string> NamesToSave
        {
            get
            {
                lock (_syncObject)
                {
                    HashSet<string> tagSet = new HashSet<string>();

                    foreach (ProcPath p in _dictionaryByPath.Values)
                    {
                        (ulong? number, string strPart) = ImageRect.NameParser(Path.GetFileNameWithoutExtension(p.CurrentPath));
                        AddTagToSet(tagSet, p.CurrentProcessor, strPart, number, string.Empty);
                    }

                    return tagSet;
                }
            }
        }

        public bool IsWorkingPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException($@"{nameof(IsWorkingPath)}: Необходимо указать путь для проверки.", nameof(path));

            string p = ImagesPath;

            char c = p[p.Length - 1];
            if (c != '\\' && c != '/')
                p += Path.DirectorySeparatorChar;

            return path.StartsWith(p, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Получает количество карт, содержащихся в коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        internal int Count
        {
            get
            {
                lock (_syncObject)
                    return _dictionaryByPath.Count;
            }
        }

        /// <summary>
        ///     Возвращает карту <see cref="Processor" /> по указанному индексу, путь к ней, и количество карт в коллекции.
        ///     Если индекс представляет собой недопустимое значение, возвращается (<see langword="null" />,
        ///     <see cref="string.Empty" />, <see cref="Count" />).
        /// </summary>
        /// <param name="index">Индекс карты <see cref="Processor" />, которую надо вернуть.</param>
        /// <returns>Возвращает карту <see cref="Processor" /> по указанному индексу, путь к ней, и количество карт в коллекции.</returns>
        (Processor processor, string path, int count) this[int index]
        {
            get
            {
                lock (_syncObject)
                {
                    if (index < 0 || index >= Count)
                        return (null, string.Empty, Count);

                    ProcPath pp = _dictionaryByPath.Values.ElementAt(index);
                    return (pp.CurrentProcessor, pp.CurrentPath, Count);
                }
            }
        }

        /// <summary>
        ///     Возвращает карту <see cref="Processor" /> по указанному пути, путь к ней, и количество карт в коллекции.
        ///     От наличия файла на жёстком диске не зависит.
        ///     Если карта не найдена, возвращается (<see langword="null" />, <see cref="string.Empty" />, <see cref="Count" />).
        /// </summary>
        /// <param name="fullPath">Полный путь к карте <see cref="Processor" />.</param>
        /// <returns>Возвращает карту <see cref="Processor" /> по указанному пути, путь к ней, и количество карт в коллекции.</returns>
        internal (Processor processor, string path, int count) this[string fullPath]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(fullPath))
                    return (null, string.Empty, Count);
                lock (_syncObject)
                {
                    _dictionaryByPath.TryGetValue(fullPath.ToLower(), out ProcPath p);
                    return (p.CurrentProcessor, string.IsNullOrEmpty(p.CurrentPath) ? string.Empty : p.CurrentPath,
                        Count);
                }
            }
        }

        /// <summary>
        ///     Добавляет карту в коллекцию, по указанному пути.
        ///     Если карта не подходит по каким-либо признакам, а в коллекции хранится карта по тому же пути, то она удаляется из
        ///     коллекции.
        ///     Если карта уже присутствовала в коллекции, то она будет перезагружена в неё.
        /// </summary>
        /// <param name="fullPath">Полный путь к изображению, которое будет интерпретировано как карта <see cref="Processor" />.</param>
        internal void AddProcessor(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);
            Processor addingProcessor;
            try
            {
                addingProcessor = GetAddingProcessor(fullPath);
            }
            catch
            {
                RemoveProcessor(fullPath);
                throw;
            }

            int hashCode = CRCIntCalc.GetHash(addingProcessor);
            lock (_syncObject)
                AddElement(hashCode, fullPath, addingProcessor);
        }

        protected static Bitmap LoadBitmap(string fullPath)
        {
            FileStream fs = null;

            for (int k = 0; k < 50; k++)
            {
                try
                {
                    fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception ex)
                {
                    if (k > 48)
                        throw new FileNotFoundException($@"{nameof(AddProcessor)}: {ex.Message}", fullPath, ex);
                    Thread.Sleep(100);
                    continue;
                }

                break;
            }

            using (fs)
                return new Bitmap(fs ?? throw new InvalidOperationException($@"{nameof(LoadBitmap)}: {nameof(fs)} == null."));
        }

        /// <summary>
        ///     Добавляет указанную карту <see cref="Processor" /> в <see cref="ConcurrentProcessorStorage" />.
        ///     Добавляет её в массив, содержащий хеши, и в массив, содержащий пути.
        ///     Хеш добавляемой карты может совпадать с хешами других карт.
        ///     Полный путь к добавляемой карте на достоверность не проверяется.
        ///     Если карта уже присутствовала в коллекции, то она будет перезагружена в неё.
        /// </summary>
        /// <param name="hashCode">Хеш добавляемой карты.</param>
        /// <param name="fullPath">Полный путь к добавляемой карте.</param>
        /// <param name="processor">Добавляемая карта <see cref="Processor" />.</param>
        void AddElement(int hashCode, string fullPath, Processor processor)
        {
            string fPath = fullPath.ToLower();
            if (_dictionaryByPath.ContainsKey(fPath))
                RemoveProcessor(fullPath);

            _dictionaryByPath.Add(fPath, new ProcPath(processor, fullPath));
            if (_dictionary.TryGetValue(hashCode, out ProcHash ph))
                ph.AddProcessor(new ProcPath(processor, fullPath));
            else
                _dictionary.Add(hashCode, new ProcHash(new ProcPath(processor, fullPath)));
        }

        /// <summary>
        ///     Получает указанную или последнюю карту в случае недопустимого значения в индексе карты <see cref="Processor" />,
        ///     которую необходимо получить.
        ///     Актуализирует номер полученной карты и путь к ней.
        ///     Получает количество карт в коллекции <see cref="ConcurrentProcessorStorage" /> на момент получения карты.
        ///     В случае отсутствия карт в коллекции, возвращается (<see langword="null" />, <see cref="string.Empty"/>, 0), index тоже будет равен нолю.
        /// </summary>
        /// <param name="index">
        ///     Индекс карты <see cref="Processor" />, которую необходимо получить. В случае допустимого
        ///     изначального значения, это значение остаётся прежним, иначе равняется индексу последней карты в коллекции.
        /// </param>
        /// <returns>Возвращает карту, путь к ней, и количество карт на момент её получения.</returns>
        internal (Processor processor, string path, int count) GetLastProcessor(ref int index)
        {
            lock (_syncObject)
            {
                int count = Count;
                if (count < 1)
                {
                    index = 0;
                    return (null, string.Empty, 0);
                }

                if (index < 0 || index >= count)
                    index = count - 1;
                (Processor processor, string path, _) = this[index];
                return (processor, path, count);
            }
        }

        /// <summary>
        ///     Получает указанную или первую карту в случае недопустимого значения в индексе карты <see cref="Processor" />,
        ///     которую необходимо получить.
        ///     Актуализирует номер полученной карты и путь к ней.
        ///     Получает количество карт в коллекции <see cref="ConcurrentProcessorStorage" /> на момент получения карты.
        ///     В случае отсутствия карт в коллекции, возвращается (<see langword="null" />, <see cref="string.Empty"/>, 0), index тоже будет равен нолю.
        /// </summary>
        /// <param name="index">
        ///     Индекс карты <see cref="Processor" />, которую необходимо получить. В случае допустимого
        ///     изначального значения, это значение остаётся прежним, иначе равняется индексу первой карты в коллекции.
        /// </param>
        /// <returns>Возвращает карту, путь к ней, и количество карт на момент её получения.</returns>
        internal (Processor processor, string path, int count) GetFirstProcessor(ref int index)
        {
            lock (_syncObject)
            {
                int count = Count;
                if (count < 1)
                {
                    index = 0;
                    return (null, string.Empty, 0);
                }

                if (index < 0 || index >= count)
                    index = 0;
                (Processor processor, string path, _) = this[index];
                return (processor, path, count);
            }
        }

        /// <summary>
        ///     Удаляет указанную карту <see cref="Processor" /> из коллекции <see cref="ConcurrentProcessorStorage" />,
        ///     идентифицируя её по пути к ней.
        /// </summary>
        /// <param name="fullPath">Полный путь к карте <see cref="Processor" />, которую необходимо удалить из коллекции.</param>
        internal void RemoveProcessor(string fullPath)
        {
            lock (_syncObject)
                RemoveProcessor(this[fullPath?.ToLower()].processor);
        }

        /// <summary>
        ///     Удаляет указанную карту <see cref="Processor" /> из коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую следует удалить.</param>
        void RemoveProcessor(Processor processor)
        {
            if (processor is null)
                return;
            int hash = CRCIntCalc.GetHash(processor);
            lock (_syncObject)
            {
                if (!_dictionary.TryGetValue(hash, out ProcHash ph))
                    return;
                int index = 0;
                foreach (ProcPath px in ph.Elements)
                    if (ReferenceEquals(processor, px.CurrentProcessor))
                    {
                        _dictionaryByPath.Remove(ph[index].CurrentPath.ToLower());
                        ph.RemoveProcessor(index);
                        break;
                    }
                    else
                        index++;

                if (!ph.Elements.Any())
                    _dictionary.Remove(hash);
            }
        }

        /// <summary>
        ///     Сохраняет указанное изображение на жёсткий диск.
        /// </summary>
        /// <param name="btm">Изображение, которое требуется сохранить.</param>
        /// <param name="path">Абсолютный путь, по которому требуется сохранить изображение. Если путь относительный, то используется <see cref="FrmExample.SearchImagesPath"/>.</param>
        protected void SaveToFile(Bitmap btm, string path)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm),
                    $@"{nameof(SaveToFile)}: Необходимо указать изображение, которое требуется сохранить.");
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException(
                    $@"{nameof(SaveToFile)}: Путь, по которому требуется сохранить изображение, не задан.",
                    nameof(path));
            path = Path.ChangeExtension(path, string.Empty);
            string resultTmp = Path.Combine(ImagesPath, $@"{path}bmpTMP");
            string result = Path.Combine(ImagesPath, path + FrmExample.ExtImg);
            using (FileStream fs = new FileStream(resultTmp, FileMode.Create, FileAccess.Write))
                btm.Save(fs, ImageFormat.Bmp);
            try
            {
                File.Delete(result);
                File.Move(resultTmp, result);
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception($@"{nameof(SaveToFile)}: Ошибка при сохранении карты: {resultTmp}: {ex.Message}");
            }
            catch (IOException ex)
            {
                throw new Exception(
                    $@"{nameof(SaveToFile)}: Попытка перезаписать существующий файл: {result}: {ex.Message}");
            }
        }

        /// <summary>
        ///     Хранит карту <see cref="Processor" /> и путь <see cref="string" /> к ней.
        /// </summary>
        protected readonly struct ProcPath
        {
            /// <summary>
            ///     Хранимая карта.
            /// </summary>
            internal Processor CurrentProcessor { get; }

            /// <summary>
            ///     Путь к карте <see cref="Processor" />.
            /// </summary>
            internal string CurrentPath { get; }

            /// <summary>
            ///     Инициализирует хранимые объекты: <see cref="Processor" /> и <see cref="string" />.
            /// </summary>
            /// <param name="p">Хранимая карта.</param>
            /// <param name="path">Путь к карте <see cref="Processor" />.</param>
            internal ProcPath(Processor p, string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException($@"Параметр {nameof(path)} не может быть пустым.");
                CurrentProcessor =
                    p ?? throw new ArgumentNullException(nameof(p), $@"{nameof(p)} не может быть равен null.");
                CurrentPath = path;
            }
        }

        /// <summary>
        ///     Хранит карты, которые соответствуют одному значению хеша.
        /// </summary>
        readonly struct ProcHash
        {
            /// <summary>
            ///     Список хранимых карт, дающих одно значение хеша.
            /// </summary>
            readonly List<ProcPath> _lst;

            /// <summary>
            ///     Конструктор, который добавляет одну карту по умолчанию.
            ///     Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            internal ProcHash(ProcPath p)
            {
                if (p.CurrentProcessor is null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция (конструктор) {nameof(ProcHash)}.");
                _lst = new List<ProcPath> { p };
            }

            /// <summary>
            ///     Добавляет одну карту в коллекцию.
            ///     Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            internal void AddProcessor(ProcPath p)
            {
                if (p.CurrentProcessor is null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(AddProcessor)}.");
                _lst.Add(p);
            }

            /// <summary>
            ///     Получает все хранимые карты в текущем экземпляре <see cref="ProcHash" />.
            /// </summary>
            internal IEnumerable<ProcPath> Elements => _lst;

            /// <summary>
            ///     Получает <see cref="ProcHash" /> по указанному индексу.
            /// </summary>
            /// <param name="index">Индекс элемента <see cref="ProcHash" />, который требуется получиться.</param>
            /// <returns>Возвращает <see cref="ProcHash" /> по указанному индексу.</returns>
            internal ProcPath this[int index] => _lst[index];

            /// <summary>
            ///     Удаляет карту <see cref="Processor" />, с указанным индексом, из коллекции.
            ///     Недопустимые значения индекса игнорируются.
            /// </summary>
            /// <param name="index">Индекс удаляемой карты.</param>
            internal void RemoveProcessor(int index)
            {
                if (index > -1 && index < _lst.Count)
                    _lst.RemoveAt(index);
            }
        }

        /// <summary>
        ///     Предназначен для вычисления хеша определённой последовательности чисел типа <see cref="int" />.
        /// </summary>
        static class CRCIntCalc
        {
            /// <summary>
            ///     Таблица значений для расчёта хеша.
            ///     Вычисляется по алгоритму Далласа Максима (полином равен 49 (0x31).
            /// </summary>
            static readonly int[] Table;

            /// <summary>
            ///     Статический конструктор, рассчитывающий таблицу значений <see cref="Table" /> по алгоритму Далласа Максима (полином
            ///     равен 49 (0x31).
            /// </summary>
            static CRCIntCalc()
            {
                int[] numArray = new int[256];
                for (int index1 = 0; index1 < 256; ++index1)
                {
                    int num = index1;
                    for (int index2 = 0; index2 < 8; ++index2)
                        if ((uint)(num & 128) > 0U)
                            num = (num << 1) ^ 49;
                        else
                            num <<= 1;
                    numArray[index1] = num;
                }

                Table = numArray;
            }

            /// <summary>
            ///     Получает хеш заданной карты.
            ///     Карта не может быть равна <see langword="null" />.
            /// </summary>
            /// <param name="p">Карта, для которой необходимо вычислить значение хеша.</param>
            /// <returns>Возвращает хеш заданной карты.</returns>
            internal static int GetHash(Processor p)
            {
                if (p is null)
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(GetHash)}.");
                return GetHash(GetInts(p));
            }

            /// <summary>
            ///     Получает значения элементов карты построчно.
            /// </summary>
            /// <param name="p">Карта, с которой необходимо получить значения элементов.</param>
            /// <returns>Возвращает значения элементов карты построчно.</returns>
            static IEnumerable<int> GetInts(Processor p)
            {
                if (p is null)
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(GetInts)}.");
                for (int j = 0; j < p.Height; j++)
                    for (int i = 0; i < p.Width; i++)
                        yield return p[i, j].Value;
            }

            /// <summary>
            ///     Получает значение хеша для заданной последовательности целых чисел <see cref="int" />.
            /// </summary>
            /// <param name="ints">Последовательность, для которой необходимо рассчитать значение хеша.</param>
            /// <returns>Возвращает значение хеша для заданной последовательности целых чисел <see cref="int" />.</returns>
            static int GetHash(IEnumerable<int> ints)
            {
                if (ints is null)
                    throw new ArgumentNullException(nameof(ints),
                        $@"Для подсчёта контрольной суммы необходимо указать массив байт. Функция {nameof(GetHash)}.");
                return ints.Aggregate(255, (current, t) => Table[(byte)(current ^ t)]);
            }
        }
    }
}