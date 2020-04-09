using System;
using System.Collections.Generic;
using System.Linq;
using Processor = DynamicParser.Processor;

namespace DynamicMosaicExample
{
    sealed class ProcessorStorage
    {
        readonly Dictionary<int, ProcHash> _dictionary = new Dictionary<int, ProcHash>();

        struct ProcHash
        {
            readonly List<Processor> _lst;

            public ProcHash(Processor p)
            {
                if (p is null)
                    throw new ArgumentNullException();
                _lst = new List<Processor> { p };
            }

            public void AddProcessor(Processor p)
            {
                if (p is null)
                    throw new ArgumentNullException();
                _lst.Add(p);
            }

            public IEnumerable<Processor> Elements => _lst;
        }

        public void AddProcessor(Processor p)
        {
            if (p is null)
                throw new ArgumentNullException();
            int hashCode = CRC8Calc.GetHash(p);
            if (!_dictionary.TryGetValue(hashCode, out ProcHash pr))
                _dictionary.Add(hashCode, new ProcHash(p));
            else
            if (pr.Elements.All(px => !ProcessorCompare(p, px)))
                pr.AddProcessor(p);
        }

        static bool ProcessorCompare(Processor pOne, Processor pTwo)
        {
            if (ReferenceEquals(pOne, pTwo))
                throw new ArgumentException();
            if (pOne is null)
                throw new ArgumentNullException();
            if (pTwo is null)
                throw new ArgumentNullException();
            if (pOne.Width != pTwo.Width || pOne.Height != pTwo.Height)
                throw new ArgumentException();
            for (int y = 0; y < pOne.Height; y++)
                for (int x = 0; x < pOne.Width; x++)
                    if (pOne[x, y] != pTwo[x, y])
                        return false;
            return true;
        }

        IEnumerable<Processor> Processors => _dictionary.SelectMany(pair => pair.Value.Elements);

        /// <summary>
        /// Предназначен для вычисления хеша для определённой последовательности чисел типа <see cref="byte"/>.
        /// </summary>
        public static class CRC8Calc
        {
            public static int GetHash(Processor p)
            {
                if (p == null)
                    throw new ArgumentNullException();
                return GetHash(GetInts(p));
            }

            static IEnumerable<int> GetInts(Processor p)
            {
                if (p == null)
                    throw new ArgumentNullException();
                for (int i = 0; i < p.Height; i++)
                    for (int j = 0; j < p.Width; j++)
                        yield return p[i, j].Value;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="bytes"></param>
            /// <returns></returns>
            static int GetHash(IEnumerable<int> bytes)
            {
                if (bytes == null)
                    throw new ArgumentNullException(nameof(bytes), $@"{nameof(GetHash)}: Для подсчёта контрольной суммы необходимо указать массив байт");
                return bytes.Aggregate(255, (current, t) => Table[current ^ t]);
            }

            static readonly int[] Table;

            static CRC8Calc()
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
