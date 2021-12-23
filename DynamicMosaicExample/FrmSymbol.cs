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
        /// <summary>
        ///     Задаёт толщину и цвет выводимой линии.
        /// </summary>
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);

        /// <summary>
        ///     Изображение создаваемого образа.
        /// </summary>
        readonly Bitmap _btmFront;

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        readonly Color _defaultColor = Color.White;

        /// <summary>
        ///     Поверхность для рисования образа.
        /// </summary>
        readonly Graphics _grFront;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае запрещён.
        /// </summary>
        bool _draw;

        /// <summary>
        ///     Необходимо для обозначения временного интервала, необходимого для задержки реакции на нажатую клавишу.
        /// </summary>
        bool _timedOut;

        /// <summary>
        ///     Хранит загруженные карты, которые требуется искать на основной карте.
        ///     Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ConcurrentProcessorStorage _imagesProcessorStorage;

        /// <summary>
        ///     Конструктор формы ввода нового искомого символа.
        /// </summary>
        internal FrmSymbol(ConcurrentProcessorStorage imagesProcessorStorage)
        {
            _imagesProcessorStorage = imagesProcessorStorage ?? throw new ArgumentNullException(nameof(imagesProcessorStorage), @"Хранилище карт должно быть указано.");
            InitializeComponent();
            _btmFront = new Bitmap(pbBox.Width, pbBox.Height);
            _grFront = Graphics.FromImage(_btmFront);
            pbBox.Image = _btmFront;
        }

        /// <summary>
        ///     Запрещает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbBox_MouseUp(object sender, MouseEventArgs e) => _draw = false;

        /// <summary>
        ///     Запрещает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void PbBox_MouseLeave(object sender, EventArgs e) => _draw = false;

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
                _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
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
                _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
                btnClear.Enabled = true;
            });
            RunAction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Сохраняет текущий образ искомой буквы.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnOK_Click(object sender, EventArgs e) => RunAction(() =>
        {
            if (string.IsNullOrWhiteSpace(txtSymbol.Text))
            {
                MessageBox.Show(this,
                    @"Необходимо вписать название символа. Оно не может быть более одного знака и состоять из невидимых символов.");
                tmrPressWait.Enabled = true;
                _timedOut = false;
                return;
            }

            _imagesProcessorStorage.SaveToFile(new Processor(_btmFront, txtSymbol.Text[0].ToString()));
            DialogResult = DialogResult.OK;
        });

        /// <summary>
        ///     Очищает поверхность для рисования искомого образа.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnClear_Click(object sender, EventArgs e)
        {
            RunAction(() =>
            {
                _grFront.Clear(_defaultColor);
                btnClear.Enabled = false;
            });
            RunAction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Подготавливает поверхность для рисования искомого образа.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_Shown(object sender, EventArgs e) => RunAction(() =>
        {
            BtnClear_Click(null, null);
            tmrPressWait.Enabled = true;
        });

        /// <summary>
        ///     Обрабатывает нажатия клавиш пользователем.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Alt || e.Control || e.Shift)
                return;
            RunAction(() =>
            {
                if (!_timedOut)
                    return;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        BtnOK_Click(null, null);
                        break;
                    case Keys.Escape:
                        DialogResult = DialogResult.Cancel;
                        break;
                }
            });
        }

        /// <summary>
        ///     Предотвращает реакцию системы на некорректный ввод.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtSymbol_KeyPress(object sender, KeyPressEventArgs e) => RunAction(() =>
        {
            if ((Keys) e.KeyChar == Keys.Enter || (Keys) e.KeyChar == Keys.Tab ||
                (Keys) e.KeyChar == Keys.Escape ||
                (Keys) e.KeyChar == Keys.Pause || (Keys) e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                e.Handled = true;
        });

        /// <summary>
        ///     Предотвращает реакцию системы на некорректный ввод.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_KeyPress(object sender, KeyPressEventArgs e) => RunAction(() =>
        {
            if ((Keys) e.KeyChar == Keys.Enter || (Keys) e.KeyChar == Keys.Tab || (Keys) e.KeyChar == Keys.Escape ||
                (Keys) e.KeyChar == Keys.Pause || (Keys) e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                e.Handled = true;
        });

        /// <summary>
        ///     Происходит, когда отсчитываемое время реакции на нажатую клавишу подошло к концу.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TmrPressWait_Tick(object sender, EventArgs e)
        {
            _timedOut = true;
            tmrPressWait.Enabled = false;
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
    }
}