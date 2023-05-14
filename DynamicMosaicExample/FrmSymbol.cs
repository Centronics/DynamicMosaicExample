using System;
using System.Drawing;
using System.Windows.Forms;
using DynamicParser;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Форма ввода нового искомого символа.
    /// </summary>
    internal sealed partial class FrmSymbol : Form
    {
        const string SymbolNameIsEmpty =
            @"Необходимо вписать название искомого символа. Оно не может быть более одного знака или состоять из невидимых символов.";

        const string IncorrectSymbolName = @"Недопустимое название искомого символа.";

        /// <summary>
        ///     Изображение создаваемого образа.
        /// </summary>
        readonly Bitmap _btmFront;

        /// <summary>
        ///     Поверхность для рисования образа.
        /// </summary>
        readonly Graphics _grFront;

        /// <summary>
        ///     Хранит загруженные карты, которые требуется искать на основной карте.
        ///     Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ImageProcessorStorage _imagesProcessorStorage;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае запрещён.
        /// </summary>
        bool _draw;

        /// <summary>
        ///     Флаг, необходимый для того, чтобы защититься от возникновения события изменения текста на поле ввода, в процессе обработки такого события.
        /// </summary>
        bool _txtSymbolTextChecking;

        /// <summary>
        ///     Конструктор формы ввода нового искомого символа.
        /// </summary>
        /// <param name="imageProcessorStorage">Хранит загруженные карты, которые требуется искать на основной карте.</param>
        internal FrmSymbol(ImageProcessorStorage imageProcessorStorage)
        {
            _imagesProcessorStorage = imageProcessorStorage ??
                                      throw new ArgumentNullException(nameof(imageProcessorStorage),
                                          @"Хранилище карт должно быть указано.");

            InitializeComponent();

            _btmFront = new Bitmap(pbBox.Width, pbBox.Height);
            _grFront = Graphics.FromImage(_btmFront);
            pbBox.Image = _btmFront;
        }

        /// <summary>
        ///     Задаёт толщину и цвет выводимой линии.
        /// </summary>
        public static Pen BlackPen => FrmExample.BlackPen;

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        public static Color DefaultColor => FrmExample.DefaultColor;

        /// <summary>
        ///     Запрещает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbBox_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        ///     Запрещает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbBox_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        ///     Разрешает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbBox_MouseDown(object sender, MouseEventArgs e)
        {
            RunAction(() =>
            {
                _draw = true;
                _grFront.DrawRectangle(BlackPen, new Rectangle(e.X, e.Y, 1, 1));
                btnClear.Enabled = true;
            });
            RunAction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Выводит создаваемую пользователем линию на экран, если вывод разрешён.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbBox_MouseMove(object sender, MouseEventArgs e)
        {
            RunAction(() =>
            {
                if (!_draw)
                    return;
                _grFront.DrawRectangle(BlackPen, new Rectangle(e.X, e.Y, 1, 1));
                btnClear.Enabled = true;
            });
            RunAction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Сохраняет текущий образ искомой буквы в рабочий каталог и закрывает форму (<see cref="DialogResult.OK"/>).
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnOK_Click(object sender, EventArgs e)
        {
            RunAction(() =>
            {
                string tag = txtSymbol.Text;

                if (string.IsNullOrWhiteSpace(tag))
                {
                    MessageBox.Show(this, SymbolNameIsEmpty);
                    return;
                }

                if (FrmExample.InvalidCharSet.Overlaps(tag))
                {
                    MessageBox.Show(this, IncorrectSymbolName, @"Уведомление", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                _imagesProcessorStorage.SaveToFile(new Processor(_btmFront, tag[0].ToString()));

                DialogResult = DialogResult.OK;
            });
        }

        /// <summary>
        ///     Очищает поверхность рисования искомого образа.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnClear_Click(object sender, EventArgs e)
        {
            RunAction(() =>
            {
                _grFront.Clear(DefaultColor);
                btnClear.Enabled = false;
            });
            RunAction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Подготавливает поверхность для рисования искомого образа.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_Shown(object sender, EventArgs e)
        {
            BtnClear_Click(btnClear, EventArgs.Empty);
        }

        /// <summary>
        ///     Выполняет функцию с выводом сообщения об ошибке на экран.
        /// </summary>
        /// <param name="act">Выполняемая функция.</param>
        void RunAction(Action act)
        {
            if (act == null)
                return;
            try
            {
                act();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Производит контроль вводимого содержимого в поле ввода (<see cref="TextBox.Text"/>) названия символа.
        /// Поле не может содержать знаки пробела, содержать строку длиннее значения <see cref="TextBoxBase.MaxLength"/>, при этом, метод всегда скорректирует строку в большой регистр (<see cref="string.ToUpper()"/>).
        /// Если поле содержит знаки пробела, его значение будет сброшено (<see cref="string.Empty"/>).
        /// Если поле пустое (<see cref="string.Empty"/>), метод не производит никаких действий.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtSymbolTextCheck(object sender, EventArgs e)
        {
            RunAction(() =>
            {
                if (_txtSymbolTextChecking)
                    return;

                string t = txtSymbol.Text;

                if (string.IsNullOrEmpty(t))
                    return;

                _txtSymbolTextChecking = true;

                try
                {
                    if (string.IsNullOrWhiteSpace(t))
                    {
                        txtSymbol.Text = string.Empty;
                        return;
                    }

                    int ml = txtSymbol.MaxLength;

                    t = txtSymbol.Text = t.Length <= ml ? t.ToUpper() : t.Remove(ml).ToUpper();
                    txtSymbol.Select(t.Length, 0);
                }
                finally
                {
                    _txtSymbolTextChecking = false;
                }
            });
        }

        /// <summary>
        /// Обрабатывает события нажатий клавиш над формой: закрывает форму (<see cref="DialogResult.Cancel"/>) при нажатии клавиши <see cref="Keys.Escape"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <seealso cref="ExitCancel()"/>
        void Btn_KeyDown(object sender, KeyEventArgs e)
        {
            RunAction(() =>
            {
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        ExitCancel();
                        break;
                }
            });
        }

        /// <summary>
        /// Обрабатывает события нажатий клавиш над полем ввода названия символа:
        /// 1) Сохраняет текущий образ искомой буквы в рабочий каталог и закрывает форму (<see cref="DialogResult.OK"/>), при нажатии клавиши <see cref="Keys.Enter"/>.
        /// 2) Закрывает форму (<see cref="DialogResult.Cancel"/>) при нажатии клавиши <see cref="Keys.Escape"/>.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <seealso cref="BtnOK_Click(object, EventArgs)"/>
        /// <seealso cref="ExitCancel()"/>
        void TxtSymbol_KeyDown(object sender, KeyEventArgs e)
        {
            RunAction(() =>
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        BtnOK_Click(btnOK, EventArgs.Empty);
                        return;
                    case Keys.Escape:
                        ExitCancel();
                        return;
                }
            });
        }

        /// <summary>
        /// Закрывает форму как диалог (<see cref="DialogResult.Cancel"/>).
        /// </summary>
        /// <seealso cref="Form.DialogResult"/>
        /// <seealso cref="DialogResult"/>
        void ExitCancel()
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Предотвращает (<see cref="KeyPressEventArgs.Handled"/>) реакцию системы (по умолчанию) на нажатие служебной клавиши.
        /// Поддерживаются следующие клавиши:
        /// 1) <see cref="Keys.Enter"/>
        /// 2) <see cref="Keys.Tab"/>
        /// 3) <see cref="Keys.Escape"/>
        /// 4) <see cref="Keys.Pause"/>
        /// 5) <see cref="Keys.XButton1"/>.
        /// В противном случае, будет выполнена очистка (<see cref="string.Empty"/>) поля ввода названия символа перед тем, как его значение будет изменено.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <seealso cref="KeyPressEventArgs.Handled"/>
        void TxtSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            RunAction(() =>
            {
                switch ((Keys)e.KeyChar)
                {
                    case Keys.Enter:
                    case Keys.Tab:
                    case Keys.Escape:
                    case Keys.Pause:
                    case Keys.XButton1:
                    case Keys.RButton | Keys.Enter:
                        e.Handled = true;
                        return;
                }

                txtSymbol.Text = string.Empty;
            });
        }

        /// <summary>
        /// Закрывает все используемые формой ресурсы.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        /// <seealso cref="FrmExample.DisposeImage(PictureBox)"/>
        void FrmSymbol_FormClosing(object sender, FormClosingEventArgs e)
        {
            RunAction(() =>
            {
                _grFront?.Dispose();

                FrmExample.DisposeImage(pbBox);
            });
        }
    }
}