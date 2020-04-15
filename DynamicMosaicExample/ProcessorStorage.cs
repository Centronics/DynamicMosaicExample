using System;
using System.Collections.Generic;
using System.Linq;
using Processor = DynamicParser.Processor;

namespace DynamicMosaicExample
{
    /// <summary>
    /// Хранилище карт <see cref="Processor"/> с поддержкой поиска с использованием хеш-таблицы.
    /// Поддерживаются повторяющиеся ключи.
    /// </summary>
    sealed class ProcessorStorage
    {
        /// <summary>
        /// Коллекция карт, идентифицируемых по хешу.
        /// </summary>
        readonly Dictionary<int, ProcHash> _dictionary = new Dictionary<int, ProcHash>();

        /// <summary>
        /// Хранит карты, которые соответствуют одному значению хеша.
        /// </summary>
        struct ProcHash
        {
            /// <summary>
            /// Список хранимых карт, дающих одно значение хеша.
            /// </summary>
            readonly List<Processor> _lst;

            /// <summary>
            /// Конструктор, который добавляет одну карту по умолчанию.
            /// Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            public ProcHash(Processor p)
            {
                if (p is null)
                    throw new ArgumentNullException(nameof(p), $@"Функция (конструктор) {nameof(ProcHash)}.");
                _lst = new List<Processor> { p };
            }

            /// <summary>
            /// Добавляет одну карту в коллекцию.
            /// Значение не может быть равно <see langword="null" />.
            /// </summary>
            /// <param name="p">Добавляемая карта.</param>
            public void AddProcessor(Processor p)
            {
                if (p is null)
                    throw new ArgumentNullException(nameof(p), $@"Функция {nameof(AddProcessor)}.");
                _lst.Add(p);
            }

            /// <summary>
            /// Получает все хранимые карты в текущем экземпляре <see cref="ProcHash"/>.
            /// </summary>
            public IEnumerable<Processor> Elements => _lst;
        }

        /// <summary>
        /// Добавляет карту в коллекцию.
        /// </summary>
        /// <param name="p">Добавляемая карта.</param>
        public void AddProcessor(Processor p)
        {
            if (p is null)
                throw new ArgumentNullException(nameof(p), $@"Функция {nameof(AddProcessor)}.");
            int hashCode = CRCIntCalc.GetHash(p);
            if (!_dictionary.TryGetValue(hashCode, out ProcHash ph))
                _dictionary.Add(hashCode, new ProcHash(p));
            else
            if (ph.Elements.All(px => !ProcessorCompare(p, px)))
                ph.AddProcessor(p);
        }

        /// <summary>
        /// Проверяет, содержится ли указанная карта в хранилище.
        /// В случае присутствия указанной карты в хранилище, возвращается значение <see langword="true" />, в противном случае возвращается значение <see langword="false" />.
        /// </summary>
        /// <param name="p">Проверяемая карта.</param>
        /// <returns>В случае присутствия указанной карты в хранилище, возвращается значение <see langword="true" />, в противном случае возвращается значение <see langword="false" />.</returns>
        public bool Contains(Processor p)
        {
            if (p is null)
                throw new ArgumentNullException(nameof(p), $@"Функция {nameof(Contains)}.");
            return _dictionary.TryGetValue(CRCIntCalc.GetHash(p), out ProcHash ph) && ph.Elements.Any(px => ProcessorCompare(p, px));
        }

        /// <summary>
        /// Сранивает содержимое двух карт.
        /// Возвращает значение <see langword="true" /> в случае, когда содержимое и первая буква свойства <see cref="Processor.Tag"/> совпадают, в противном случае - <see langword="false" />.
        /// В случае совпадения по значению ссылки или различиям по размерам карт, выдаётся соответствующее исключение.
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
            if (string.Compare(new string(pOne.Tag[0], 1), new string(pTwo.Tag[0], 1),
                    StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            for (int y = 0; y < pOne.Height; y++)
                for (int x = 0; x < pOne.Width; x++)
                    if (pOne[x, y] != pTwo[x, y])
                        return false;
            return true;
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
