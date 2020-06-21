using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Processor = DynamicParser.Processor;

namespace DynamicMosaicExample
{
    /// <summary>
    /// Потокобезопасное хранилище карт <see cref="Processor"/> с поддержкой поиска с использованием хеш-таблицы.
    /// Поддерживаются повторяющиеся ключи.
    /// </summary>
    sealed class ConcurrentProcessorStorage
    {
        /// <summary>
        /// Коллекция карт, идентифицируемых по хешу.
        /// </summary>
        readonly Dictionary<int, ProcHash> _dictionary = new Dictionary<int, ProcHash>();

        /// <summary>
        /// Коллекция карт, идентифицируемых по путям.
        /// </summary>
        readonly Dictionary<string, ProcPath> _dictionaryByPath = new Dictionary<string, ProcPath>();

        /// <summary>
        /// Содержит количество файлов с именами, начинающимися на одно и то же слово.
        /// </summary>
        readonly Dictionary<string, uint> _dictionaryFileNames = new Dictionary<string, uint>();

        /// <summary>
        /// Объект для синхронизации доступа к экземпляру класса <see cref="ConcurrentProcessorStorage"/>, с использованием конструкции <see langword="lock"/>.
        /// </summary>
        readonly object _syncObject = new object();

        /// <summary>
        /// Показывает, пригоден ли текущий экземпляр к выполнению каких-либо операций.
        /// Если нет, то при попытке выполнить операцию, выбрасывается исключение <see cref="InvalidOperationException"/>.
        /// </summary>
        internal bool IsOperationAllowed { get; private set; } = true;

        /// <summary>
        /// Хранит карту <see cref="Processor"/> и путь <see cref="string"/> к ней.
        /// </summary>
        struct ProcPath
        {
            /// <summary>
            /// Хранимая карта.
            /// </summary>
            internal Processor CurrentProcessor { get; }

            /// <summary>
            /// Путь к карте <see cref="Processor"/>.
            /// </summary>
            internal string CurrentPath { get; }

            /// <summary>
            /// Инициализирует хранимые объекты: <see cref="Processor"/> и <see cref="string"/>.
            /// </summary>
            /// <param name="p">Хранимая карта.</param>
            /// <param name="path">Путь к карте <see cref="Processor"/>.</param>
            internal ProcPath(Processor p, string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException($@"Параметр {nameof(path)} не может быть пустым.");
                CurrentProcessor = p ?? throw new ArgumentNullException(nameof(p), $@"{nameof(p)} не может быть равен null.");
                CurrentPath = path;
            }
        }

        /// <summary>
        /// Хранит карты, которые соответствуют одному значению хеша.
        /// </summary>
        struct ProcHash
        {
            /// <summary>
            /// Список хранимых карт, дающих одно значение хеша.
            /// </summary>
            readonly List<ProcPath> _lst;

            /// <summary>
            /// Конструктор, который добавляет одну карту по умолчанию.
            /// Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            internal ProcHash(ProcPath p)
            {
                if (p.CurrentProcessor is null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция (конструктор) {nameof(ProcHash)}.");
                _lst = new List<ProcPath> { p };
            }

            /// <summary>
            /// Добавляет одну карту в коллекцию.
            /// Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            internal void AddProcessor(ProcPath p)
            {
                if (p.CurrentProcessor is null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(AddProcessor)}.");
                _lst.Add(p);
            }

            /// <summary>
            /// Получает все хранимые карты в текущем экземпляре <see cref="ProcHash"/>.
            /// </summary>
            internal IEnumerable<ProcPath> Elements => _lst;

            /// <summary>
            /// Получает <see cref="ProcHash"/> по указанному индексу.
            /// </summary>
            /// <param name="index">Индекс элемента <see cref="ProcHash"/>, который требуется получиться.</param>
            /// <returns>Возвращает <see cref="ProcHash"/> по указанному индексу.</returns>
            internal ProcPath this[int index] => _lst[index];

            /// <summary>
            /// Удаляет карту <see cref="Processor"/>, с указанным индексом, из коллекции.
            /// Недопустимые значения индекса игнорируются.
            /// </summary>
            /// <param name="index">Индекс удаляемой карты.</param>
            internal void RemoveProcessor(int index)
            {
                if (index >= 0 && index < _lst.Count)
                    _lst.RemoveAt(index);
            }
        }

        /// <summary>
        /// Добавляет карту в коллекцию, по указанному пути.
        /// Если карта не подходит по каким-либо признакам, а в коллекции хранится карта по тому же пути, то она удаляется из коллекции.
        /// </summary>
        /// <param name="fullPath">Полный путь к изображению, которое будет интерпретировано как карта <see cref="Processor"/>.</param>
        internal void AddProcessor(string fullPath)
        {
            if (!IsOperationAllowed)
                throw new InvalidOperationException($@"{nameof(AddProcessor)}: Операция недопустима.");
            fullPath = Path.GetFullPath(fullPath);
            FileStream fs = null;
            for (int k = 0; k < 50; k++)
            {
                try
                {
                    fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception ex)
                {
                    if (k >= 49)
                        throw new FileNotFoundException($@"{nameof(AddProcessor)}: {ex.Message}", fullPath);
                    Thread.Sleep(100);
                    continue;
                }

                break;
            }
            Bitmap btm = null;
            using (fs)
                btm = new Bitmap(fs);
            ImageRect ir = new ImageRect(btm, Path.GetFileNameWithoutExtension(fullPath));
            if (!ir.IsSymbol)
            {
                RemoveProcessor(fullPath);
                return;
            }

            int hashCode = CRCIntCalc.GetHash(ir.CurrentProcessor);
            lock (_syncObject)
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(AddProcessor)}: Операция недопустима.");
                try
                {
                    if (!AddElement(hashCode, fullPath, ir.CurrentProcessor))
                        return;
                    if (!_dictionaryFileNames.TryGetValue(ir.SymbolicName, out uint value))
                        _dictionaryFileNames.Add(ir.SymbolicName, ir.Number);
                    else if (value < ir.Number)
                        _dictionaryFileNames[ir.SymbolicName] = ir.Number;
                }
                catch
                {
                    IsOperationAllowed = false;
                    throw;
                }
            }
        }

        /// <summary>
        /// Добавляет указанную карту <see cref="Processor"/> в <see cref="ConcurrentProcessorStorage"/>.
        /// Добавляет её в массив, содержащий хеши, и в массив, содержащий пути.
        /// Хеш добавляемой карты может совпадать с хешами других карт.
        /// Полный путь к добавляемой карте на достоверность не проверяется.
        /// Возвращает значение <see langword="true"/> в случае, если карта была добавлена в коллекцию, <see langword="false"/>, если карта была перезагружена.
        /// </summary>
        /// <param name="hashCode">Хеш добавляемой карты.</param>
        /// <param name="fullPath">Полный путь к добавляемой карте.</param>
        /// <param name="processor">Добавляемая карта <see cref="Processor"/>.</param>
        /// <returns>Возвращает значение <see langword="true"/> в случае, если карта была добавлена в коллекцию, <see langword="false"/>, если карта была перезагружена.</returns>
        bool AddElement(int hashCode, string fullPath, Processor processor)
        {
            string fPath = fullPath.ToLower();
            bool result = true;
            if (_dictionaryByPath.ContainsKey(fPath))
            {
                RemoveProcessor(fullPath);
                result = false;
            }

            _dictionaryByPath.Add(fPath, new ProcPath(processor, fullPath));
            if (!_dictionary.TryGetValue(hashCode, out ProcHash ph))
                _dictionary.Add(hashCode, new ProcHash(new ProcPath(processor, fullPath)));
            else if (ph.Elements.All(px => !ProcessorCompare(processor, px.CurrentProcessor)))
                ph.AddProcessor(new ProcPath(processor, fullPath));
            return result;
        }

        /// <summary>
        /// Преобразует название карты, заканчивающееся символами '0', в параметры, включающие имя и количество символов '0' в конце названия карты <see cref="Processor"/>.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag"/> карты <see cref="Processor"/>.</param>
        /// <returns>Возвращает параметры, включающие имя и количество символов '0' в конце названия карты <see cref="Processor"/>.</returns>
        static (bool result, uint count, string name) GetFilesNumberByName(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException(nameof(tag), nameof(GetFilesNumberByName));
            uint count = 0;
            int k = tag.Length - 1;
            for (; k > 0; k--)
            {
                if (tag[k] != '0')
                    break;
                if (count == uint.MaxValue)
                    return (false, 0, string.Empty);
                count++;
            }
            return (true, count, tag.Substring(0, k + 1));
        }

        /// <summary>
        /// Проверяет, содержится ли указанная карта в коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// В случае совпадения по значению ссылки или различиям по размерам карт, выдаётся исключение.
        /// Ни одна из сравниваемых карт не может быть <see langword="null" />.
        /// Значение свойства <see cref="Processor.Tag"/> сопоставляется только по первой букве, без учёта регистра.
        /// Возвращает значение <see langword="true" /> в случае, когда содержимое и первая буква свойства <see cref="Processor.Tag"/> совпадают, в противном случае - <see langword="false" />.
        /// </summary>
        /// <param name="p">Проверяемая карта.</param>
        /// <returns>Возвращает значение <see langword="true" /> в случае, когда содержимое и первая буква свойства <see cref="Processor.Tag"/> совпадают, в противном случае - <see langword="false" />.</returns>
        internal bool Contains(Processor p)
        {
            if (!IsOperationAllowed)
                throw new InvalidOperationException($@"{nameof(Contains)}: Операция недопустима.");
            if (p is null)
                throw new ArgumentNullException(nameof(p), $@"Функция {nameof(Contains)}.");
            int hash = CRCIntCalc.GetHash(p);
            lock (_syncObject)
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(Contains)}: Операция недопустима.");
                return _dictionary.TryGetValue(hash, out ProcHash ph) &&
                       ph.Elements.Any(px => ProcessorCompare(p, px.CurrentProcessor));
            }
        }

        /// <summary>
        /// Получает все элементы, добавленные в коллекцию <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        internal IEnumerable<Processor> Elements
        {
            get
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(Elements)}: Операция недопустима.");
                lock (_syncObject)
                {
                    if (!IsOperationAllowed)
                        throw new InvalidOperationException($@"{nameof(Elements)}: Операция недопустима.");
                    foreach (Processor processor in _dictionaryByPath.Values.Select(pair => pair.CurrentProcessor))
                        yield return processor;
                }
            }
        }

        /// <summary>
        /// Получает количество карт, содержащихся в коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        internal int Count
        {
            get
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(Count)}: Операция недопустима.");
                lock (_syncObject)
                {
                    if (!IsOperationAllowed)
                        throw new InvalidOperationException($@"{nameof(Count)}: Операция недопустима.");
                    return _dictionaryByPath.Count;
                }
            }
        }

        /// <summary>
        /// Возвращает карту <see cref="Processor"/> по указанному индексу, путь к ней, и количество карт в коллекции.
        /// Если индекс представляет собой недопустимое значение, возвращается (<see langword="null"/>, <see cref="string.Empty"/>, <see cref="Count"/>).
        /// </summary>
        /// <param name="index">Индекс карты <see cref="Processor"/>, которую надо вернуть.</param>
        /// <returns>Возвращает карту <see cref="Processor"/> по указанному индексу, путь к ней, и количество карт в коллекции.</returns>
        internal (Processor processor, string path, int count) this[int index]
        {
            get
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(ConcurrentProcessorStorage)}.indexer(int): Операция недопустима.");
                lock (_syncObject)
                {
                    if (!IsOperationAllowed)
                        throw new InvalidOperationException(
                            $@"{nameof(ConcurrentProcessorStorage)}.indexer(int): Операция недопустима.");
                    if (index >= 0 && index < Count)
                    {
                        ProcPath pp = _dictionaryByPath.Values.ElementAt(index);
                        return (pp.CurrentProcessor, pp.CurrentPath, Count);
                    }

                    return (null, string.Empty, Count);
                }
            }
        }

        /// <summary>
        /// Возвращает карту <see cref="Processor"/> по указанному пути, путь к ней, и количество карт в коллекции.
        /// От наличия файла на жёстком диске не зависит.
        /// Если карта не найдена, возвращается (<see langword="null"/>, <see cref="string.Empty"/>, <see cref="Count"/>).
        /// </summary>
        /// <param name="fullPath">Полный путь к карте <see cref="Processor"/>.</param>
        /// <returns>Возвращает карту <see cref="Processor"/> по указанному пути, путь к ней, и количество карт в коллекции.</returns>
        internal (Processor processor, string path, int count) this[string fullPath]
        {
            get
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(ConcurrentProcessorStorage)}.indexer(string): Операция недопустима.");
                if (string.IsNullOrWhiteSpace(fullPath))
                    return (null, string.Empty, Count);
                lock (_syncObject)
                {
                    if (!IsOperationAllowed)
                        throw new InvalidOperationException($@"{nameof(ConcurrentProcessorStorage)}.indexer(string): Операция недопустима.");
                    _dictionaryByPath.TryGetValue(fullPath.ToLower(), out ProcPath p);
                    return (p.CurrentProcessor, string.IsNullOrEmpty(p.CurrentPath) ? string.Empty : p.CurrentPath, Count);
                }
            }
        }

        /// <summary>
        /// Получает указанную или последнюю карту в случае недопустимого значения в индексе карты <see cref="Processor"/>, которую необходимо получить.
        /// Актуализирует номер полученной карты и путь к ней.
        /// Получает количество карт в коллекции <see cref="ConcurrentProcessorStorage"/> на момент получения карты.
        /// В случае отсутствия карт в коллекции, возвращается (<see langword="null"/>, 0), index тоже будет равен нолю.
        /// </summary>
        /// <param name="index">Индекс карты <see cref="Processor"/>, которую необходимо получить. В случае допустимого изначального значения, это значение остаётся прежним, иначе равняется индексу последней карты в коллекции.</param>
        /// <returns>Возвращает карту, путь к ней, и количество карт на момент её получения.</returns>
        internal (Processor processor, string path, int count) GetLastProcessor(ref int index)
        {
            if (!IsOperationAllowed)
                throw new InvalidOperationException($@"{nameof(GetLastProcessor)}: Операция недопустима.");
            lock (_syncObject)
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(GetLastProcessor)}: Операция недопустима.");
                int count = Count;
                if (count <= 0)
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
        /// Получает указанную или первую карту в случае недопустимого значения в индексе карты <see cref="Processor"/>, которую необходимо получить.
        /// Актуализирует номер полученной карты и путь к ней.
        /// Получает количество карт в коллекции <see cref="ConcurrentProcessorStorage"/> на момент получения карты.
        /// В случае отсутствия карт в коллекции, возвращается (<see langword="null"/>, 0), index тоже будет равен нолю.
        /// </summary>
        /// <param name="index">Индекс карты <see cref="Processor"/>, которую необходимо получить. В случае допустимого изначального значения, это значение остаётся прежним, иначе равняется индексу первой карты в коллекции.</param>
        /// <returns>Возвращает карту, путь к ней, и количество карт на момент её получения.</returns>
        internal (Processor processor, string path, int count) GetFirstProcessor(ref int index)
        {
            if (!IsOperationAllowed)
                throw new InvalidOperationException($@"{nameof(GetFirstProcessor)}: Операция недопустима.");
            lock (_syncObject)
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(GetFirstProcessor)}: Операция недопустима.");
                int count = Count;
                if (count <= 0)
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
        /// Удаляет указанную карту <see cref="Processor"/> из коллекции <see cref="ConcurrentProcessorStorage"/>, идентифицируя её по пути к ней.
        /// </summary>
        /// <param name="fullPath">Полный путь к карте <see cref="Processor"/>, которую необходимо удалить из коллекции.</param>
        internal void RemoveProcessor(string fullPath)
        {
            if (!IsOperationAllowed)
                throw new InvalidOperationException($@"{nameof(RemoveProcessor)}: Операция недопустима.");
            if (string.IsNullOrWhiteSpace(fullPath))
                return;
            lock (_syncObject)
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(RemoveProcessor)}: Операция недопустима.");
                RemoveProcessor(this[fullPath.ToLower()].processor);
            }
        }

        /// <summary>
        /// Удаляет указанную карту <see cref="Processor"/> из коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor"/>, которую следует удалить.</param>
        void RemoveProcessor(Processor processor)
        {
            if (!IsOperationAllowed)
                throw new InvalidOperationException($@"{nameof(RemoveProcessor)}: Операция недопустима.");
            if (processor is null)
                return;
            int hash = CRCIntCalc.GetHash(processor);
            lock (_syncObject)
            {
                if (!IsOperationAllowed)
                    throw new InvalidOperationException($@"{nameof(RemoveProcessor)}: Операция недопустима.");
                if (!_dictionary.TryGetValue(hash, out ProcHash ph))
                    return;
                try
                {
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
                catch
                {
                    IsOperationAllowed = false;
                    throw;
                }
            }
        }

        /// <summary>
        /// Сранивает содержимое двух карт.
        /// Возвращает значение <see langword="true" /> в случае, когда содержимое и первая буква свойства <see cref="Processor.Tag"/> совпадают, в противном случае - <see langword="false" />.
        /// В случае совпадения по значению ссылки или различиям по размерам карт, выдаётся исключение.
        /// Ни одна из сравниваемых карт не может быть <see langword="null" />.
        /// Значение свойства <see cref="Processor.Tag"/> сопоставляется только по первой букве, без учёта регистра.
        /// </summary>
        /// <param name="pOne">Сравниваемая карта.</param>
        /// <param name="pTwo">Сравниваемая карта.</param>
        /// <returns>Возвращает значение <see langword="true" /> в случае, когда содержимое и первая буква свойства <see cref="Processor.Tag"/> совпадают, в противном случае - <see langword="false" />.</returns>
        static bool ProcessorCompare(Processor pOne, Processor pTwo)
        {
            if (ReferenceEquals(pOne, pTwo))
                throw new ArgumentException($@"Ссылки на сравниваемые карты не могут быть равны. Функция {nameof(ProcessorCompare)}.");
            if (pOne is null)
                throw new ArgumentNullException(nameof(pOne), $@"Функция {nameof(ProcessorCompare)}.");
            if (pTwo is null)
                throw new ArgumentNullException(nameof(pTwo), $@"Функция {nameof(ProcessorCompare)}.");
            if (pOne.Width != pTwo.Width || pOne.Height != pTwo.Height)
                throw new ArgumentException($@"Сравниваемые карты не равны по размерам. Функция {nameof(ProcessorCompare)}.");
            if (char.ToUpper(pOne.Tag[0]) != char.ToUpper(pTwo.Tag[0]))
                return false;
            for (int y = 0; y < pOne.Height; y++)
                for (int x = 0; x < pOne.Width; x++)
                    if (pOne[x, y] != pTwo[x, y])
                        return false;
            return true;
        }

        /// <summary>
        ///     Сохраняет указанную карту <see cref="Processor"/> на жёсткий диск в формате BMP.
        ///     Если карта содержит в конце названия ноли, то метод преобразует их в число, отражающее их количество.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor"/>, которую требуется сохранить.</param>
        internal static void SaveToFile(Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");
            (bool res, uint count, string name) = GetFilesNumberByName(processor.Tag);
            if (!res)
                throw new Exception($@"{nameof(SaveToFile)}: Счётчик количества файлов дошёл до максимума.");
            SaveToFile(ImageRect.GetBitmap(processor), $@"{name + count}");
        }

        /// <summary>
        /// Сохраняет указанное изображение на жёсткий диск.
        /// </summary>
        /// <param name="btm">Изображение, которое требуется сохранить.</param>
        /// <param name="path">Путь, по которому требуется сохранить изображение.</param>
        internal static void SaveToFile(Bitmap btm, string path)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(SaveToFile)}: Необходимо указать изображение, которое требуется сохранить.");
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException($@"{nameof(SaveToFile)}: Имя не задано.", nameof(path));
            path = Path.ChangeExtension(path, string.Empty);
            string resultTmp = Path.Combine(FrmExample.SearchPath, $@"{path}bmpTMP");
            string result = Path.Combine(FrmExample.SearchPath, $@"{path}{FrmExample.ExtImg}");
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
                throw new Exception($@"{nameof(SaveToFile)}: Попытка перезаписать существующий файл: {result}: {ex.Message}");
            }
        }

        /// <summary>
        /// Предназначен для вычисления хеша определённой последовательности чисел типа <see cref="int"/>.
        /// </summary>
        static class CRCIntCalc
        {
            /// <summary>
            /// Получает хеш заданной карты.
            /// Карта не может быть равна <see langword="null" />.
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
            /// Получает значения элементов карты построчно.
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
            /// Получает значение хеша для заданной последовательности целых чисел <see cref="int"/>.
            /// </summary>
            /// <param name="ints">Последовательность, для которой необходимо рассчитать значение хеша.</param>
            /// <returns>Возвращает значение хеша для заданной последовательности целых чисел <see cref="int"/>.</returns>
            static int GetHash(IEnumerable<int> ints)
            {
                if (ints is null)
                    throw new ArgumentNullException(nameof(ints), $@"Для подсчёта контрольной суммы необходимо указать массив байт. Функция {nameof(GetHash)}.");
                return ints.Aggregate(255, (current, t) => Table[(byte)(current ^ t)]);
            }

            /// <summary>
            /// Таблица значений для расчёта хеша.
            /// Вычисляется по алгоритму Далласа Максима (полином равен 49 (0x31).
            /// </summary>
            static readonly int[] Table;

            /// <summary>
            /// Статический конструктор, рассчитывающий таблицу значений <see cref="Table"/> по алгоритму Далласа Максима (полином равен 49 (0x31).
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
        }
    }
}
