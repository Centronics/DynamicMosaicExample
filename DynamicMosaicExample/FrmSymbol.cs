using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicMosaicExample
{
    /// <summary>
    ///     Форма ввода нового искомого символа.
    /// </summary>
    public partial class FrmSymbol : Form
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
        ///     Поверхность для рисования образа.
        /// </summary>
        readonly Graphics _grFront;

        /// <summary>
        /// Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        /// Используется для стирания изображения.
        /// </summary>
        readonly Color _defaultColor = Color.White;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение true - вывод разрешён, в противном случае - false.
        /// </summary>
        bool _draw;

        /// <summary>
        ///     Необходимо для обозначения временного интервала, необходимого для задержки реакции на нажатую клавишу.
        /// </summary>
        bool _timedOut;

        /// <summary>
        ///     Конструктор формы ввода нового искомого символа.
        /// </summary>
        public FrmSymbol()
        {
            InitializeComponent();
            _btmFront = new Bitmap(pbBox.Width, pbBox.Height);
            _grFront = Graphics.FromImage(_btmFront);
            pbBox.Image = _btmFront;
        }

        /// <summary>
        ///     Текущий образ, который был сохранён.
        ///     Если образ не сохранён, то содержит null, в противном случае содержит образ искомой буквы.
        /// </summary>
        public ImageRect LastImage { get; private set; }

        /// <summary>
        ///     Запрещает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbBox_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        ///     Запрещает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbBox_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        ///     Разрешает вывод создаваемой пользователем линии на экран.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbBox_MouseDown(object sender, MouseEventArgs e)
        {
            RunFunction(() =>
            {
                _draw = true;
                _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            });
            RunFunction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Выводит создаваемую пользователем линию на экран, если вывод разрешён.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbBox_MouseMove(object sender, MouseEventArgs e)
        {
            RunFunction(() =>
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            });
            RunFunction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Сохраняет текущий образ искомой буквы.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnOK_Click(object sender, EventArgs e)
        {
            RunFunction(() =>
            {
                if (string.IsNullOrWhiteSpace(txtSymbol.Text))
                {
                    MessageBox.Show(this,
                        @"Необходимо вписать название символа. Оно не может быть более одного знака и состоять из невидимых символов.");
                    tmrPressWait.Enabled = true;
                    _timedOut = false;
                    return;
                }
                LastImage = ImageRect.Save(txtSymbol.Text[0], _btmFront);
                DialogResult = DialogResult.OK;
            });
        }

        /// <summary>
        ///     Очищает поверхность для рисования искомого образа.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnClear_Click(object sender, EventArgs e)
        {
            RunFunction(() => _grFront.Clear(_defaultColor));
            RunFunction(() => pbBox.Refresh());
        }

        /// <summary>
        ///     Подготавливает поверхность для рисования искомого образа.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_Shown(object sender, EventArgs e)
        {
            RunFunction(() =>
            {
                btnClear_Click(null, null);
                tmrPressWait.Enabled = true;
            });
        }

        /// <summary>
        ///     Обрабатывает нажатия клавиш пользователем.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Alt || e.Control || e.Shift)
                return;
            RunFunction(() =>
            {
                if (!_timedOut)
                    return;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        btnOK_Click(null, null);
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
        void txtSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            RunFunction(() =>
            {
                if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab ||
                    (Keys)e.KeyChar == Keys.Escape ||
                    (Keys)e.KeyChar == Keys.Pause || (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                    e.Handled = true;
            });
        }

        /// <summary>
        ///     Предотвращает реакцию системы на некорректный ввод.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            RunFunction(() =>
            {
                if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab || (Keys)e.KeyChar == Keys.Escape ||
                    (Keys)e.KeyChar == Keys.Pause || (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                    e.Handled = true;
            });
        }

        /// <summary>
        ///     Происходит, когда отсчитываемое время реакции на нажатую клавишу подошло к концу.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void tmrPressWait_Tick(object sender, EventArgs e)
        {
            _timedOut = true;
            tmrPressWait.Enabled = false;
        }

        /// <summary>
        ///     Выполняет функцию с выводом сообщения об ошибке на экран.
        /// </summary>
        /// <param name="act">Выполняемая функция.</param>
        void RunFunction(Action act)
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