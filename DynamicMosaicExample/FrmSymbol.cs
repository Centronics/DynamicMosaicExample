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
        public static Pen BlackPen => FrmExample.BlackPen;

        /// <summary>
        ///     Изображение создаваемого образа.
        /// </summary>
        readonly Bitmap _btmFront;

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        public static Color DefaultColor => FrmExample.DefaultColor;

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
        ///     Хранит загруженные карты, которые требуется искать на основной карте.
        ///     Предназначена для использования несколькими потоками одновременно.
        /// </summary>
        readonly ImageProcessorStorage _imagesProcessorStorage;

        /// <summary>
        ///     Конструктор формы ввода нового искомого символа.
        /// </summary>
        internal FrmSymbol(ImageProcessorStorage imageProcessorStorage)
        {
            _imagesProcessorStorage = imageProcessorStorage ?? throw new ArgumentNullException(nameof(imageProcessorStorage), @"Хранилище карт должно быть указано.");
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
        ///     Сохраняет текущий образ искомой буквы.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void BtnOK_Click(object sender, EventArgs e) => RunAction(() =>
        {
            if (string.IsNullOrWhiteSpace(txtSymbol.Text))
            {
                MessageBox.Show(this, @"Необходимо вписать название символа. Оно не может быть более одного знака и состоять из невидимых символов.");
                return;
            }

            FrmExample.CreateFolder(_imagesProcessorStorage.ImagesPath);

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
        void FrmSymbol_Shown(object sender, EventArgs e) => BtnClear_Click(btnClear, EventArgs.Empty);

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

        bool _txtSymbolTextChecking;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void TxtSymbolTextCheck(object sender, EventArgs e) => RunAction(() =>
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

        void Btn_KeyDown(object sender, KeyEventArgs e) => RunAction(() =>
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    ExitCancel();
                    break;
            }
        });

        void TxtSymbol_KeyDown(object sender, KeyEventArgs e) => RunAction(() =>
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

        void ExitCancel() => DialogResult = DialogResult.Cancel;

        void TxtSymbol_KeyPress(object sender, KeyPressEventArgs e) => RunAction(() =>
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
}