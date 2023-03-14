namespace DynamicMosaicExample
{
    sealed partial class FrmExample
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmExample));
            this.lstHistory = new System.Windows.Forms.ListBox();
            this.txtWord = new System.Windows.Forms.TextBox();
            this.grpImages = new System.Windows.Forms.GroupBox();
            this.txtImagesCount = new System.Windows.Forms.TextBox();
            this.lblImagesCount = new System.Windows.Forms.Label();
            this.btnImageUpToQueries = new System.Windows.Forms.Button();
            this.txtSymbolPath = new System.Windows.Forms.TextBox();
            this.txtImagesNumber = new System.Windows.Forms.TextBox();
            this.btnImageDelete = new System.Windows.Forms.Button();
            this.btnImagePrev = new System.Windows.Forms.Button();
            this.btnImageCreate = new System.Windows.Forms.Button();
            this.btnImageNext = new System.Windows.Forms.Button();
            this.pbBrowse = new System.Windows.Forms.PictureBox();
            this.grpResults = new System.Windows.Forms.GroupBox();
            this.txtConSymbolNumber = new System.Windows.Forms.TextBox();
            this.lblConSymbolEqual = new System.Windows.Forms.Label();
            this.lblConSymbolCount = new System.Windows.Forms.Label();
            this.txtConSymbolTag = new System.Windows.Forms.TextBox();
            this.txtConSymbolCount = new System.Windows.Forms.TextBox();
            this.btnConSaveAllImages = new System.Windows.Forms.Button();
            this.btnConSaveImage = new System.Windows.Forms.Button();
            this.pbConSymbol = new System.Windows.Forms.PictureBox();
            this.btnConNext = new System.Windows.Forms.Button();
            this.btnConPrevious = new System.Windows.Forms.Button();
            this.grpSourceImage = new System.Windows.Forms.GroupBox();
            this.txtRecogCount = new System.Windows.Forms.TextBox();
            this.lblSourceCount = new System.Windows.Forms.Label();
            this.txtRecogPath = new System.Windows.Forms.TextBox();
            this.txtRecogNumber = new System.Windows.Forms.TextBox();
            this.pbSuccess = new System.Windows.Forms.PictureBox();
            this.btnRecogPrev = new System.Windows.Forms.Button();
            this.btnRecogNext = new System.Windows.Forms.Button();
            this.btnDeleteRecognizeImage = new System.Windows.Forms.Button();
            this.btnWide = new System.Windows.Forms.Button();
            this.btnNarrow = new System.Windows.Forms.Button();
            this.btnLoadRecognizeImage = new System.Windows.Forms.Button();
            this.btnSaveRecognizeImage = new System.Windows.Forms.Button();
            this.pbDraw = new System.Windows.Forms.PictureBox();
            this.btnRecognizeImage = new System.Windows.Forms.Button();
            this.btnClearImage = new System.Windows.Forms.Button();
            this.dlgOpenImage = new System.Windows.Forms.OpenFileDialog();
            this.grpImages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).BeginInit();
            this.grpResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).BeginInit();
            this.grpSourceImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            this.SuspendLayout();
            // 
            // lstHistory
            // 
            this.lstHistory.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstHistory.ColumnWidth = 125;
            this.lstHistory.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstHistory.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lstHistory.Location = new System.Drawing.Point(2, 14);
            this.lstHistory.Name = "lstHistory";
            this.lstHistory.ScrollAlwaysVisible = true;
            this.lstHistory.Size = new System.Drawing.Size(125, 132);
            this.lstHistory.TabIndex = 17;
            this.lstHistory.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.LstResults_DrawItem);
            this.lstHistory.SelectedIndexChanged += new System.EventHandler(this.LstResults_SelectedIndexChanged);
            // 
            // txtWord
            // 
            this.txtWord.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtWord.Location = new System.Drawing.Point(2, 88);
            this.txtWord.MaxLength = 6;
            this.txtWord.Name = "txtWord";
            this.txtWord.Size = new System.Drawing.Size(94, 23);
            this.txtWord.TabIndex = 9;
            this.txtWord.Tag = "";
            this.txtWord.TextChanged += new System.EventHandler(this.TxtWord_TextChanged);
            this.txtWord.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtWord_KeyPress);
            // 
            // grpImages
            // 
            this.grpImages.BackColor = System.Drawing.SystemColors.Control;
            this.grpImages.Controls.Add(this.txtImagesCount);
            this.grpImages.Controls.Add(this.lblImagesCount);
            this.grpImages.Controls.Add(this.btnImageUpToQueries);
            this.grpImages.Controls.Add(this.txtSymbolPath);
            this.grpImages.Controls.Add(this.txtImagesNumber);
            this.grpImages.Controls.Add(this.btnImageDelete);
            this.grpImages.Controls.Add(this.btnImagePrev);
            this.grpImages.Controls.Add(this.btnImageCreate);
            this.grpImages.Controls.Add(this.btnImageNext);
            this.grpImages.Controls.Add(this.pbBrowse);
            this.grpImages.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpImages.Location = new System.Drawing.Point(0, 134);
            this.grpImages.Name = "grpImages";
            this.grpImages.Size = new System.Drawing.Size(264, 90);
            this.grpImages.TabIndex = 26;
            this.grpImages.TabStop = false;
            this.grpImages.Text = "Искомые образы";
            // 
            // txtImagesCount
            // 
            this.txtImagesCount.Enabled = false;
            this.txtImagesCount.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtImagesCount.Location = new System.Drawing.Point(236, 13);
            this.txtImagesCount.Name = "txtImagesCount";
            this.txtImagesCount.ReadOnly = true;
            this.txtImagesCount.Size = new System.Drawing.Size(26, 20);
            this.txtImagesCount.TabIndex = 27;
            // 
            // lblImagesCount
            // 
            this.lblImagesCount.AutoSize = true;
            this.lblImagesCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblImagesCount.Location = new System.Drawing.Point(223, 16);
            this.lblImagesCount.Name = "lblImagesCount";
            this.lblImagesCount.Size = new System.Drawing.Size(13, 13);
            this.lblImagesCount.TabIndex = 26;
            this.lblImagesCount.Text = "/";
            // 
            // btnImageUpToQueries
            // 
            this.btnImageUpToQueries.Enabled = false;
            this.btnImageUpToQueries.Image = global::DynamicMosaicExample.Resources.UpToQueries;
            this.btnImageUpToQueries.Location = new System.Drawing.Point(131, 34);
            this.btnImageUpToQueries.Name = "btnImageUpToQueries";
            this.btnImageUpToQueries.Size = new System.Drawing.Size(48, 54);
            this.btnImageUpToQueries.TabIndex = 14;
            this.btnImageUpToQueries.UseVisualStyleBackColor = true;
            this.btnImageUpToQueries.Click += new System.EventHandler(this.BtnImageUpToQueries_Click);
            // 
            // txtSymbolPath
            // 
            this.txtSymbolPath.Enabled = false;
            this.txtSymbolPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtSymbolPath.Location = new System.Drawing.Point(2, 13);
            this.txtSymbolPath.Name = "txtSymbolPath";
            this.txtSymbolPath.ReadOnly = true;
            this.txtSymbolPath.Size = new System.Drawing.Size(194, 20);
            this.txtSymbolPath.TabIndex = 10;
            this.txtSymbolPath.TextChanged += new System.EventHandler(this.TextBoxTextChanged);
            // 
            // txtImagesNumber
            // 
            this.txtImagesNumber.Enabled = false;
            this.txtImagesNumber.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtImagesNumber.Location = new System.Drawing.Point(197, 13);
            this.txtImagesNumber.Name = "txtImagesNumber";
            this.txtImagesNumber.ReadOnly = true;
            this.txtImagesNumber.Size = new System.Drawing.Size(26, 20);
            this.txtImagesNumber.TabIndex = 11;
            // 
            // btnImageDelete
            // 
            this.btnImageDelete.Image = global::DynamicMosaicExample.Resources.DeleteFile;
            this.btnImageDelete.Location = new System.Drawing.Point(223, 34);
            this.btnImageDelete.Name = "btnImageDelete";
            this.btnImageDelete.Size = new System.Drawing.Size(40, 54);
            this.btnImageDelete.TabIndex = 16;
            this.btnImageDelete.UseVisualStyleBackColor = true;
            this.btnImageDelete.Click += new System.EventHandler(this.BtnImageDelete_Click);
            // 
            // btnImagePrev
            // 
            this.btnImagePrev.Enabled = false;
            this.btnImagePrev.Image = global::DynamicMosaicExample.Resources.Previous;
            this.btnImagePrev.Location = new System.Drawing.Point(47, 34);
            this.btnImagePrev.Name = "btnImagePrev";
            this.btnImagePrev.Size = new System.Drawing.Size(40, 54);
            this.btnImagePrev.TabIndex = 12;
            this.btnImagePrev.UseVisualStyleBackColor = true;
            this.btnImagePrev.Click += new System.EventHandler(this.BtnImagePrev_Click);
            // 
            // btnImageCreate
            // 
            this.btnImageCreate.Image = global::DynamicMosaicExample.Resources.CreateImage;
            this.btnImageCreate.Location = new System.Drawing.Point(184, 34);
            this.btnImageCreate.Name = "btnImageCreate";
            this.btnImageCreate.Size = new System.Drawing.Size(40, 54);
            this.btnImageCreate.TabIndex = 15;
            this.btnImageCreate.UseVisualStyleBackColor = true;
            this.btnImageCreate.Click += new System.EventHandler(this.BtnImageCreate_Click);
            // 
            // btnImageNext
            // 
            this.btnImageNext.Enabled = false;
            this.btnImageNext.Image = global::DynamicMosaicExample.Resources.Next;
            this.btnImageNext.Location = new System.Drawing.Point(86, 34);
            this.btnImageNext.Name = "btnImageNext";
            this.btnImageNext.Size = new System.Drawing.Size(40, 54);
            this.btnImageNext.TabIndex = 13;
            this.btnImageNext.UseVisualStyleBackColor = true;
            this.btnImageNext.Click += new System.EventHandler(this.BtnImageNext_Click);
            // 
            // pbBrowse
            // 
            this.pbBrowse.Enabled = false;
            this.pbBrowse.Location = new System.Drawing.Point(3, 36);
            this.pbBrowse.Name = "pbBrowse";
            this.pbBrowse.Size = new System.Drawing.Size(43, 50);
            this.pbBrowse.TabIndex = 0;
            this.pbBrowse.TabStop = false;
            this.pbBrowse.Paint += new System.Windows.Forms.PaintEventHandler(this.PbBrowse_Paint);
            // 
            // grpResults
            // 
            this.grpResults.BackColor = System.Drawing.SystemColors.Control;
            this.grpResults.Controls.Add(this.txtConSymbolNumber);
            this.grpResults.Controls.Add(this.lblConSymbolEqual);
            this.grpResults.Controls.Add(this.lblConSymbolCount);
            this.grpResults.Controls.Add(this.txtConSymbolTag);
            this.grpResults.Controls.Add(this.txtConSymbolCount);
            this.grpResults.Controls.Add(this.btnConSaveAllImages);
            this.grpResults.Controls.Add(this.lstHistory);
            this.grpResults.Controls.Add(this.btnConSaveImage);
            this.grpResults.Controls.Add(this.pbConSymbol);
            this.grpResults.Controls.Add(this.btnConNext);
            this.grpResults.Controls.Add(this.btnConPrevious);
            this.grpResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpResults.ForeColor = System.Drawing.SystemColors.ControlText;
            this.grpResults.Location = new System.Drawing.Point(265, -1);
            this.grpResults.Name = "grpResults";
            this.grpResults.Size = new System.Drawing.Size(129, 225);
            this.grpResults.TabIndex = 27;
            this.grpResults.TabStop = false;
            this.grpResults.Text = "История объекта";
            // 
            // txtConSymbolNumber
            // 
            this.txtConSymbolNumber.Enabled = false;
            this.txtConSymbolNumber.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtConSymbolNumber.Location = new System.Drawing.Point(2, 148);
            this.txtConSymbolNumber.Name = "txtConSymbolNumber";
            this.txtConSymbolNumber.ReadOnly = true;
            this.txtConSymbolNumber.Size = new System.Drawing.Size(26, 20);
            this.txtConSymbolNumber.TabIndex = 23;
            // 
            // lblConSymbolEqual
            // 
            this.lblConSymbolEqual.AutoSize = true;
            this.lblConSymbolEqual.Enabled = false;
            this.lblConSymbolEqual.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblConSymbolEqual.Location = new System.Drawing.Point(74, 151);
            this.lblConSymbolEqual.Name = "lblConSymbolEqual";
            this.lblConSymbolEqual.Size = new System.Drawing.Size(14, 13);
            this.lblConSymbolEqual.TabIndex = 26;
            this.lblConSymbolEqual.Text = "=";
            // 
            // lblConSymbolCount
            // 
            this.lblConSymbolCount.AutoSize = true;
            this.lblConSymbolCount.Enabled = false;
            this.lblConSymbolCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblConSymbolCount.Location = new System.Drawing.Point(30, 152);
            this.lblConSymbolCount.Name = "lblConSymbolCount";
            this.lblConSymbolCount.Size = new System.Drawing.Size(13, 13);
            this.lblConSymbolCount.TabIndex = 25;
            this.lblConSymbolCount.Text = "/";
            // 
            // txtConSymbolTag
            // 
            this.txtConSymbolTag.Enabled = false;
            this.txtConSymbolTag.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtConSymbolTag.Location = new System.Drawing.Point(91, 148);
            this.txtConSymbolTag.Name = "txtConSymbolTag";
            this.txtConSymbolTag.ReadOnly = true;
            this.txtConSymbolTag.Size = new System.Drawing.Size(36, 20);
            this.txtConSymbolTag.TabIndex = 24;
            // 
            // txtConSymbolCount
            // 
            this.txtConSymbolCount.Enabled = false;
            this.txtConSymbolCount.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.txtConSymbolCount.Location = new System.Drawing.Point(45, 148);
            this.txtConSymbolCount.Name = "txtConSymbolCount";
            this.txtConSymbolCount.ReadOnly = true;
            this.txtConSymbolCount.Size = new System.Drawing.Size(26, 20);
            this.txtConSymbolCount.TabIndex = 23;
            // 
            // btnConSaveAllImages
            // 
            this.btnConSaveAllImages.Enabled = false;
            this.btnConSaveAllImages.Image = ((System.Drawing.Image)(resources.GetObject("btnConSaveAllImages.Image")));
            this.btnConSaveAllImages.Location = new System.Drawing.Point(41, 169);
            this.btnConSaveAllImages.Name = "btnConSaveAllImages";
            this.btnConSaveAllImages.Size = new System.Drawing.Size(41, 27);
            this.btnConSaveAllImages.TabIndex = 20;
            this.btnConSaveAllImages.UseVisualStyleBackColor = true;
            this.btnConSaveAllImages.Click += new System.EventHandler(this.BtnConSaveAllImages_Click);
            // 
            // btnConSaveImage
            // 
            this.btnConSaveImage.Enabled = false;
            this.btnConSaveImage.Image = ((System.Drawing.Image)(resources.GetObject("btnConSaveImage.Image")));
            this.btnConSaveImage.Location = new System.Drawing.Point(1, 169);
            this.btnConSaveImage.Name = "btnConSaveImage";
            this.btnConSaveImage.Size = new System.Drawing.Size(41, 27);
            this.btnConSaveImage.TabIndex = 19;
            this.btnConSaveImage.UseVisualStyleBackColor = true;
            this.btnConSaveImage.Click += new System.EventHandler(this.BtnConSaveImage_Click);
            // 
            // pbConSymbol
            // 
            this.pbConSymbol.Enabled = false;
            this.pbConSymbol.Location = new System.Drawing.Point(83, 171);
            this.pbConSymbol.Name = "pbConSymbol";
            this.pbConSymbol.Size = new System.Drawing.Size(43, 50);
            this.pbConSymbol.TabIndex = 19;
            this.pbConSymbol.TabStop = false;
            this.pbConSymbol.Paint += new System.Windows.Forms.PaintEventHandler(this.PbConSymbol_Paint);
            // 
            // btnConNext
            // 
            this.btnConNext.Enabled = false;
            this.btnConNext.Image = ((System.Drawing.Image)(resources.GetObject("btnConNext.Image")));
            this.btnConNext.Location = new System.Drawing.Point(41, 195);
            this.btnConNext.Name = "btnConNext";
            this.btnConNext.Size = new System.Drawing.Size(41, 28);
            this.btnConNext.TabIndex = 22;
            this.btnConNext.UseVisualStyleBackColor = true;
            this.btnConNext.Click += new System.EventHandler(this.BtnConNext_Click);
            // 
            // btnConPrevious
            // 
            this.btnConPrevious.Enabled = false;
            this.btnConPrevious.Image = ((System.Drawing.Image)(resources.GetObject("btnConPrevious.Image")));
            this.btnConPrevious.Location = new System.Drawing.Point(1, 195);
            this.btnConPrevious.Name = "btnConPrevious";
            this.btnConPrevious.Size = new System.Drawing.Size(41, 28);
            this.btnConPrevious.TabIndex = 21;
            this.btnConPrevious.UseVisualStyleBackColor = true;
            this.btnConPrevious.Click += new System.EventHandler(this.BtnConPrevious_Click);
            // 
            // grpSourceImage
            // 
            this.grpSourceImage.BackColor = System.Drawing.SystemColors.Control;
            this.grpSourceImage.Controls.Add(this.txtRecogCount);
            this.grpSourceImage.Controls.Add(this.lblSourceCount);
            this.grpSourceImage.Controls.Add(this.txtRecogPath);
            this.grpSourceImage.Controls.Add(this.txtRecogNumber);
            this.grpSourceImage.Controls.Add(this.pbSuccess);
            this.grpSourceImage.Controls.Add(this.btnRecogPrev);
            this.grpSourceImage.Controls.Add(this.txtWord);
            this.grpSourceImage.Controls.Add(this.btnRecogNext);
            this.grpSourceImage.Controls.Add(this.btnDeleteRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnWide);
            this.grpSourceImage.Controls.Add(this.btnNarrow);
            this.grpSourceImage.Controls.Add(this.btnLoadRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnSaveRecognizeImage);
            this.grpSourceImage.Controls.Add(this.pbDraw);
            this.grpSourceImage.Controls.Add(this.btnRecognizeImage);
            this.grpSourceImage.Controls.Add(this.btnClearImage);
            this.grpSourceImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grpSourceImage.Location = new System.Drawing.Point(0, -1);
            this.grpSourceImage.Name = "grpSourceImage";
            this.grpSourceImage.Size = new System.Drawing.Size(264, 138);
            this.grpSourceImage.TabIndex = 24;
            this.grpSourceImage.TabStop = false;
            this.grpSourceImage.Text = "Рисование (ЛКМ / ПКМ)";
            // 
            // txtRecogCount
            // 
            this.txtRecogCount.Enabled = false;
            this.txtRecogCount.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtRecogCount.Location = new System.Drawing.Point(236, 14);
            this.txtRecogCount.Name = "txtRecogCount";
            this.txtRecogCount.ReadOnly = true;
            this.txtRecogCount.Size = new System.Drawing.Size(26, 20);
            this.txtRecogCount.TabIndex = 31;
            // 
            // lblSourceCount
            // 
            this.lblSourceCount.AutoSize = true;
            this.lblSourceCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblSourceCount.Location = new System.Drawing.Point(223, 17);
            this.lblSourceCount.Name = "lblSourceCount";
            this.lblSourceCount.Size = new System.Drawing.Size(13, 13);
            this.lblSourceCount.TabIndex = 30;
            this.lblSourceCount.Text = "/";
            // 
            // txtRecogPath
            // 
            this.txtRecogPath.Enabled = false;
            this.txtRecogPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtRecogPath.Location = new System.Drawing.Point(2, 14);
            this.txtRecogPath.Name = "txtRecogPath";
            this.txtRecogPath.ReadOnly = true;
            this.txtRecogPath.Size = new System.Drawing.Size(194, 20);
            this.txtRecogPath.TabIndex = 28;
            this.txtRecogPath.TextChanged += new System.EventHandler(this.TextBoxTextChanged);
            // 
            // txtRecogNumber
            // 
            this.txtRecogNumber.Enabled = false;
            this.txtRecogNumber.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtRecogNumber.Location = new System.Drawing.Point(197, 14);
            this.txtRecogNumber.Name = "txtRecogNumber";
            this.txtRecogNumber.ReadOnly = true;
            this.txtRecogNumber.Size = new System.Drawing.Size(26, 20);
            this.txtRecogNumber.TabIndex = 29;
            // 
            // pbSuccess
            // 
            this.pbSuccess.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbSuccess.Image = global::DynamicMosaicExample.Resources.Result_Unknown;
            this.pbSuccess.Location = new System.Drawing.Point(96, 88);
            this.pbSuccess.Name = "pbSuccess";
            this.pbSuccess.Size = new System.Drawing.Size(28, 23);
            this.pbSuccess.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSuccess.TabIndex = 24;
            this.pbSuccess.TabStop = false;
            // 
            // btnRecogPrev
            // 
            this.btnRecogPrev.Image = global::DynamicMosaicExample.Resources.PreviousImage;
            this.btnRecogPrev.Location = new System.Drawing.Point(194, 111);
            this.btnRecogPrev.Name = "btnRecogPrev";
            this.btnRecogPrev.Size = new System.Drawing.Size(35, 25);
            this.btnRecogPrev.TabIndex = 7;
            this.btnRecogPrev.UseVisualStyleBackColor = true;
            this.btnRecogPrev.Click += new System.EventHandler(this.BtnPrevRecogImage_Click);
            // 
            // btnRecogNext
            // 
            this.btnRecogNext.Image = global::DynamicMosaicExample.Resources.NextImage;
            this.btnRecogNext.Location = new System.Drawing.Point(228, 111);
            this.btnRecogNext.Name = "btnRecogNext";
            this.btnRecogNext.Size = new System.Drawing.Size(35, 25);
            this.btnRecogNext.TabIndex = 8;
            this.btnRecogNext.UseVisualStyleBackColor = true;
            this.btnRecogNext.Click += new System.EventHandler(this.BtnNextRecogImage_Click);
            // 
            // btnDeleteRecognizeImage
            // 
            this.btnDeleteRecognizeImage.Enabled = false;
            this.btnDeleteRecognizeImage.Image = global::DynamicMosaicExample.Resources.DeleteFile;
            this.btnDeleteRecognizeImage.Location = new System.Drawing.Point(159, 111);
            this.btnDeleteRecognizeImage.Name = "btnDeleteRecognizeImage";
            this.btnDeleteRecognizeImage.Size = new System.Drawing.Size(35, 25);
            this.btnDeleteRecognizeImage.TabIndex = 2;
            this.btnDeleteRecognizeImage.UseVisualStyleBackColor = true;
            this.btnDeleteRecognizeImage.Click += new System.EventHandler(this.BtnDeleteRecognizeImage_Click);
            // 
            // btnWide
            // 
            this.btnWide.Enabled = false;
            this.btnWide.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold);
            this.btnWide.Image = global::DynamicMosaicExample.Resources.ExpandRight;
            this.btnWide.Location = new System.Drawing.Point(228, 87);
            this.btnWide.Name = "btnWide";
            this.btnWide.Size = new System.Drawing.Size(35, 25);
            this.btnWide.TabIndex = 4;
            this.btnWide.UseVisualStyleBackColor = true;
            this.btnWide.Click += new System.EventHandler(this.BtnWide_Click);
            // 
            // btnNarrow
            // 
            this.btnNarrow.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnNarrow.Image = global::DynamicMosaicExample.Resources.ExpandLeft;
            this.btnNarrow.Location = new System.Drawing.Point(194, 87);
            this.btnNarrow.Name = "btnNarrow";
            this.btnNarrow.Size = new System.Drawing.Size(35, 25);
            this.btnNarrow.TabIndex = 3;
            this.btnNarrow.UseVisualStyleBackColor = true;
            this.btnNarrow.Click += new System.EventHandler(this.BtnNarrow_Click);
            // 
            // btnLoadRecognizeImage
            // 
            this.btnLoadRecognizeImage.Image = global::DynamicMosaicExample.Resources.OpenFile;
            this.btnLoadRecognizeImage.Location = new System.Drawing.Point(159, 87);
            this.btnLoadRecognizeImage.Name = "btnLoadRecognizeImage";
            this.btnLoadRecognizeImage.Size = new System.Drawing.Size(35, 25);
            this.btnLoadRecognizeImage.TabIndex = 6;
            this.btnLoadRecognizeImage.UseVisualStyleBackColor = true;
            this.btnLoadRecognizeImage.Click += new System.EventHandler(this.BtnLoadRecognizeImage_Click);
            // 
            // btnSaveRecognizeImage
            // 
            this.btnSaveRecognizeImage.Enabled = false;
            this.btnSaveRecognizeImage.Image = global::DynamicMosaicExample.Resources.SaveFile;
            this.btnSaveRecognizeImage.Location = new System.Drawing.Point(125, 87);
            this.btnSaveRecognizeImage.Name = "btnSaveRecognizeImage";
            this.btnSaveRecognizeImage.Size = new System.Drawing.Size(35, 25);
            this.btnSaveRecognizeImage.TabIndex = 5;
            this.btnSaveRecognizeImage.UseVisualStyleBackColor = true;
            this.btnSaveRecognizeImage.Click += new System.EventHandler(this.BtnSaveRecognizeImage_Click);
            // 
            // pbDraw
            // 
            this.pbDraw.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pbDraw.Location = new System.Drawing.Point(3, 36);
            this.pbDraw.MaximumSize = new System.Drawing.Size(258, 50);
            this.pbDraw.MinimumSize = new System.Drawing.Size(43, 50);
            this.pbDraw.Name = "pbDraw";
            this.pbDraw.Size = new System.Drawing.Size(258, 50);
            this.pbDraw.TabIndex = 0;
            this.pbDraw.TabStop = false;
            this.pbDraw.Paint += new System.Windows.Forms.PaintEventHandler(this.PbDraw_Paint);
            this.pbDraw.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PbDraw_MouseDown);
            this.pbDraw.MouseLeave += new System.EventHandler(this.PbDraw_MouseLeave);
            this.pbDraw.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PbDraw_MouseMove);
            this.pbDraw.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PbDraw_MouseUp);
            // 
            // btnRecognizeImage
            // 
            this.btnRecognizeImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnRecognizeImage.Image = global::DynamicMosaicExample.Resources.Search;
            this.btnRecognizeImage.Location = new System.Drawing.Point(1, 111);
            this.btnRecognizeImage.Name = "btnRecognizeImage";
            this.btnRecognizeImage.Size = new System.Drawing.Size(124, 25);
            this.btnRecognizeImage.TabIndex = 0;
            this.btnRecognizeImage.UseVisualStyleBackColor = true;
            this.btnRecognizeImage.Click += new System.EventHandler(this.BtnRecognizeImage_Click);
            // 
            // btnClearImage
            // 
            this.btnClearImage.Enabled = false;
            this.btnClearImage.Image = global::DynamicMosaicExample.Resources.ClearImage;
            this.btnClearImage.Location = new System.Drawing.Point(125, 111);
            this.btnClearImage.Name = "btnClearImage";
            this.btnClearImage.Size = new System.Drawing.Size(35, 25);
            this.btnClearImage.TabIndex = 1;
            this.btnClearImage.UseVisualStyleBackColor = true;
            this.btnClearImage.Click += new System.EventHandler(this.BtnClearImage_Click);
            // 
            // dlgOpenImage
            // 
            this.dlgOpenImage.Filter = "BMP|*.bmp";
            // 
            // FrmExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(394, 224);
            this.Controls.Add(this.grpSourceImage);
            this.Controls.Add(this.grpResults);
            this.Controls.Add(this.grpImages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmExample";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Тестовый стенд";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmExample_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmExample_FormClosed);
            this.Shown += new System.EventHandler(this.FrmExample_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmExample_KeyDown);
            this.grpImages.ResumeLayout(false);
            this.grpImages.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).EndInit();
            this.grpResults.ResumeLayout(false);
            this.grpResults.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbConSymbol)).EndInit();
            this.grpSourceImage.ResumeLayout(false);
            this.grpSourceImage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSuccess)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbDraw;
        private System.Windows.Forms.Button btnRecognizeImage;
        private System.Windows.Forms.Button btnClearImage;
        private System.Windows.Forms.ListBox lstHistory;
        private System.Windows.Forms.TextBox txtWord;
        private System.Windows.Forms.Button btnImageCreate;
        private System.Windows.Forms.GroupBox grpImages;
        private System.Windows.Forms.GroupBox grpResults;
        private System.Windows.Forms.PictureBox pbBrowse;
        private System.Windows.Forms.Button btnImagePrev;
        private System.Windows.Forms.Button btnImageNext;
        private System.Windows.Forms.Button btnImageDelete;
        private System.Windows.Forms.GroupBox grpSourceImage;
        private System.Windows.Forms.TextBox txtImagesNumber;
        private System.Windows.Forms.Button btnLoadRecognizeImage;
        private System.Windows.Forms.Button btnSaveRecognizeImage;
        private System.Windows.Forms.OpenFileDialog dlgOpenImage;
        private System.Windows.Forms.Button btnWide;
        private System.Windows.Forms.Button btnNarrow;
        private System.Windows.Forms.PictureBox pbConSymbol;
        private System.Windows.Forms.Button btnConPrevious;
        private System.Windows.Forms.Button btnConNext;
        private System.Windows.Forms.PictureBox pbSuccess;
        private System.Windows.Forms.TextBox txtSymbolPath;
        private System.Windows.Forms.TextBox txtConSymbolNumber;
        private System.Windows.Forms.Button btnConSaveImage;
        private System.Windows.Forms.Button btnConSaveAllImages;
        private System.Windows.Forms.Button btnDeleteRecognizeImage;
        private System.Windows.Forms.Button btnRecogPrev;
        private System.Windows.Forms.Button btnRecogNext;
        private System.Windows.Forms.Button btnImageUpToQueries;
        private System.Windows.Forms.TextBox txtConSymbolTag;
        private System.Windows.Forms.TextBox txtConSymbolCount;
        private System.Windows.Forms.Label lblConSymbolEqual;
        private System.Windows.Forms.Label lblConSymbolCount;
        private System.Windows.Forms.TextBox txtImagesCount;
        private System.Windows.Forms.Label lblImagesCount;
        private System.Windows.Forms.TextBox txtRecogCount;
        private System.Windows.Forms.Label lblSourceCount;
        private System.Windows.Forms.TextBox txtRecogPath;
        private System.Windows.Forms.TextBox txtRecogNumber;
    }
}

