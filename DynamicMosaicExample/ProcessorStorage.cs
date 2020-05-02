using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        readonly Dictionary<string, ImageRect> _dictionaryByPath = new Dictionary<string, ImageRect>();

        /// <summary>
        /// Содержит количество файлов с именами, начинающимися на одно и то же слово.
        /// </summary>
        readonly Dictionary<string, uint> _dictionaryFileNames = new Dictionary<string, uint>();

        /// <summary>
        /// Объект для синхронизации доступа к экземпляру класса <see cref="ConcurrentProcessorStorage"/>, с использованием конструкции <see langword="lock"/>.
        /// </summary>
        readonly object _syncObject = new object();

        /// <summary>
        /// Хранит карту <see cref="Processor"/> и путь <see cref="string"/> к ней.
        /// </summary>
        struct ProcPath
        {
            /// <summary>
            /// Хранимая карта.
            /// </summary>
            public Processor CurrentProcessor { get; }

            /// <summary>
            /// Путь к карте <see cref="Processor"/>.
            /// </summary>
            public string CurrentPath { get; }

            /// <summary>
            /// Инициализирует хранимые объекты: <see cref="Processor"/> и <see cref="string"/>.
            /// </summary>
            /// <param name="p">Хранимая карта.</param>
            /// <param name="path">Путь к карте <see cref="Processor"/>.</param>
            public ProcPath(Processor p, string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException($@"Параметр {nameof(path)} не может быть пустым.");
                CurrentProcessor = p ?? throw new ArgumentNullException(nameof(p), $@"{nameof(p)} не может быть равен null."); ;
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
            public ProcHash(ProcPath p)
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
            public void AddProcessor(ProcPath p)
            {
                if (p.CurrentProcessor is null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(AddProcessor)}.");
                _lst.Add(p);
            }

            /// <summary>
            /// Получает все хранимые карты в текущем экземпляре <see cref="ProcHash"/>.
            /// </summary>
            public IEnumerable<ProcPath> Elements => _lst;

            /// <summary>
            /// Получает <see cref="ProcHash"/> по указанному индексу.
            /// </summary>
            /// <param name="index">Индекс элемента <see cref="ProcHash"/>, который требуется получиться.</param>
            /// <returns>Возвращает <see cref="ProcHash"/> по указанному индексу.</returns>
            public ProcPath this[int index] => _lst[index];

            /// <summary>
            /// Удаляет одну карту <see cref="Processor"/> из коллекции.
            /// Недопустимые значения игнорируются.
            /// </summary>
            /// <param name="index">Индекс удаляемой карты.</param>
            public void RemoveProcessor(int index)
            {
                if (index >= 0 && index < _lst.Count)
                    _lst.RemoveAt(index);
            }
        }

        /// <summary>
        /// Добавляет карту в коллекцию, по указанному пути.
        /// </summary>
        /// <param name="fullPath">Полный путь к изображению, которое будет интерпретировано как карта <see cref="Processor"/>.</param>
        public ImageRect AddProcessor(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);
            ImageRect ir = null;
            using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                ir = new ImageRect(new Bitmap(fs), Path.GetFileNameWithoutExtension(fullPath), fullPath);
            if (!ir.IsSymbol)
                return null;
            int hashCode = CRCIntCalc.GetHash(ir.CurrentProcessor);
            lock (_syncObject)
            {
                if (!_dictionaryFileNames.TryGetValue(ir.SymbolString, out uint value))
                    _dictionaryFileNames[ir.SymbolString] = ir.Number;
                else
                if (value < ir.Number)
                    _dictionaryFileNames[ir.SymbolString] = ir.Number;
                if (!_dictionary.TryGetValue(hashCode, out ProcHash ph))
                    _dictionary.Add(hashCode, new ProcHash(new ProcPath(ir.CurrentProcessor, fullPath)));
                else if (ph.Elements.All(px => !ProcessorCompare(ir.CurrentProcessor, px.CurrentProcessor)))
                    ph.AddProcessor(new ProcPath(ir.CurrentProcessor, fullPath));
                _dictionaryByPath.Add(fullPath, ir);
            }
            return ir;
        }

        /// <summary>
        /// Преобразует название карты, заканчивающееся символами '0', в параметры, включающие имя и количество символов '0' в конце названия карты <see cref="Processor"/>.
        /// </summary>
        /// <param name="tag">Значение свойства <see cref="Processor.Tag"/> карты <see cref="Processor"/>.</param>
        /// <returns>Возвращает параметры, включающие имя и количество символов '0' в конце названия карты <see cref="Processor"/>.</returns>
        internal static (uint count, string name) GetFilesNumberByName(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException(nameof(tag), nameof(GetFilesNumberByName));
            uint count = 0;
            int k = tag.Length - 1;
            for (; k > 0; k--)
            {
                if (tag[k] != '0')
                    break;
                count++;
            }
            return (count, tag.Substring(0, k + 1));
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
        public bool Contains(Processor p)
        {
            if (p is null)
                throw new ArgumentNullException(nameof(p), $@"Функция {nameof(Contains)}.");
            int hash = CRCIntCalc.GetHash(p);
            lock (_syncObject)
                return _dictionary.TryGetValue(hash, out ProcHash ph) && ph.Elements.Any(px => ProcessorCompare(p, px.CurrentProcessor));
        }

        /// <summary>
        /// Получает все элементы, добавленные в коллекцию <see cref="ConcurrentProcessorStorage"/>.
        /// Возвращает копию коллекции в виде массива.
        /// </summary>
        public Processor[] Elements
        {
            get
            {
                lock (_syncObject)
                    return _dictionaryByPath.Select(pair => pair.Value.CurrentProcessor).ToArray();
            }
        }

        /// <summary>
        /// Находит карту <see cref="Processor"/> по указанному пути.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Processor FindByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            lock (_syncObject)
                return _dictionaryByPath[path].CurrentProcessor;
        }

        /// <summary>
        /// Удаляет указанную карту <see cref="Processor"/> из коллекции <see cref="ConcurrentProcessorStorage"/>, идентифицируя её по пути к ней.
        /// </summary>
        /// <param name="path">Путь к карте <see cref="Processor"/>, которую необходимо удалить из коллекции.</param>
        public void RemoveProcessor(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            lock (_syncObject)
                RemoveProcessor(FindByPath(path));
        }

        /// <summary>
        /// Удаляет карту <see cref="Processor"/> из коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        /// <param name="p">Карта <see cref="Processor"/>, которую следует удалить.</param>
        public void RemoveProcessor(Processor p)
        {
            if (p == null)
                return;
            int hash = CRCIntCalc.GetHash(p);
            lock (_syncObject)
            {
                if (!_dictionary.TryGetValue(hash, out ProcHash ph))
                    return;
                int index = 0;
                foreach (ProcPath px in ph.Elements)
                    if (ProcessorCompare(p, px.CurrentProcessor))
                    {
                        _dictionaryByPath.Remove(ph[index].CurrentPath);
                        ph.RemoveProcessor(index);
                    }
                    else
                        index++;
                if (!ph.Elements.Any())
                    _dictionary.Remove(hash);
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
        ///     Сохраняет указанный образ буквы с указанным названием.
        /// </summary>
        /// <param name="name">Название буквы.</param>
        /// <param name="btm">Изображение буквы.</param>
        /// <returns>Возвращает экземпляр текущего класса образа буквы.</returns>
        internal ImageRect Save(string name, Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(Save)}: Сохраняемое изображение не указано.");
            ImageRect ir = new ImageRect(btm, name, SaveFile(name, btm));
            if (!ir.IsSymbol)
                throw new Exception($"{nameof(Save)}: Неизвестная ошибка при сохранении изображения.");
            return ir;
        }

        /// <summary>
        ///     Генерирует имя нового образа, увеличивая его номер.
        /// </summary>
        /// <param name="imageName">Имя образа, на основании которого требуется сгенерировать новое имя.</param>
        /// <param name="btm">Изображение буквы.</param>
        /// <returns>Возвращает строку полного пути к файлу нового образа.</returns>
        string SaveFile(string imageName, Bitmap btm)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                throw new ArgumentNullException(nameof(imageName), nameof(SaveFile));
            (_, string name) = GetFilesNumberByName(imageName);
            lock (_syncObject)
            {
                if (_dictionaryFileNames.TryGetValue(name, out uint value))
                    value = unchecked(value + 1);
                else
                    value = 1;
                using (FileStream fs = new FileStream(Path.Combine(FrmExample.SearchPath, $@"{name}{unchecked(value)}.{FrmExample.ExtImg}"), FileMode.Create, FileAccess.Write))//ПРОВЕРИТЬ на перезапись существующего файла
                    btm.Save(fs, ImageFormat.Bmp);
                _dictionaryFileNames[name] = value;
            }
            return Path.GetFullPath(imageName);
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
            public static int GetHash(Processor p)
            {
                if (p == null)
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
                if (p == null)
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
                if (ints == null)
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
