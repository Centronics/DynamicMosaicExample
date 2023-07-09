using System;
using System.Drawing;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    /// <summary>
    /// Реализация потокобезопасного хранилища карт <see cref="ConcurrentProcessorStorage"/>.
    /// </summary>
    /// <remarks>
    /// Учитывает особенности обработки карт, на которых производится поиск запрашиваемых данных.
    /// </remarks>
    public sealed class RecognizeProcessorStorage : ConcurrentProcessorStorage
    {
        /// <summary>
        /// Высота хранимых изображений.
        /// </summary>
        readonly int _height;

        /// <summary>
        /// Максимальная ширина хранимых изображений.
        /// </summary>
        readonly int _maxWidth;

        /// <summary>
        /// Минимальная ширина хранимых изображений.
        /// </summary>
        readonly int _minWidth;

        /// <summary>
        /// Инициализирует текущий экземпляр, устанавливая параметры хранимых карт.
        /// </summary>
        /// <param name="minWidth">Минимальная ширина хранимых изображений.</param>
        /// <param name="maxWidth">Максимальная ширина хранимых изображений.</param>
        /// <param name="height">Высота хранимых изображений.</param>
        /// <param name="extImg">Параметр <see cref="ConcurrentProcessorStorage.ExtImg"/>.</param>
        public RecognizeProcessorStorage(int minWidth, int maxWidth, int height, string extImg) : base(extImg)
        {
            if (string.IsNullOrWhiteSpace(extImg))
                throw new ArgumentNullException(nameof(extImg),
                    $@"Расширение загружаемых изображений должно быть указано ({extImg ?? @"null"}).");

            _minWidth = minWidth;
            _maxWidth = maxWidth;
            _height = height;
        }

        /// <summary>
        /// Рабочий каталог хранилища <see cref="RecognizeProcessorStorage"/>.
        /// </summary>
        /// <remarks>
        /// Подробнее см. <see cref="ConcurrentProcessorStorage.WorkingDirectory"/>.
        /// Содержится в <see cref="FrmExample.RecognizeImagesPath"/>.
        /// </remarks>
        public override string WorkingDirectory => FrmExample.RecognizeImagesPath;

        /// <summary>
        /// Тип хранилища <see cref="RecognizeProcessorStorage"/>.
        /// </summary>
        /// <remarks>
        /// Подробнее см. <see cref="ConcurrentProcessorStorage.StorageType"/>.
        /// Является <see cref="ConcurrentProcessorStorage.ProcessorStorageType.RECOGNIZE"/>.
        /// </remarks>
        public override ProcessorStorageType StorageType => ProcessorStorageType.RECOGNIZE;

        /// <summary>
        /// Считывает карту по указанному пути (не добавляя её в коллекцию), выполняя все необходимые проверки, характерные для хранилища <see cref="RecognizeProcessorStorage"/>.
        /// </summary>
        /// <param name="fullPath">Абсолютный путь к карте.</param>
        /// <returns>Возвращает считанную карту.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Проверяет карту на соответствие по ширине и высоте, для хранилища <see cref="RecognizeProcessorStorage"/>.
        /// В том числе, метод выполняет проверку значения <see cref="Color.A"/> в считанном изображении, с помощью метода <see cref="ConcurrentProcessorStorage.CheckBitmapByAlphaColor(Bitmap)"/>, который, в случае неудачной проверки, выбрасывает исключение <see cref="InvalidOperationException"/>.
        /// Если значение какого-либо параметра не соответствует ожидаемому, метод выбросит одно из нижеперечисленных исключений.
        /// При обработке исключений необходимо проверять свойство <see cref="Exception.InnerException"/>, т.к. в нём находится первоначальное исключение.
        /// При указании относительного пути возможны различные коллизии, поэтому рекомендуется всегда указывать только абсолютный путь.
        /// <paramref name="fullPath"/> не должен содержать недопустимые символы (<see cref="Path.GetInvalidPathChars()"/>), в том числе, быть пустым (<see langword="null"/>, <see cref="string.Empty"/> или состоять из пробелов), иначе метод выбросит исключение <see cref="ArgumentException"/>.
        /// </remarks>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="Exception"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <seealso cref="ReadBitmap(string)"/>
        /// <seealso cref="GetProcessorTag(string)"/>
        /// <seealso cref="ConcurrentProcessorStorage.ParseName(string)"/>
        protected override Processor GetAddingProcessor(string fullPath)
        {
            string tag = GetProcessorTag(fullPath);
            Bitmap btm = ReadBitmap(fullPath);

            try
            {
                return new Processor(btm, tag);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(GetAddingProcessor)}: {ex.Message}{Environment.NewLine}Путь: {fullPath}.", ex);
            }
        }

        /// <summary>
        ///     Сохраняет указанную карту на жёсткий диск (в хранилище <see cref="RecognizeProcessorStorage"/>), в формате <see cref="System.Drawing.Imaging.ImageFormat.Bmp"/>.
        /// </summary>
        /// <param name="processor">Карта, которую требуется сохранить.</param>
        /// <returns>Возвращает путь к сохранённой карте.</returns>
        /// <remarks>
        /// Если рабочий каталог <see cref="ConcurrentProcessorStorage.WorkingDirectory"/> отсутствует, то он будет автоматически создан этим методом.
        /// Сохраняет указанную карту с расширением <see cref="ConcurrentProcessorStorage.ExtImg"/>.
        /// Карта сохраняется под уникальным именем.
        /// Устанавливает значение <see cref="ConcurrentProcessorStorage.SelectedPath"/>.
        /// </remarks>
        /// <seealso cref="ConcurrentProcessorStorage.SelectedPath"/>
        /// <seealso cref="ConcurrentProcessorStorage.CreateWorkingDirectory()"/>
        public string SaveToFile(Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor),
                    $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                CreateWorkingDirectory();
                string path = GetUniquePath(processor.Tag);
                SaveToFile(ImageRect.GetBitmap(processor), path);
                SelectedPath = path;
                return path;
            }
        }

        /// <summary>
        /// Извлекает значение свойства <see cref="Processor.Tag"/> (запрос на поиск данных) из указанного пути.
        /// </summary>
        /// <param name="fullPath">Путь, из которого требуется извлечь значение свойства <see cref="Processor.Tag"/>.</param>
        /// <returns>Возвращает значение свойства <see cref="Processor.Tag"/>.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Получает имя без расширения. Для этого использует метод <see cref="Path.GetFileNameWithoutExtension(string)"/>.
        /// С помощью метода <see cref="ConcurrentProcessorStorage.ParseName(string)"/> выполняет демаскировку имени карты, в случае необходимости.
        /// Путь может быть как абсолютным, так и относительным.
        /// <paramref name="fullPath"/> не должен содержать недопустимые символы (<see cref="Path.GetInvalidPathChars()"/>), в том числе, быть пустым (<see langword="null"/>, <see cref="string.Empty"/> или состоять из пробелов), иначе метод выбросит исключение <see cref="ArgumentException"/>.
        /// </remarks>
        /// <exception cref="ArgumentException"/>
        /// <seealso cref="ConcurrentProcessorStorage.ParseName(string)"/>
        /// <seealso cref="Path.GetFileNameWithoutExtension(string)"/>
        /// <seealso cref="Path.GetInvalidPathChars()"/>
        static string GetProcessorTag(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException($@"{nameof(GetProcessorTag)}: Обнаружен пустой параметр, значение ({fullPath ?? @"<null>"}).", nameof(fullPath));

            try
            {
                return ParseName(Path.GetFileNameWithoutExtension(fullPath)).name;
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(GetProcessorTag)}: {ex.Message}{Environment.NewLine}Путь: {fullPath}.", ex);
            }
        }

        /// <summary>
        /// Считывает изображение по указанному пути, выполняя все требуемые проверки.
        /// </summary>
        /// <param name="fullPath">Путь к изображению.</param>
        /// <returns>Возвращает считанное изображение.</returns>
        /// <remarks>
        /// Метод потокобезопасен.
        /// Проверяет его на соответствие по ширине и высоте, для хранилища <see cref="RecognizeProcessorStorage"/>.
        /// В том числе, метод выполняет проверку компоненты <see cref="Color.A"/> в считанном изображении, с помощью метода <see cref="ConcurrentProcessorStorage.CheckBitmapByAlphaColor(Bitmap)"/>, который, в случае неудачной проверки, выбрасывает исключение <see cref="InvalidOperationException"/>.
        /// Если значение какого-либо параметра не соответствует ожидаемому, метод выбросит одно из нижеперечисленных исключений.
        /// При обработке исключений необходимо проверять свойство <see cref="Exception.InnerException"/>, т.к. в нём находится первоначальное исключение.
        /// </remarks>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="Exception"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <seealso cref="ConcurrentProcessorStorage.CheckBitmapByAlphaColor(Bitmap)"/>
        /// <seealso cref="FrmExample.DefaultOpacity"/>
        /// <seealso cref="FrmExample.CheckAlphaColor(Color)"/>
        new Bitmap ReadBitmap(string fullPath)
        {
            Bitmap btm;

            try
            {
                btm = ConcurrentProcessorStorage.ReadBitmap(fullPath);
            }
            catch (FormatException fx)
            {
                throw new FormatException($@"{nameof(ReadBitmap)}: Ошибка при загрузке изображения по пути: {fullPath}.{Environment.NewLine}Текст ошибки: ""{fx.Message}"".", fx);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(ReadBitmap)}: Ошибка при загрузке изображения по пути: {fullPath}.{Environment.NewLine}Текст ошибки: ""{ex.Message}"".", ex);
            }

            if (btm.Width < _minWidth || btm.Width > _maxWidth)
            {
                int w = btm.Width;
                btm.Dispose();

                throw new ArgumentException($@"{nameof(ReadBitmap)}: Загружаемое изображение не подходит по ширине: {w}. Она выходит за рамки допустимого ({_minWidth};{_maxWidth}).{Environment.NewLine}Путь: {fullPath}.");
            }

            if (btm.Height != _height)
            {
                int h = btm.Height;
                btm.Dispose();

                throw new ArgumentException($@"{nameof(ReadBitmap)}: Загружаемое изображение не подходит по высоте: {h}; необходимо: {_height}.{Environment.NewLine}Путь: {fullPath}.");
            }

            btm.SetPixel(0, 0, btm.GetPixel(0, 0)); // Необходим для устранения "Ошибки общего вида в GDI+" при попытке сохранения загруженного файла.

            return btm;
        }

        /// <summary>
        ///     Добавляет указанную карту в <see cref="RecognizeProcessorStorage" />.
        /// </summary>
        /// <param name="hashCode">Хеш добавляемой карты.</param>
        /// <param name="fullPath">Полный путь к добавляемой карте.</param>
        /// <param name="processor">Добавляемая карта.</param>
        /// <remarks>
        /// Ключевой особенностью реализации этого метода в классе <see cref="RecognizeProcessorStorage" /> является то, что он позволяет выбрать желаемую карту,
        /// перезагрузив её из рабочего каталога <see cref="ConcurrentProcessorStorage.WorkingDirectory"/>, для этого она изначально должна там располагаться.
        /// В этом случае, в свойстве <see cref="ConcurrentProcessorStorage.SelectedPath"/> будет содержаться значение параметра <paramref name="fullPath"/>,
        /// индекс выбранной карты содержится в свойстве <see cref="ConcurrentProcessorStorage.SelectedIndex"/> и, как следствие,
        /// свойство <see cref="ConcurrentProcessorStorage.IsSelectedOne"/> примет значение <see langword="true"/>.
        /// В противном случае, свойство <see cref="ConcurrentProcessorStorage.SelectedPath"/>, как и все вышеуказанные, будет содержать прежнее значение.
        /// На этот метод влияет флаг <see cref="ConcurrentProcessorStorage.LongOperationsAllowed"/>.
        /// Если прервать выполнение метода с помощью флага <see cref="ConcurrentProcessorStorage.LongOperationsAllowed"/>, то значения вышеуказанных свойств не определены.
        /// Путь к добавляемой карте на достоверность не проверяется. Имеется ввиду, что необходимо удостовериться в том, что указанный <paramref name="fullPath"/> указывает на карту, а не на папку.
        /// Дело в том, что, поскольку этот метод перезагружает карту (удаляет и добавляет), он использует функцию <see cref="ConcurrentProcessorStorage.RemoveProcessor(string)"/>, которая может массово удалять карты из хранилища.
        /// Таким образом, задав неверный <paramref name="fullPath"/>, можно получить непредсказуемый результат.
        /// Метод НЕ является потокобезопасным.
        /// </remarks>
        /// <seealso cref="ConcurrentProcessorStorage.SelectedPath"/>
        /// <seealso cref="ConcurrentProcessorStorage.LongOperationsAllowed"/>
        /// <seealso cref="ConcurrentProcessorStorage.IsSelectedOne"/>
        /// <seealso cref="ConcurrentProcessorStorage.SelectedIndex"/>
        /// <seealso cref="ConcurrentProcessorStorage.RemoveProcessor(string)"/>
        protected override void ReplaceElement(int hashCode, string fullPath, Processor processor)
        {
            bool needReplace = RemoveProcessor(fullPath);

            if (!LongOperationsAllowed)
                return;

            BaseAddElement(hashCode, fullPath, processor);

            if (needReplace)
                SelectedPath = fullPath;
        }
    }
}