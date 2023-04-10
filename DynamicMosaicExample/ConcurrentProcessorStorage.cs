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
    ///     Потокобезопасное хранилище карт <see cref="Processor" />.
    /// </summary>
    /// <remarks>
    ///     Позволяет идентифицировать карты как по путям хранения, так и по индексу.
    ///     Предоставляет следующие функции для управления хранилищем.
    ///     1) Сохраняет карты на жёсткий диск (номерует их при совпадении названий).
    ///     2) Позволяет загружать карты как массово (путь к папке с картами), так и по одиночке.
    ///     3) Позволяет перечислять карты с уникальными именами или "как есть".
    /// </remarks>
    public abstract class ConcurrentProcessorStorage
    {
        /// <summary>
        ///     Тип используемого хранилища.
        /// </summary>
        public enum ProcessorStorageType
        {
            /// <summary>
            ///     Хранилище карт, которые требуется найти.
            /// </summary>
            IMAGE,

            /// <summary>
            ///     Хранилище карт, которые требуется исследовать последством карт <see cref="IMAGE" />.
            /// </summary>
            RECOGNIZE
        }

        /// <summary>
        /// Хранит значение свойства <see cref="LongOperationsAllowed"/>.
        /// Значение по умолчанию - <see langword="true" />.
        /// </summary>
        static bool _longOperationsAllowed = true;

        /// <summary>
        /// Синхронизирует доступ к значению <see cref="_longOperationsAllowed"/> при изменении статического свойства <see cref="LongOperationsAllowed"/>.
        /// </summary>
        static readonly object LongOperationsSync = new object();

        /// <summary>
        ///     Коллекция карт, идентифицируемых по хешу.
        /// </summary>
        protected readonly Dictionary<int, ProcHash> DictionaryByHash = new Dictionary<int, ProcHash>();

        /// <summary>
        ///     Коллекция карт, идентифицируемых по ключам. Ключ представляет собой путь в LowerCase формате.
        /// </summary>
        protected readonly Dictionary<string, ProcPath> DictionaryByKey = new Dictionary<string, ProcPath>();

        /// <summary>
        ///     Объект для синхронизации доступа к экземпляру класса <see cref="ConcurrentProcessorStorage" />, с использованием
        ///     конструкции <see langword="lock" />.
        /// </summary>
        /// <remarks>
        /// С целью исключения возможных взаимоблокировок при обращении к нему нескольких потоков, доступ к объекту <see cref="ConcurrentProcessorStorage" /> синхронизируется только с помощью этого объекта.
        /// </remarks>
        protected readonly object SyncObject = new object();

        /// <summary>
        /// Хранит значение свойства <see cref="SelectedPath"/>.
        /// Значение по умолчанию - <see cref="string.Empty"/>.
        /// </summary>
        string _selectedPath = string.Empty;

        /// <summary>
        /// Хранит значение для свойства <see cref="SelectedIndex"/>.
        /// Значение по умолчанию -1.
        /// </summary>
        protected int IntSelectedIndex = -1;

        /// <summary>
        ///     Внутренний конструтор, используется для передачи значений внутренним переменным.
        /// </summary>
        /// <param name="extImg">Расширение файлов с изображениями. Любой регистр, без точки.</param>
        protected ConcurrentProcessorStorage(string extImg)
        {
            ExtImg = extImg;
        }

        /// <summary>
        ///     Получает расширение файлов с изображениями, с которым работает текущий экземпляр.
        /// </summary>
        public string ExtImg { get; }

        /// <summary>
        ///     Рабочий каталог. Зависит от типа хранилища. Реализации по умолчанию нет.
        /// </summary>
        /// <remarks>
        /// Используется для определения того, является ли указанный каталог рабочим или нет.
        /// При выполнении различных операций с картами, в зависимости от этого параметра, принимаются те или иные решения.
        /// Например, определение типа хранилища, исходя из сведений о пути к его рабочему каталогу.
        /// Этот каталог существует всегда. Если не существует, должен быть создан с помощью метода <see cref="CreateWorkingDirectory()" /> или создастся автоматически, при вызове соответствующего метода.
        /// В него всегда сохраняются карты, которые требуется сохранить.
        /// </remarks>
        public abstract string WorkingDirectory { get; }

        /// <summary>
        ///     Текущий тип хранилища.
        /// </summary>
        public abstract ProcessorStorageType StorageType { get; }

        /// <summary>
        ///     Получает все элементы, добавленные в коллекцию <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        public IEnumerable<Processor> Elements
        {
            get
            {
                lock (SyncObject)
                {
                    return DictionaryByKey.Values.Select(p => p.CurrentProcessor);
                }
            }
        }

        /// <summary>
        ///     Получает количество карт, содержащихся в коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        /// <remarks>
        /// Если требуется узнать, является ли коллекция пустой или нет, можно воспользоваться свойством <see cref="IsEmpty"/>.
        /// </remarks>
        /// <seealso cref="IsEmpty"/>
        public int Count
        {
            get
            {
                lock (SyncObject)
                {
                    return DictionaryByKey.Count;
                }
            }
        }

        /// <summary>
        /// Определяет, содержит ли коллекция <see cref="ConcurrentProcessorStorage" /> какие-либо элементы.
        /// </summary>
        /// <seealso cref="Count"/>
        public bool IsEmpty
        {
            get
            {
                lock (SyncObject)
                {
                    return !DictionaryByKey.Any();
                }
            }
        }

        /// <summary>
        ///     Получает карту по указанному индексу.
        /// </summary>
        /// <param name="index">Индекс карты, которую требуется получить.</param>
        /// <returns>Возвращает карту <see cref="Processor" /> по указанному индексу, путь к ней, и количество карт в коллекции.</returns>
        /// <remarks>
        /// Если индекс представляет собой недопустимое значение, будут возвращены следующие значения: (<see langword="null" />, <see cref="string.Empty" />, <see cref="Count" />).
        /// </remarks>
        (Processor processor, string path, int count) this[int index]
        {
            get
            {
                lock (SyncObject)
                {
                    int count = Count;

                    if (index < 0 || index >= count)
                        return (null, string.Empty, count);

                    ProcPath pp = DictionaryByKey.Values.ElementAt(index);
                    return (pp.CurrentProcessor, pp.CurrentPath, count);
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
        public (Processor processor, string path, int count) this[string fullPath]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(fullPath))
                    return (null, string.Empty, Count);

                lock (SyncObject)
                {
                    return DictionaryByKey.TryGetValue(GetStringKey(fullPath), out ProcPath p) ?
                        (p.CurrentProcessor, p.CurrentPath, Count) :
                        (null, string.Empty, Count);
                }
            }
        }

        /// <summary>
        /// Получает путь к последней карте, к которой было обращение с помощью различных методов (добавление, сохранение, выборка по индексу (<see cref="GetFirstProcessor"/> или <see cref="GetLatestProcessor"/>).
        /// С помощью свойства <see cref="SelectedIndex"/> можно получить её индекс в коллекции.
        /// </summary>
        /// <remarks>
        /// Является потокобезопасным.
        /// Значение этого свойства остаётся неизменным до тех пор, пока не будет изменено или сброшено либо добавлением (сохранением) карты, либо удалением текущей карты, в том числе, очисткой всей коллекции или обращением по индексу (<see cref="GetFirstProcessor"/> или <see cref="GetLatestProcessor"/>).
        /// Если требуется узнать, выбрана ли какая-либо карта на данный момент, можно воспользоваться свойством <see cref="IsSelectedOne"/>.
        /// Доступ на запись возможен только изнутри этого класса, и служит для актуализации значения этого свойства с последующим сбросом значения свойства <see cref="SelectedIndex"/>.
        /// Чтение и запись производятся с блокировками. Если свойство сброшено, его значением будет <see cref="string.Empty" />.
        /// </remarks>
        /// <seealso cref="SelectedIndex"/>
        /// <seealso cref="IsSelectedOne"/>
        public string SelectedPath
        {
            get
            {
                lock (SyncObject)
                {
                    return _selectedPath;
                }
            }

            protected set
            {
                lock (SyncObject)
                {
                    _selectedPath = value ?? string.Empty;
                    SelectedIndex = -1;
                }
            }
        }

        /// <summary>
        /// Получает статус текущей выбранной карты на данный момент.
        /// </summary>
        /// <remarks>
        /// Является потокобезопасным.
        /// Значение свойства может быть сброшено либо удалением текущей карты, в том числе, очисткой всей коллекции.
        /// В случае, когда это свойство содержит значение <see langword="true" />, свойство <see cref="SelectedPath"/> содержит путь к этой карте.
        /// </remarks>
        /// <seealso cref="SelectedPath"/>
        /// <seealso cref="SelectedIndex"/>
        public virtual bool IsSelectedOne => !string.IsNullOrEmpty(SelectedPath);

        /// <summary>
        /// Получает индекс последней карты, к которой было обращение с помощью различных методов (добавление, сохранение, выборка по индексу (<see cref="GetFirstProcessor"/> или <see cref="GetLatestProcessor"/>).
        /// </summary>
        /// <remarks>
        /// Является потокобезопасным.
        /// Значение получается путём линейного поиска индекса карты в соответствующей коллекции, находящейся по пути <see cref="SelectedPath"/>.
        /// После чтения значения оно сохраняется во внутренней переменной <see cref="IntSelectedIndex"/>, далее берётся оттуда до тех пор, пока не будет сброшено (примет значение -1) либо добавлением (сохранением) карты,
        /// либо удалением текущей карты, в том числе, очисткой всей коллекции или обращением по индексу (<see cref="GetFirstProcessor"/> или <see cref="GetLatestProcessor"/>).
        /// Доступ на запись возможен только изнутри этого класса, и служит для сброса значения этого свойства.
        /// </remarks>
        /// <seealso cref="SelectedPath"/>
        /// <seealso cref="IsSelectedOne"/>
        public int SelectedIndex
        {
            get
            {
                lock (SyncObject)
                {
                    if (IntSelectedIndex > -1)
                        return IntSelectedIndex;

                    string selectedPath = SelectedPath;

                    if (string.IsNullOrEmpty(selectedPath))
                        return -1;

                    string findKey = GetStringKey(selectedPath);

                    int index = DictionaryByKey.Keys.TakeWhile(key => key != findKey).Count();

                    IntSelectedIndex = index < DictionaryByKey.Count ? index : -1;

                    return IntSelectedIndex;
                }
            }

            private set
            {
                lock (SyncObject)
                {
                    IntSelectedIndex = value;
                }
            }
        }

        /// <summary>
        /// Статическое свойство.
        /// Получает или задаёт значение, указывающее, разрешены или запрещены длительные операции.
        /// </summary>
        /// <remarks>
        /// Например, операции перечисления файлов, связанные с выполнением каких-либо действий, например, массовым добавлением или удалением карт из коллекции.
        /// Если во время выполнения длительной операции значение свойства будет изменено на <see langword="false" />, операция(-ции) будет прервана.
        /// Это свойство является потокобезопасным.
        /// Запись производится с помощью блокировки, чтение без блокировки.
        /// Как правило, применяется перед завершением работы приложения.
        /// Значение по умолчанию - <see langword="true" />.
        /// </remarks>
        public static bool LongOperationsAllowed
        {
            get => _longOperationsAllowed;

            set
            {
                lock (LongOperationsSync)
                {
                    _longOperationsAllowed = value;
                }
            }
        }

        /// <summary>
        ///     Получает те же элементы, что и <see cref="Elements" />, отличающиеся уникальными именами.
        /// </summary>
        /// <remarks>
        ///     Имена будут уникальными даже в случае, когда элементы находятся в разных папках.
        /// Отличаются значения свойств <see cref="Processor.Tag" />.
        /// </remarks>
        public IEnumerable<Processor> UniqueElements
        {
            get
            {
                lock (SyncObject)
                {
                    HashSet<string> tagSet = new HashSet<string>();

                    foreach ((string name, Processor p) in DictionaryByKey.Values.Select(pp =>
                                 (Path.GetFileNameWithoutExtension(pp.CurrentPath).ToLower(), pp.CurrentProcessor)))
                        yield return IntGetUniqueProcessor(tagSet, p, ParseName(name), string.Empty).processor;
                }
            }
        }

        /// <summary>
        /// Получает уникальные имена хранимых в коллекции карт, что и <see cref="UniqueElements"/>, только в виде набора строк.
        /// </summary>
        /// <remarks>
        /// Следует использовать для оптимизации производительности, т.к. в этом случае нет накладных затрат на создание копии набора карт, как в случае со свойством <see cref="UniqueElements"/>.
        /// </remarks>
        protected HashSet<string> UniqueNames
        {
            get
            {
                HashSet<string> tagSet = new HashSet<string>();

                foreach (string name in DictionaryByKey.Values.Select(pp =>
                             Path.GetFileNameWithoutExtension(pp.CurrentPath).ToLower()))
                    IntGetUniqueProcessor(tagSet, null, ParseName(name), string.Empty);

                return tagSet;
            }
        }

        /// <summary>
        ///     Загружает карту, выполняя все соответствующие проверки, характерные для текущего типа хранилища.
        /// </summary>
        /// <param name="fullPath">Путь к файлу, который содержит карту.</param>
        protected abstract Processor GetAddingProcessor(string fullPath);

        /// <summary>
        /// Получает полный путь (с расширением) к указанному имени карты.
        /// </summary>
        /// <param name="sourcePath">Путь к рабочей папке. Как правило, <see cref="WorkingDirectory"/>.</param>
        /// <param name="fileName">Имя файла, содержащего карту.</param>
        /// <returns>Полный путь к карте.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Расширение берётся из свойства <see cref="ExtImg"/>.
        /// </remarks>
        protected string CreateImagePath(string sourcePath, string fileName)
        {
            return
                $@"{Path.Combine(sourcePath ?? throw new ArgumentException($@"{nameof(CreateImagePath)}: Исходный путь карты не указан."), fileName)}.{ExtImg}";
        }

        /// <summary>
        /// Выполняет разбор (демаскировку) названия карты, разделяя его с помощью символа <see cref="ImageRect.TagSeparatorChar"/>.
        /// </summary>
        /// <param name="name">Название, которое требуется разобрать. Не может быть пустым.</param>
        /// <returns>Возвращает номер карты (или <see langword="null" />, если его нет) и имя карты без маскировки.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Номер карты интерпретируется как <see cref="ulong"/>.
        /// </remarks>
        protected static (ulong? number, string name) ParseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), nameof(ParseName));

            for (int k = name.Length - 1; k > 0; k--)
                if (name[k] == ImageRect.TagSeparatorChar)
                    return ulong.TryParse(name.Substring(k + 1), out ulong number)
                        ? (number, name.Substring(0, k))
                        : ((ulong?)null, name);

            return (null, name);
        }

        /// <summary>
        /// Получает карту с уникальным именем, добавляя его в указанный <see cref="ISet&lt;T&gt;"/>.
        /// </summary>
        /// <param name="maskedTagSet">Набор для поддержания уникальности имён карт.</param>
        /// <param name="tagProc">Карта, для которой требуется получить уникальное имя. Может быть равна <see langword="null"/>. Изначальное значение свойства <see cref="Processor.Tag"/> не имеет значения.</param>
        /// <param name="hint">Подсказка по имени (обязательно) и номеру карты, в целях оптимизации производительности.</param>
        /// <param name="pathToSave">Может быть <see cref="string.Empty"/>, но не может быть <see langword="null"/>. Путь, по которому будет располагаться указанная карта. Если <see cref="string.Empty"/>, то вернётся имя карты и расширение, но без полного пути.</param>
        /// <returns>Возвращает карту, соответствующую заданным параметрам или <see langword="null"/>. Второй параметр - путь, по которому располагается карта.</returns>
        /// <remarks>
        /// Карта может быть равна <see langword="null"/>. В этом случае уникальное имя будет получено и добавлено в указанный <see cref="ISet&lt;T&gt;"/>, но карта на выходе будет равна <see langword="null"/>.
        /// Таким образом, можно получить набор уникальных имён карт без наличия их самих.
        /// </remarks>
        (Processor processor, string path) IntGetUniqueProcessor(ISet<string> maskedTagSet, Processor tagProc,
            (ulong? number, string name) hint, string pathToSave)
        {
            (Processor, string) GetResult(string fileName)
            {
                return (tagProc != null ? ProcessorHandler.ChangeProcessorTag(tagProc, fileName) : null,
                    CreateImagePath(pathToSave, fileName));
            }

            unchecked
            {
                string nm = hint.name.ToLower();

                if (hint.number == null && maskedTagSet.Add(nm))
                    return GetResult(hint.name);

                ulong k = hint.number ?? 0, mk = k;

                do
                {
                    string att = $@"{nm}{ImageRect.TagSeparatorChar}{k}";

                    if (maskedTagSet.Add(att))
                        return GetResult($@"{hint.name}{ImageRect.TagSeparatorChar}{k}");
                } while (++k != mk);

                string pTag = tagProc != null ? tagProc.Tag : "<пусто>";

                throw new Exception(
                    $@"Нет свободного места для добавления карты в коллекцию: {pTag} по пути {WorkingDirectory}, изначальное имя карты {hint.name}{ImageRect.TagSeparatorChar}{hint.number}.");
            }
        }

        /// <summary>
        /// Получает карту и путь к ней, задавая ей соответствующее имя.
        /// Если путь окажется полным, то вернёт карту и путь без изменений.
        /// </summary>
        /// <param name="args">Входные параметры. Значения следующие: 1) Карта, которую необходимо переименовать. Может быть <see langword="null"/>. 2) Подсказка по имени (обязательно) и номеру карты, в целях оптимизации производительности. 3) Путь, по которому будет располагаться указанная карта. Может быть пустым (<see langword="null"/> или <see cref="string.Empty"/>).</param>
        /// <returns>Возвращает последовательность карт и путей к ним. Если карта не указана, то она всегда будет равна <see langword="null"/>. Если путь пустой (<see langword="null"/> или <see cref="string.Empty"/>), то будет возвращаться путь в рабочем каталоге <see cref="WorkingDirectory"/>.</returns>
        /// <remarks>
        /// Принимает массив различных параметров для преобразования.
        /// Это сделано для того, чтобы инициализировать внутренний набор один раз для обработки всех входных данных, с целью оптимизации производительности.
        /// </remarks>
        protected IEnumerable<(Processor processor, string path)> GetUniqueProcessor(
            IEnumerable<(Processor, (ulong?, string), string)> args)
        {
            HashSet<string> tagSet = null;

            foreach ((Processor p, (ulong?, string) t, string pathToSave) in args)
            {
                string argPath = pathToSave;

                if (GetImagePath(ref argPath))
                {
                    yield return (p, argPath);
                    continue;
                }

                if (tagSet == null)
                    tagSet = UniqueNames;

                yield return IntGetUniqueProcessor(tagSet, p, t, argPath);
            }
        }

        /// <summary>
        /// Получает уникальный путь для сохраняемой карты.
        /// </summary>
        /// <param name="tag">Название карты.</param>
        /// <returns>Возвращает путь в рабочем каталоге <see cref="WorkingDirectory"/>.</returns>
        /// <remarks>
        /// В случае необходимости, название карты маскируется.
        /// Возвращает только полный путь.
        /// </remarks>
        protected string GetUniquePath(string tag)
        {
            return GetUniqueProcessor(new[]
                { ((Processor)null, (ParseName(tag).number == null ? (ulong?)null : 0, tag), string.Empty) }).Single().path;
        }

        /// <summary>
        /// Присоединяет указанную часть пути (папку) к <see cref="WorkingDirectory"/>.
        /// </summary>
        /// <param name="folderName">Имя папки внутри рабочего каталога <see cref="WorkingDirectory"/>.</param>
        /// <returns>
        /// Возвращает полный путь к указанной папке.
        /// </returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// В случае нахождения некорректных символов, они будут заменены на символ подчёркивания.
        /// Путь корректируется с помощью метода <see cref="ReplaceInvalidPathChars"/>.
        /// Если путь пустой (<see langword="null"/> или <see cref="string.Empty"/>), то будет возвращаться путь в рабочем каталоге <see cref="WorkingDirectory"/>.
        /// Если имя папки будет содержать корневой каталог, будет возвращён именно этот путь.
        /// </remarks>
        public string CombinePaths(string folderName)
        {
            return Path.Combine(WorkingDirectory, ReplaceInvalidPathChars(folderName));
        }

        /// <summary>
        /// Заменяет недопустимые символы, которые не должен содержать путь, на подчёркивания.
        /// </summary>
        /// <param name="path">Исследуемый путь.</param>
        /// <returns>
        /// Возвращает исправленный путь, если в нём были найдены какие-либо недопустимые символы.
        /// В противном случае, возвращает исходный путь.
        /// </returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Символы берутся из коллекции <see cref="FrmExample.InvalidCharSet"/>.
        /// </remarks>
        static string ReplaceInvalidPathChars(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : FrmExample.InvalidCharSet.Aggregate(path, (current, c) => current.Replace(c, '_'));
        }

        /// <summary>
        /// Создаёт указанный каталог на жёстком диске.
        /// Создаёт все подкаталоги, если они отсутствуют.
        /// </summary>
        /// <param name="path">Путь каталога.</param>
        public static void CreateFolder(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Создаёт рабочий каталог (<see cref="WorkingDirectory"/>) на жёстком диске.
        /// </summary>
        public void CreateWorkingDirectory()
        {
            Directory.CreateDirectory(WorkingDirectory);
        }

        /// <summary>
        /// Определяет, находится ли указанный путь в рабочей директории (<see cref="WorkingDirectory"/>).
        /// </summary>
        /// <param name="path">Проверяемый путь.</param>
        /// <param name="isEqual">
        /// <see langword="true"/>, если требуется проверка пути полностью (пути совпадают без учёта регистра).
        /// <see langword="false"/>, если требуется выяснить принадлежность (указанный путь начинается с <see cref="WorkingDirectory"/>, без учёта регистра).</param>
        /// <returns>Возвращает значение <see langword="true"/> в случае, если указанный путь находится в рабочем каталоге.</returns>
        /// <remarks>
        /// Для метода не имеет значения наличие разделителей в конце сравниваемых путей.
        /// Если их не будет, метод сам их добавит для коректного сравнения.
        /// Метод является потокобезопасным.
        /// </remarks>
        public bool IsWorkingDirectory(string path, bool isEqual = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException($@"{nameof(IsWorkingDirectory)}: Необходимо указать путь для проверки.",
                    nameof(path));

            string p = AddEndingSlash(path), ip = AddEndingSlash(WorkingDirectory);

            return isEqual
                ? string.Compare(p, ip, StringComparison.OrdinalIgnoreCase) == 0
                : p.StartsWith(ip, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Проверяет, является ли указанный символ разделителем для строки пути.
        /// </summary>
        /// <param name="c">Проверяемый символ.</param>
        /// <returns>Возвращает значение <see langword="true"/> в случае, если символ является таковым.</returns>
        /// <remarks>
        /// Проверяет как основной, так и дополнительный символ (прямой и обратный слеш).
        /// Метод является потокобезопасным.
        /// </remarks>
        public static bool IsDirectorySeparatorSymbol(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        /// <summary>
        /// Возвращает строку пути, добавив разделитель каталогов в её конец.
        /// </summary>
        /// <param name="path">Строка пути.</param>
        /// <returns>Возвращает путь с разделителем на конце.</returns>
        /// <remarks>
        /// Для определения наличия разделителя на конце используется метод <see cref="IsDirectorySeparatorSymbol"/>.
        /// Если строка пустая (<see langword="null"/> или <see cref="string.Empty"/>), возвращается <see cref="string.Empty"/>.
        /// Метод является потокобезопасным.
        /// </remarks>
        public static string AddEndingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            if (!IsDirectorySeparatorSymbol(path[path.Length - 1]))
                path += Path.DirectorySeparatorChar;

            return path;
        }

        /// <summary>
        /// Корректирует указанный путь и возвращает соответствующее его значение.
        /// Используется в методе <see cref="GetUniqueProcessor"/>.
        /// </summary>
        /// <param name="relativeFolderPath">Корректируемый путь.</param>
        /// <returns>Если в пути отсутствуют ошибки, возвращается значение <see langword="true"/>.</returns>
        /// <remarks>
        /// В случае, когда путь указывает на файл с картой (абсолютный путь, с расширением <see cref="ExtImg"/>), возвращается значение <see langword="true"/>.
        /// В этом случае параметр <paramref name="relativeFolderPath"/> остаётся без изменений.
        /// В случае, если он ведёт не в рабочий каталог, выдаётся исключение <see cref="ArgumentException"/>. При этом, параметр <paramref name="relativeFolderPath"/> остаётся без изменений.
        /// В случае, если параметр <paramref name="relativeFolderPath"/> пустой (<see langword="null"/> или <see cref="string.Empty"/>), ему будет присвоен путь в рабочий каталог <see cref="WorkingDirectory"/>.
        /// Вернётся значение <see langword="false"/>.
        /// Если путь <paramref name="relativeFolderPath"/> ведёт в каталог (находящийся в рабочем каталоге), то параметр <paramref name="relativeFolderPath"/> останется без изменений. Вернётся значение <see langword="false"/>.
        /// Для определения, указывает ли путь на каталог, используется метод <see cref="IsDirectory"/>.
        /// В случае, когда путь указывает на файл с другим расширением, возникает исключение <see cref="ArgumentException"/>.
        /// Параметр <paramref name="relativeFolderPath"/> остаётся неизменным.
        /// Метод является потокобезопасным.
        /// </remarks>
        public bool GetImagePath(ref string relativeFolderPath)
        {
            if (string.IsNullOrEmpty(relativeFolderPath))
            {
                relativeFolderPath = WorkingDirectory;
                return false;
            }

            if (!IsWorkingDirectory(relativeFolderPath))
                throw new ArgumentException($@"Необходимо нахождение пути в рабочем каталоге ({WorkingDirectory})",
                    nameof(relativeFolderPath));

            if (IsDirectory(relativeFolderPath))
                return false;

            if (string.Compare(Path.GetExtension(relativeFolderPath), $".{ExtImg}",
                    StringComparison.OrdinalIgnoreCase) != 0)
                throw new ArgumentException($@"Необходимо, чтобы путь вёл к файлу с требуемым расширением ({ExtImg})",
                    nameof(relativeFolderPath));

            return true;
        }

        /// <summary>
        ///     Получает перечисляемую коллекцию путей к изображениям, интерпретируемым как карты, в указанной папке и всех подпапках.
        ///     Это файлы с расширением <see cref="ExtImg" />.
        /// </summary>
        /// <param name="path">Путь, по которому требуется получить список файлов изображений карт.</param>
        /// <returns>Возвращает запрос списка файлов.</returns>
        /// <remarks>
        /// Поскольку этот запрос является длительным, на него влияет флаг <see cref="LongOperationsAllowed"/>.
        /// Чтобы выполнить этот запрос, необходимо вызвать соответствующий метод для того, чтобы получить коллекцию.
        /// Метод является потокобезопасным.
        /// </remarks>
        IEnumerable<string> QueryProcessorFiles(string path)
        {
            return Directory.EnumerateFiles(path, $"*.{ExtImg}", SearchOption.AllDirectories)
                .TakeWhile(_ => LongOperationsAllowed).Where(IsProcessorFile);
        }

        /// <summary>
        /// Добавляет карту(ы), расположенную по указанному пути.
        /// </summary>
        /// <param name="fullPath">Путь к файлу или папке с картами.</param>
        /// <returns>Возвращает карты, считанные по указанному пути.</returns>
        /// <remarks>
        /// Если в этой же папке будут встречаться файлы с другими расширениями, они будут игнорироваться.
        /// Чтение карт производится рекурсивно - из всех папок и подпапок.
        /// В случае возникновения исключения в процессе чтения карты, если карта по указанному пути уже была добавлена в хранилище, она будет удалена из хранилища.
        /// Это касается только карт, находящихся в рабочем каталоге.
        /// Для добавления карт в <see cref="ConcurrentProcessorStorage"/> требуется, чтобы добавляемые карты находились в папке хранилища (<see cref="WorkingDirectory"/>), которую определяет метод <see cref="IsWorkingDirectory"/>.
        /// Если карта находится за пределами рабочего каталога (<see cref="WorkingDirectory"/>), она будет считана и возвращена, но не будет добавлена в коллекцию.
        /// Исключение может возникать только после завершения обработки карт. В случае получения нескольких исключений будет выброшено <see cref="AggregateException"/>.
        /// В случае деактивации флага <see cref="LongOperationsAllowed"/> метод вернёт пустой массив. <see langword="null"/> никогда не возвращается.
        /// Метод потокобезопасен.
        /// </remarks>
        public IEnumerable<Processor> AddProcessor(string fullPath)
        {
            List<Exception> lstExceptions = new List<Exception>();

            bool needAdd = IsWorkingDirectory(fullPath);

            IEnumerable<Processor> IntAdd()
            {
                if (IsProcessorFile(fullPath))
                {
                    yield return IntAddProcessor(fullPath, needAdd);
                    yield break;
                }

                if (!IsDirectory(fullPath))
                    yield break;

                foreach (string pFile in QueryProcessorFiles(fullPath))
                {
                    Processor p = IntAddProcessor(pFile, needAdd, lstExceptions);

                    if (p != null)
                        yield return p;
                }
            }

            IEnumerable<Processor> result = IntAdd().ToList();

            if (!LongOperationsAllowed)
                return new Processor[0];

            int count = lstExceptions.Count;

            if (count > 1)
                throw new AggregateException(
                    $@"{nameof(AddProcessor)}: При загрузке группы карт возникли исключения ({count}).", lstExceptions);

            if (count == 1)
                throw lstExceptions[0];

            return result;
        }

        /// <summary>
        ///     Добавляет карту в коллекцию, по указанному пути.
        ///     Если карта не подходит по каким-либо признакам, а в коллекции хранится карта по тому же пути, то она удаляется из
        ///     коллекции (только в случае, когда <paramref name="needAdd"/> = <see langword="true"/>).
        ///     Если карта уже присутствовала в коллекции, то она будет перезагружена в неё.
        /// </summary>
        /// <param name="fullPath">Путь к изображению, которое будет интерпретировано как карта.</param>
        /// <param name="needAdd">
        /// Значение <see langword="true"/> в случае необходимости добавить указанную карту в коллекцию.
        /// Значение <see langword="false"/> в случае, если необходимо считать карту и вернуть её без добавления в коллекцию.
        /// </param>
        /// <param name="lstExceptions">Коллекция исключений, куда требуется добавить возникшее исключение. Может быть <see langword="null"/> (значение по умолчанию).</param>
        /// <returns>Возвращает карту, считанную по указанному пути.</returns>
        /// <remarks>
        /// Если указана коллекция исключений <paramref name="lstExceptions"/>, то возникшее исключение будет добавлено в неё, и не будет выброшено, в отличии от случая, если коллекция исключений равна <see langword="null"/> (значение по умолчанию).
        /// В этом случае метод вернёт <see langword="null"/>.
        /// Такой способ обработки исключений необходим для массовой загрузки карт в коллекцию.
        /// В случае деактивации флага <see cref="LongOperationsAllowed"/> метод вернёт <see langword="null"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        Processor IntAddProcessor(string fullPath, bool needAdd, ICollection<Exception> lstExceptions = null)
        {
            try
            {
                Processor addingProcessor;

                try
                {
                    addingProcessor = GetAddingProcessor(fullPath);
                }
                catch
                {
                    if (needAdd)
                        RemoveProcessor(fullPath);

                    if (!LongOperationsAllowed)
                        return null;

                    throw;
                }

                if (!LongOperationsAllowed)
                    return null;

                if (!needAdd)
                    return addingProcessor;

                int hashCode = GetHashCode(addingProcessor);

                lock (SyncObject)
                {
                    ReplaceElement(hashCode, fullPath, addingProcessor);
                }

                return LongOperationsAllowed ? addingProcessor : null;
            }
            catch (Exception ex)
            {
                if (!LongOperationsAllowed)
                    return null;

                if (lstExceptions == null)
                    throw;

                lstExceptions.Add(ex);

                return null;
            }
        }

        /// <summary>
        /// Выполняет проверку значения <see cref="Color.A"/> цветов, применяемых в изображениях, которые будут преобразованы в карты.
        /// </summary>
        /// <param name="btm">Проверяемое изображение.</param>
        /// <returns>Возвращает <paramref name="btm"/>.</returns>
        /// <remarks>
        /// Параметр <paramref name="btm"/> не может быть равен <see langword="null"/>, иначе <see cref="ArgumentNullException"/>.
        /// Для проверки используется метод <see cref="FrmExample.CheckAlphaColor"/>.
        /// В случае несоответствия ожиданиям, будет выдано исключение <see cref="InvalidOperationException"/>.
        /// Ожидаемое значение содержится в свойстве <see cref="FrmExample.DefaultOpacity"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        static Bitmap CheckBitmapByAlphaColor(Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(CheckBitmapByAlphaColor)}: Изображение должно быть указано.");

            for (int y = 0; y < btm.Height; y++)
                for (int x = 0; x < btm.Width; x++)
                    FrmExample.CheckAlphaColor(btm.GetPixel(x, y));

            return btm;
        }

        /// <summary>
        /// Считывает изображение по указанному пути.
        /// </summary>
        /// <param name="fullPath">Путь к изображению.</param>
        /// <returns>Возвращает считанное изображение.</returns>
        /// <remarks>
        /// Поддерживается возможность совершения повторных попыток открытия файла в случае возникновения ошибок.
        /// Например, файл занят другим приложением.
        /// В случае какой-либо ошибки открытия файла, попытки его открыть будут продолжаться каждые 100 мс, в течение пяти секунд.
        /// Если ни одна попытка не была успешной, выдаётся <see cref="FileNotFoundException"/>.
        /// Для получения изначального исключения, необходимо считывать свойство <see cref="Exception.InnerException"/>.
        /// После считывания изображения выполняется его проверка с помощью метода <see cref="CheckBitmapByAlphaColor"/>.
        /// Файл открывается на чтение, с флагом <see cref="FileShare.Read"/>.
        /// </remarks>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="InvalidOperationException"/>
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
                        throw new FileNotFoundException($@"{nameof(LoadBitmap)}: {ex.Message}", fullPath, ex);

                    Thread.Sleep(100);

                    continue;
                }

                break;
            }

            if (fs == null)
                throw new InvalidOperationException($@"{nameof(LoadBitmap)}: {nameof(fs)} = null.");

            using (fs)
            {
                Bitmap btm = new Bitmap(fs);

                try
                {
                    return CheckBitmapByAlphaColor(btm);
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
        ///     Добавляет её в коллекцию, идентифицирующую карты по <paramref name="hashCode"/>, так и в коллекцию, идентифицирующую карты по путям.
        ///     <paramref name="hashCode"/> добавляемой карты может совпадать с <paramref name="hashCode"/> других карт.
        ///     Полный путь к добавляемой карте на существование не проверяется.
        ///     Если карта уже присутствовала в коллекции, то она будет перезагружена в неё.
        /// </summary>
        /// <param name="hashCode">Хеш добавляемой карты.</param>
        /// <param name="fullPath">Полный путь к добавляемой карте.</param>
        /// <param name="processor">Добавляемая карта <see cref="Processor" />.</param>
        /// <remarks>
        /// На эту операцию влияет флаг <see cref="LongOperationsAllowed"/>.
        /// </remarks>
        protected virtual void ReplaceElement(int hashCode, string fullPath, Processor processor)
        {
            RemoveProcessor(fullPath);

            if (!LongOperationsAllowed)
                return;

            BaseAddElement(hashCode, fullPath, processor);
        }

        /// <summary>
        /// Базовая функция для добавления указанной карты <see cref="Processor" /> в <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        /// <param name="hashCode">Хеш добавляемой карты.</param>
        /// <param name="fullPath">Полный путь к добавляемой карте.</param>
        /// <param name="processor">Добавляемая карта <see cref="Processor" />.</param>
        /// <remarks>
        /// Эта функция заполняет коллекции <see cref="DictionaryByKey"/> и <see cref="DictionaryByHash"/>.
        /// Обрабатывает случай, когда <paramref name="hashCode"/> добавляемой карты совпадает с <paramref name="hashCode"/> других карт.
        /// Не синхронизирует доступ к коллекциям. Никаких проверок не производится.
        /// При подаче некорректных значений аргументов поведение неопределено.
        /// </remarks>
        protected void BaseAddElement(int hashCode, string fullPath, Processor processor)
        {
            DictionaryByKey.Add(GetStringKey(fullPath), new ProcPath(processor, fullPath));

            if (DictionaryByHash.TryGetValue(hashCode, out ProcHash ph))
                ph.AddProcessor(new ProcPath(processor, fullPath));
            else
                DictionaryByHash.Add(hashCode, new ProcHash(new ProcPath(processor, fullPath)));
        }

        /// <summary>
        ///     Получает карту по указанному индексу.
        /// </summary>
        /// <param name="index">
        ///     Индекс карты <see cref="Processor" />, которую необходимо получить. В случае допустимого
        ///     изначального значения, это значение остаётся прежним, иначе равняется индексу последней карты в коллекции.
        /// </param>
        /// <returns>Возвращает карту, путь к ней, и количество карт в коллекции.</returns>
        /// <remarks>
        /// Путь, по которому располагается выбранная карта, сохраняется в свойстве <see cref="SelectedPath"/>.
        /// В случае недопустимого значения индекса карты возвращается последняя карта.
        /// В случае отсутствия карт в коллекции будут возвращены следующие значения: (<see langword="null" />, <see cref="string.Empty" />, 0), <paramref name="index"/> будет равен нолю.
        /// </remarks>
        /// <seealso cref="SelectedIndex"/>
        /// <seealso cref="IsSelectedOne"/>
        public (Processor processor, string path, int count) GetLatestProcessor(ref int index)
        {
            lock (SyncObject)
            {
                if (IsEmpty)
                {
                    index = 0;
                    return (null, string.Empty, 0);
                }

                int count = Count;

                if (index < 0 || index >= count)
                    index = count - 1;
                (Processor processor, string path, _) = this[index];

                SelectedPath = path;

                return (processor, path, count);
            }
        }

        /// <summary>
        /// Получает карту по указанному индексу.
        /// </summary>
        /// <param name="index">
        ///     Индекс карты <see cref="Processor" />, которую необходимо получить. В случае допустимого
        ///     изначального значения, это значение остаётся прежним, иначе равняется индексу первой карты в коллекции.
        /// </param>
        /// <param name="useLastIndex">
        /// Если значение <see langword="true" />, то значение параметра <paramref name="index"/> будет считано из свойства <see cref="SelectedIndex"/>.
        /// Если свойство <see cref="SelectedIndex"/> сброшено, то будут возвращены следующие значения: (<see langword="null" />, <see cref="SelectedPath" />, <see cref="Count" />), значение параметра <paramref name="index"/> останется прежним.
        /// </param>
        /// <returns>Возвращает карту, путь к ней, и количество карт в коллекции.</returns>
        /// <remarks>
        /// Путь, по которому располагается выбранная карта, сохраняется в свойстве <see cref="SelectedPath"/>.
        /// В случае недопустимого значения индекса карты возвращается первая карта.
        /// В случае отсутствия карт в коллекции будут возвращены следующие значения: (<see langword="null" />, <see cref="SelectedPath" />, 0), <paramref name="index"/> будет равен нолю.
        /// </remarks>
        /// <seealso cref="SelectedIndex"/>
        /// <seealso cref="IsSelectedOne"/>
        public (Processor processor, string path, int count) GetFirstProcessor(ref int index, bool useLastIndex = false)
        {
            lock (SyncObject)
            {
                if (IsEmpty)
                {
                    index = 0;
                    return (null, SelectedPath, 0);
                }

                int count = Count;

                if (useLastIndex)
                {
                    int lastIndex = SelectedIndex;

                    if (lastIndex < 0)
                        return (null, SelectedPath, count);

                    index = lastIndex;
                }

                if (index < 0 || index >= count)
                    index = 0;

                (Processor processor, string path, _) = this[index];

                SelectedPath = path;

                return (processor, path, count);
            }
        }

        /// <summary>
        /// Получает значение, отражающее фактическое существование выбранной карты на жёстком диске.
        /// </summary>
        /// <returns>
        /// Если файл существует, возвращает ("путь к файлу", <see langword="true"/>), в противном случае - ("путь к файлу", <see langword="false"/>).
        /// Если свойство <see cref="SelectedPath"/> сброшено, вернёт (<see cref="string.Empty"/>, <see langword="false"/>).
        /// </returns>
        /// <remarks>
        /// Является потокобезопасным.
        /// Считывает свойство <see cref="SelectedPath"/>.
        /// Обращается к файловой системе.
        /// </remarks>
        /// <seealso cref="SelectedPath"/>
        public (string path, bool isExists) IsSelectedPathExists()
        {
            string path = SelectedPath;

            return (path, !string.IsNullOrEmpty(path) && new FileInfo(path).Exists);
        }

        /// <summary>
        ///     Удаляет указанную карту <see cref="Processor" /> из коллекции <see cref="ConcurrentProcessorStorage" />,
        ///     идентифицируя её по пути к ней.
        /// </summary>
        /// <param name="fullPath">Полный путь к карте (или к папке с картами), которую необходимо удалить из коллекции.</param>
        /// <returns>
        /// В случае, если была найдена и удалена хотя бы одна карта, возвращает значение <see langword="true" />.
        /// Если операция была отменена с помощью флага <see cref="LongOperationsAllowed"/>, метод всегда возвращает значение <see langword="false" />.
        /// По этой же причине, не все карты могут быть удалены, т.к. неизвестно, на каком этапе операция была прервана.
        /// </returns>
        /// <remarks>
        /// Является потокобезопасным.
        /// Если указан путь к папке, то будут удалены все карты, содержащиеся в ней (в том числе, во вложенных папках).
        /// В случае, если удаляемая (указанная) карта является выбранной в данный момент, свойство <see cref="SelectedPath"/> будет сброшено.
        /// </remarks>
        /// <seealso cref="SelectedPath"/>
        /// <seealso cref="SelectedIndex"/>
        public bool RemoveProcessor(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return false;

            lock (SyncObject)
            {
                if (!IsDirectory(fullPath))
                    return RemoveProcessor(this[fullPath].processor);

                if (!LongOperationsAllowed)
                    return false;

                Processor[] arrNeedRemove = DictionaryByKey.Keys.TakeWhile(_ => LongOperationsAllowed)
                    .Where(x => x.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
                    .Select(path => this[path].processor).ToArray();

                if (!LongOperationsAllowed)
                    return false;

                bool result = false;

                foreach (Processor p in arrNeedRemove)
                {
                    if (!LongOperationsAllowed)
                        return false;

                    if (RemoveProcessor(p))
                        result = true;
                }

                return result;
            }
        }

        /// <summary>
        /// Получает значение, отражающее, является ли указанный путь приемлимым в качестве пути к карте.
        /// </summary>
        /// <param name="path">Проверяемый путь.</param>
        /// <returns>
        /// Если указанный путь является приемлимым в качестве пути к карте, возвращает значение <see langword="true" />.
        /// Если входная строка пустая (<see langword="null" /> или <see cref="string.Empty" />), возвращает значение <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// Метод проверяет только расширение.
        /// Путь обязательно должен содержать хотя бы один разделитель (<see cref="Path.DirectorySeparatorChar"/> или <see cref="Path.AltDirectorySeparatorChar"/>).
        /// Расширение должно быть <see cref="ExtImg"/>.
        /// Регистр символов расширения не имеет значения.
        /// К файловой системе не обращается.
        /// Метод потокобезопасный.
        /// </remarks>
        /// <seealso cref="ExtImg"/>
        /// <seealso cref="Path.DirectorySeparatorChar"/>
        /// <seealso cref="Path.AltDirectorySeparatorChar"/>
        public bool IsProcessorFile(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   string.Compare(Path.GetExtension(path), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Проверяет, указывает ли заданный путь на директорию.
        /// </summary>
        /// <param name="path">Проверяемый путь.</param>
        /// <returns>Если указывает на директорию, то возвращает значение <see langword="true" />.</returns>
        /// <remarks>
        /// Входная строка может быть <see langword="null" /> или <see cref="string.Empty" />.
        /// Для увеличения производительности метода лучше на конце строки использовать какой-либо разделитель каталогов.
        /// Если в конце строки символ-разделитель не найден, то ищется расширение. Если оно отсутствует, то считается, что строка пути указывает на каталог.
        /// Метод является потокобезопасным.
        /// К файловой системе не обращается.
        /// Использует метод <see cref="IsDirectorySeparatorSymbol"/>.
        /// </remarks>
        static bool IsDirectory(string path)
        {
            return !string.IsNullOrEmpty(path) && (IsDirectorySeparatorSymbol(path[path.Length - 1]) ||
                                                   string.IsNullOrEmpty(Path.GetExtension(path)));
        }

        /// <summary>
        /// Получает ключ для хранения элемента в коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        /// <param name="path">Путь к элементу, для которого необходимо сформировать ключ.</param>
        /// <returns>Возвращает ключ в виде строки.</returns>
        /// <remarks>
        /// Если путь пустой (<see langword="null" /> или <see cref="string.Empty" />), то вернётся <see cref="string.Empty"/>.
        /// Ключ представляет собой обработку методом <see cref="string.ToLower()"/>.
        /// Метод потокобезопасен.
        /// </remarks>
        protected static string GetStringKey(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.ToLower();
        }

        /// <summary>
        /// Получает хеш-код указанной карты.
        /// </summary>
        /// <param name="processor">Карта, хеш-код которой требуется получить.</param>
        /// <returns>Хеш-код указанной карты.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Для получения дополнительных сведений см. класс <see cref="CRCIntCalc"/>.
        /// Для генерации хеш-кода используется только тело карты, значение свойства <see cref="Processor.Tag"/> не принимается во внимание.
        /// </remarks>
        static int GetHashCode(Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor), @"Для получения хеша карты необходимо её указать.");

            return CRCIntCalc.GetHash(processor);
        }

        /// <summary>
        ///     Удаляет указанную карту <see cref="Processor" /> из коллекции <see cref="ConcurrentProcessorStorage" />.
        /// </summary>
        /// <param name="processor">Карта <see cref="Processor" />, которую следует удалить.</param>
        /// <remarks>
        /// Является потокобезопасным.
        /// В случае, если указанная карта является выбранной в данный момент, свойство <see cref="SelectedPath"/> будет сброшено.
        /// </remarks>
        /// <returns>В случае успешного удаления карты возвращает значение <see langword="true" />.</returns>
        /// <seealso cref="SelectedPath"/>
        /// <seealso cref="SelectedIndex"/>
        bool RemoveProcessor(Processor processor)
        {
            if (processor == null)
                return false;
            int hashCode = GetHashCode(processor);
            bool result = false;
            lock (SyncObject)
            {
                if (!DictionaryByHash.TryGetValue(hashCode, out ProcHash ph))
                    return false;
                int index = 0;
                foreach (ProcPath px in ph.Elements)
                    if (ReferenceEquals(processor, px.CurrentProcessor))
                    {
                        string path = ph[index].CurrentPath;
                        result = DictionaryByKey.Remove(GetStringKey(path));
                        result &= ph.RemoveProcessor(index);
                        if (string.Compare(path, SelectedPath, StringComparison.OrdinalIgnoreCase) == 0)
                            SelectedPath = string.Empty;
                        SelectedIndex = -1;
                        break;
                    }
                    else
                    {
                        index++;
                    }

                if (!ph.Elements.Any())
                    DictionaryByHash.Remove(hashCode);
            }

            return result;
        }

        /// <summary>
        /// Удаляет всё содержимое коллекции <see cref="ConcurrentProcessorStorage"/>.
        /// </summary>
        /// <remarks>
        /// Не затрагивает свойство <see cref="LongOperationsAllowed"/>.
        /// Является потокобезопасным.
        /// </remarks>
        /// <seealso cref="LongOperationsAllowed"/>
        public void Clear()
        {
            lock (SyncObject)
            {
                DictionaryByKey.Clear();
                DictionaryByHash.Clear();

                SelectedPath = string.Empty;
            }
        }

        /// <summary>
        ///     Сохраняет указанное изображение на жёсткий диск.
        /// </summary>
        /// <param name="btm">Изображение, которое требуется сохранить.</param>
        /// <param name="path">
        ///     Абсолютный путь, по которому требуется сохранить изображение. Если путь относительный, то
        ///     используется <see cref="WorkingDirectory" />.
        /// </param>
        protected void SaveToFile(Bitmap btm, string path)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(SaveToFile)}: Необходимо указать изображение, которое требуется сохранить.");

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException($@"{nameof(SaveToFile)}: Путь, по которому требуется сохранить изображение, не задан.", nameof(path));

            string resultTmp, result;

            try
            {
                path = Path.ChangeExtension(path, string.Empty);

                resultTmp = Path.Combine(WorkingDirectory, Path.ChangeExtension(path, @"bmpTMP"));
                result = Path.Combine(WorkingDirectory, Path.ChangeExtension(path, ExtImg));

                using (FileStream fs = new FileStream(resultTmp, FileMode.Create, FileAccess.Write))
                    btm.Save(fs, ImageFormat.Bmp);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(SaveToFile)}: Неизвестная ошибка при сохранении карты (1): {ex.Message}", ex);
            }

            try
            {
                File.Delete(result);
                File.Move(resultTmp, result);
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception($@"{nameof(SaveToFile)}: Ошибка при сохранении карты: {resultTmp}: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new Exception($@"{nameof(SaveToFile)}: Попытка перезаписать существующий файл: {result}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(SaveToFile)}: Неизвестная ошибка при сохранении карты (2): {ex.Message}", ex);
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
            public Processor CurrentProcessor { get; }

            /// <summary>
            ///     Путь к карте <see cref="Processor" />.
            /// </summary>
            public string CurrentPath { get; }

            /// <summary>
            ///     Инициализирует хранимые объекты: <see cref="Processor" /> и <see cref="string" />.
            /// </summary>
            /// <param name="p">Хранимая карта.</param>
            /// <param name="path">Путь к карте <see cref="Processor" />.</param>
            public ProcPath(Processor p, string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException($@"{nameof(ProcPath)} (конструктор): Параметр {nameof(path)} не может быть пустым.", nameof(path));
                CurrentProcessor =
                    p ?? throw new ArgumentNullException(nameof(p), $@"{nameof(ProcPath)} (конструктор): {nameof(p)} не может быть равен null.");
                CurrentPath = path;
            }
        }

        /// <summary>
        ///     Хранит карты, которые соответствуют одному значению хеша.
        /// </summary>
        protected readonly struct ProcHash
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
            public ProcHash(ProcPath p)
            {
                if (p.CurrentProcessor == null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция (конструктор) {nameof(ProcHash)}.");
                _lst = new List<ProcPath> { p };
            }

            /// <summary>
            ///     Добавляет одну карту в коллекцию.
            ///     Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            public void AddProcessor(ProcPath p)
            {
                if (p.CurrentProcessor == null || string.IsNullOrWhiteSpace(p.CurrentPath))
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(AddProcessor)}.");
                _lst.Add(p);
            }

            /// <summary>
            ///     Получает все хранимые карты в текущем экземпляре <see cref="ProcHash" />.
            /// </summary>
            public IEnumerable<ProcPath> Elements => _lst;

            /// <summary>
            ///     Получает <see cref="ProcHash" /> по указанному индексу.
            /// </summary>
            /// <param name="index">Индекс элемента <see cref="ProcHash" />, который требуется получиться.</param>
            /// <returns>Возвращает <see cref="ProcHash" /> по указанному индексу.</returns>
            public ProcPath this[int index] => _lst[index];

            /// <summary>
            ///     Удаляет элемент <see cref="ProcPath"/> из коллекции <see cref="ProcHash"/>, с указанным индексом.
            /// </summary>
            /// <param name="index">Индекс карты, которую требуется удалить.</param>
            /// <returns>В случае успешного удаления карты возвращает значение <see langword="true" />.</returns>
            /// <remarks>
            /// НЕ является потокобезопасным.
            /// В случае недопустимого значения параметра <paramref name="index"/> возвращает значение <see langword="false" />.
            /// </remarks>
            public bool RemoveProcessor(int index)
            {
                if (index < 0 || index >= _lst.Count)
                    return false;

                _lst.RemoveAt(index);

                return true;
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
            public static int GetHash(Processor p)
            {
                if (p == null)
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
                if (p == null)
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
                if (ints == null)
                    throw new ArgumentNullException(nameof(ints),
                        $@"Для подсчёта контрольной суммы необходимо указать массив байт. Функция {nameof(GetHash)}.");
                return ints.Aggregate(255, (current, t) => Table[(byte)(current ^ t)]);
            }
        }
    }
}