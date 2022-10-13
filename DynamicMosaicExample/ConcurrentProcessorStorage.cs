using System;
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
        public enum ProcessorStorageType
        {
            IMAGE,
            RECOGNIZE
        }

        public abstract Processor GetAddingProcessor(string fullPath);

        public abstract string GetProcessorTag(string fullPath);

        public abstract string ImagesPath { get; }

        public abstract ProcessorStorageType StorageType { get; }

        protected static string GetImagePath(string sourcePath, string name) => $@"{Path.Combine(sourcePath ?? throw new InvalidOperationException($@"{nameof(GetImagePath)}: Исходный путь образа не указан."), name)}.{FrmExample.ExtImg}";

        internal abstract (Bitmap, string) SaveToFile(Processor processor, string relativeFolderPath);

        protected (Processor processor, string path) AddTagToSet(ISet<string> tagSet, Processor p, (string tag, ulong? number) tn, string pathToSave)
        {
            unchecked
            {
                bool isDirectory = CreateWorkingPath(ref pathToSave);

                ulong k = tn.number ?? 0, mk = k;

                do
                {
                    string t = $@"{tn.tag}{ImageRect.TagSeparatorChar}{k}";

                    if (!tagSet.Add(t))
                        continue;

                    string path;

                    if (string.IsNullOrEmpty(pathToSave))
                        path = GetImagePath(ImagesPath, t);
                    else
                        path = isDirectory ? GetImagePath(pathToSave, t) : pathToSave;

                    return (ProcessorHandler.ChangeProcessorTag(p, t), path);
                } while (++k != mk);

                string n = tn.number is null ? "<пусто>" : tn.number.ToString();
                throw new Exception($@"Нет свободного места для добавления карты в коллекцию: {p.Tag} по пути {ImagesPath}, изначальное имя карты {tn.tag}, номер {n}.");
            }
        }

        public string CombinePaths(string folderName, string fileName) => Path.Combine(ImagesPath, ReplaceInvalidPathChars(folderName), ReplaceInvalidPathChars(fileName));

        static string ReplaceInvalidPathChars(string path)
        {
            HashSet<char> lstChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (char c in Path.GetInvalidPathChars())
                lstChars.Add(c);

            return lstChars.Aggregate(path, (current, c) => current.Replace(c, '_'));
        }

        /// <summary>
        ///     Коллекция карт, идентифицируемых по хешу.
        /// </summary>
        readonly Dictionary<int, ProcHash> _dictionaryByHash = new Dictionary<int, ProcHash>();

        /// <summary>
        ///     Коллекция карт, идентифицируемых по путям.
        /// </summary>
        protected readonly Dictionary<string, ProcPath> _dictionaryByKey = new Dictionary<string, ProcPath>();

        /// <summary>
        ///     Объект для синхронизации доступа к экземпляру класса <see cref="ConcurrentProcessorStorage" />, с использованием
        ///     конструкции <see langword="lock" />.
        /// </summary>
        protected readonly object _syncObject = new object();

        string _savedRecognizePath = string.Empty;

        /// <summary>
        ///     Получает все элементы, добавленные в коллекцию <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        internal IEnumerable<(Processor processor, string sourcePath)> Elements
        {
            get
            {
                lock (_syncObject)
                {
                    foreach (ProcPath p in _dictionaryByKey.Values)
                        yield return (p.CurrentProcessor, p.CurrentPath);
                }
            }
        }

        protected HashSet<string> NamesToSave
        {
            get
            {
                lock (_syncObject)
                {
                    HashSet<string> tagSet = new HashSet<string>();

                    foreach (ProcPath p in _dictionaryByKey.Values)
                    {
                        (ulong? number, string strPart) = ImageRect.NameParser(Path.GetFileNameWithoutExtension(p.CurrentPath));
                        AddTagToSet(tagSet, p.CurrentProcessor, (strPart, number), string.Empty);
                    }

                    return tagSet;
                }
            }
        }

        public bool IsWorkingPath(string path, bool isEqual = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException($@"{nameof(IsWorkingPath)}: Необходимо указать путь для проверки.", nameof(path));

            string p = AddEndingSlash(path), ip = AddEndingSlash(ImagesPath);

            return isEqual ? string.Compare(p, ip, StringComparison.OrdinalIgnoreCase) == 0 : p.StartsWith(ip, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDirectorySeparatorSymbol(char c) => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;

        public static string AddEndingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            if (!IsDirectorySeparatorSymbol(path[path.Length - 1]))
                path += Path.DirectorySeparatorChar;

            return path;
        }

        public bool CreateWorkingPath(ref string relativeFolderPath)
        {
            if (string.IsNullOrEmpty(relativeFolderPath))
            {
                relativeFolderPath = string.Empty;
                return false;
            }

            if (!IsWorkingPath(relativeFolderPath))
                throw new ArgumentException($@"Необходимо нахождение пути в рабочем каталоге ({ImagesPath})", nameof(relativeFolderPath));

            string ext = Path.GetExtension(relativeFolderPath);

            if (string.IsNullOrEmpty(ext))
            {
                Directory.CreateDirectory(relativeFolderPath = AddEndingSlash(relativeFolderPath));
                return true;
            }

            if (string.Compare(ext, $".{FrmExample.ExtImg}", StringComparison.OrdinalIgnoreCase) != 0)
                throw new ArgumentException($@"Необходимо, чтобы путь вёл к файлу с требуемым расширением ({FrmExample.ExtImg})", nameof(relativeFolderPath));

            return false;
        }

        /// <summary>
        ///     Получает количество карт, содержащихся в коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        internal int Count
        {
            get
            {
                lock (_syncObject)
                    return _dictionaryByKey.Count;
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

                    ProcPath pp = _dictionaryByKey.Values.ElementAt(index);
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
                    _dictionaryByKey.TryGetValue(GetStringKey(fullPath), out ProcPath p);
                    return (p.CurrentProcessor, p.CurrentPath, Count);
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

            int hashCode = GetHashKey(addingProcessor);
            lock (_syncObject)
                AddElement(hashCode, fullPath, addingProcessor);
        }

        static Bitmap CheckBitmapByAlphaColor(Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), @"Изображение должно быть указано.");

            for (int y = 0; y < btm.Height; y++)
                for (int x = 0; x < btm.Width; x++)
                    FrmExample.CheckAlphaColor(btm.GetPixel(x, y));

            return btm;
        }

        static Bitmap CheckImageFormat(Bitmap btm)
        {
            ImageFormat iformat = btm.RawFormat;

            if (!iformat.Equals(ImageFormat.Bmp))
                throw new FormatException($@"Загружаемое изображение не подходит по формату: {iformat}; необходимо: {ImageFormat.Bmp}.");

            return btm;
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

            if (fs == null)
                throw new InvalidOperationException($@"{nameof(LoadBitmap)}: {nameof(fs)} == null.");

            using (fs)
            {
                Bitmap btm = new Bitmap(fs);

                try
                {
                    return CheckImageFormat(CheckBitmapByAlphaColor(btm));
                }
                catch
                {
                    btm.Dispose();
                    throw;
                }
            }
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
            string key = GetStringKey(fullPath);
            if (_dictionaryByKey.ContainsKey(key))
                RemoveProcessor(fullPath);

            _dictionaryByKey.Add(key, new ProcPath(processor, fullPath));
            if (_dictionaryByHash.TryGetValue(hashCode, out ProcHash ph))
                ph.AddProcessor(new ProcPath(processor, fullPath));
            else
                _dictionaryByHash.Add(hashCode, new ProcHash(new ProcPath(processor, fullPath)));
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

                _savedRecognizePath = path;

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

                _savedRecognizePath = path;

                return (processor, path, count);
            }
        }

        internal void RemoveProcessor()
        {
            lock (_syncObject)
            {
                if (string.IsNullOrEmpty(_savedRecognizePath))
                    return;

                RemoveProcessor(this[GetStringKey(_savedRecognizePath)].processor);
            }
        }

        /// <summary>
        ///     Удаляет указанную карту <see cref="Processor" /> из коллекции <see cref="ConcurrentProcessorStorage" />,
        ///     идентифицируя её по пути к ней.
        /// </summary>
        /// <param name="fullPath">Полный путь к карте <see cref="Processor" />, которую необходимо удалить из коллекции.</param>
        internal void RemoveProcessor(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return;

            lock (_syncObject)
                RemoveProcessor(this[GetStringKey(fullPath)].processor);
        }

        static string GetStringKey(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException(@"Для получения ключа карты необходимо указать путь к ней.", nameof(path));

            return path.ToLower();
        }

        static int GetHashKey(Processor processor)
        {
            if (processor is null)
                throw new ArgumentNullException(nameof(processor), @"Для получения хеша карты необходимо её указать.");

            return CRCIntCalc.GetHash(processor);
        }

        /// <summary>
        ///     Удаляет указанную карту <see cref="Processor" /> из коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую следует удалить.</param>
        void RemoveProcessor(Processor processor)
        {
            if (processor is null)
                return;
            int hashCode = GetHashKey(processor);
            lock (_syncObject)
            {
                if (!_dictionaryByHash.TryGetValue(hashCode, out ProcHash ph))
                    return;
                int index = 0;
                foreach (ProcPath px in ph.Elements)
                    if (ReferenceEquals(processor, px.CurrentProcessor))
                    {
                        string path = ph[index].CurrentPath;
                        _dictionaryByKey.Remove(GetStringKey(path));
                        ph.RemoveProcessor(index);
                        if (string.Compare(path, _savedRecognizePath, StringComparison.OrdinalIgnoreCase) == 0)
                            _savedRecognizePath = string.Empty;
                        break;
                    }
                    else
                        index++;

                if (!ph.Elements.Any())
                    _dictionaryByHash.Remove(hashCode);
            }
        }

        public void Clear()
        {
            lock (_syncObject)
            {
                _dictionaryByKey.Clear();
                _dictionaryByHash.Clear();
                _savedRecognizePath = string.Empty;
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