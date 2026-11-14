namespace DocumentRecognition.WindowsForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            imageViewer1 = new ImageViewerControl();
            button1 = new Button();
            tabControl1 = new TabControl();
            tabPageTitleBlock = new TabPage();
            dgvSignatures = new DataGridView();
            colSignatory = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colDate = new DataGridViewTextBoxColumn();
            lblSignatures = new Label();
            dgvTitleBlock = new DataGridView();
            colField = new DataGridViewTextBoxColumn();
            colValue = new DataGridViewTextBoxColumn();
            tabPageRequirements = new TabPage();
            txtTechnicalRequirements = new TextBox();
            tabPageSettings = new TabPage();
            btnResetSettings = new Button();
            chkCleanImage = new CheckBox();
            lblDpiHint = new Label();
            numDpi = new NumericUpDown();
            lblDpi = new Label();
            tabControl1.SuspendLayout();
            tabPageTitleBlock.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSignatures).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTitleBlock).BeginInit();
            tabPageRequirements.SuspendLayout();
            tabPageSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDpi).BeginInit();
            SuspendLayout();
            // 
            // imageViewer1
            // 
            imageViewer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            imageViewer1.BackColor = Color.FromArgb(64, 64, 64);
            imageViewer1.Location = new Point(12, 12);
            imageViewer1.Name = "imageViewer1";
            imageViewer1.Size = new Size(620, 536);
            imageViewer1.TabIndex = 0;
            imageViewer1.HoveredRegionChanged += imageViewer1_HoveredRegionChanged;
            imageViewer1.RegionSelected += imageViewer1_RegionSelected;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            button1.Location = new Point(644, 519);
            button1.Name = "button1";
            button1.Size = new Size(458, 29);
            button1.TabIndex = 1;
            button1.Text = "Выбрать файл";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPageTitleBlock);
            tabControl1.Controls.Add(tabPageRequirements);
            tabControl1.Controls.Add(tabPageSettings);
            tabControl1.Location = new Point(644, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(458, 500);
            tabControl1.TabIndex = 2;
            // 
            // tabPageTitleBlock
            // 
            tabPageTitleBlock.Controls.Add(dgvSignatures);
            tabPageTitleBlock.Controls.Add(lblSignatures);
            tabPageTitleBlock.Controls.Add(dgvTitleBlock);
            tabPageTitleBlock.Location = new Point(4, 24);
            tabPageTitleBlock.Name = "tabPageTitleBlock";
            tabPageTitleBlock.Padding = new Padding(6);
            tabPageTitleBlock.Size = new Size(450, 472);
            tabPageTitleBlock.TabIndex = 0;
            tabPageTitleBlock.Text = "Основная надпись";
            tabPageTitleBlock.UseVisualStyleBackColor = true;
            // 
            // dgvSignatures
            // 
            dgvSignatures.AllowUserToAddRows = false;
            dgvSignatures.AllowUserToDeleteRows = false;
            dgvSignatures.AllowUserToResizeRows = false;
            dgvSignatures.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSignatures.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSignatures.BackgroundColor = SystemColors.Window;
            dgvSignatures.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSignatures.Columns.AddRange(new DataGridViewColumn[] { colSignatory, colName, colDate });
            dgvSignatures.Location = new Point(6, 251);
            dgvSignatures.MultiSelect = false;
            dgvSignatures.Name = "dgvSignatures";
            dgvSignatures.RowHeadersVisible = false;
            dgvSignatures.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSignatures.Size = new Size(438, 215);
            dgvSignatures.TabIndex = 2;
            dgvSignatures.SelectionChanged += dgvSignatures_SelectionChanged;
            // 
            // colSignatory
            // 
            colSignatory.FillWeight = 40F;
            colSignatory.HeaderText = "Должность";
            colSignatory.Name = "colSignatory";
            colSignatory.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // colName
            // 
            colName.FillWeight = 40F;
            colName.HeaderText = "ФИО";
            colName.Name = "colName";
            colName.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // colDate
            // 
            colDate.FillWeight = 20F;
            colDate.HeaderText = "Дата";
            colDate.Name = "colDate";
            colDate.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // lblSignatures
            // 
            lblSignatures.AutoSize = true;
            lblSignatures.Location = new Point(6, 232);
            lblSignatures.Name = "lblSignatures";
            lblSignatures.Size = new Size(59, 15);
            lblSignatures.TabIndex = 1;
            lblSignatures.Text = "Подписи:";
            // 
            // dgvTitleBlock
            // 
            dgvTitleBlock.AllowUserToAddRows = false;
            dgvTitleBlock.AllowUserToDeleteRows = false;
            dgvTitleBlock.AllowUserToResizeRows = false;
            dgvTitleBlock.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvTitleBlock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTitleBlock.BackgroundColor = SystemColors.Window;
            dgvTitleBlock.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTitleBlock.Columns.AddRange(new DataGridViewColumn[] { colField, colValue });
            dgvTitleBlock.Location = new Point(6, 6);
            dgvTitleBlock.MultiSelect = false;
            dgvTitleBlock.Name = "dgvTitleBlock";
            dgvTitleBlock.RowHeadersVisible = false;
            dgvTitleBlock.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTitleBlock.Size = new Size(438, 220);
            dgvTitleBlock.TabIndex = 0;
            dgvTitleBlock.SelectionChanged += dgvTitleBlock_SelectionChanged;
            // 
            // colField
            // 
            colField.FillWeight = 45F;
            colField.HeaderText = "Поле";
            colField.Name = "colField";
            colField.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // colValue
            // 
            colValue.FillWeight = 55F;
            colValue.HeaderText = "Значение";
            colValue.Name = "colValue";
            colValue.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // tabPageRequirements
            // 
            tabPageRequirements.Controls.Add(txtTechnicalRequirements);
            tabPageRequirements.Location = new Point(4, 24);
            tabPageRequirements.Name = "tabPageRequirements";
            tabPageRequirements.Padding = new Padding(6);
            tabPageRequirements.Size = new Size(450, 472);
            tabPageRequirements.TabIndex = 1;
            tabPageRequirements.Text = "Технические требования";
            tabPageRequirements.UseVisualStyleBackColor = true;
            // 
            // txtTechnicalRequirements
            // 
            txtTechnicalRequirements.BackColor = SystemColors.Window;
            txtTechnicalRequirements.Dock = DockStyle.Fill;
            txtTechnicalRequirements.Location = new Point(6, 6);
            txtTechnicalRequirements.Multiline = true;
            txtTechnicalRequirements.Name = "txtTechnicalRequirements";
            txtTechnicalRequirements.ScrollBars = ScrollBars.Vertical;
            txtTechnicalRequirements.Size = new Size(438, 460);
            txtTechnicalRequirements.TabIndex = 0;
            // 
            // tabPageSettings
            // 
            tabPageSettings.Controls.Add(btnResetSettings);
            tabPageSettings.Controls.Add(chkCleanImage);
            tabPageSettings.Controls.Add(lblDpiHint);
            tabPageSettings.Controls.Add(numDpi);
            tabPageSettings.Controls.Add(lblDpi);
            tabPageSettings.Location = new Point(4, 24);
            tabPageSettings.Name = "tabPageSettings";
            tabPageSettings.Padding = new Padding(6);
            tabPageSettings.Size = new Size(450, 472);
            tabPageSettings.TabIndex = 2;
            tabPageSettings.Text = "Настройки";
            tabPageSettings.UseVisualStyleBackColor = true;
            // 
            // lblDpi
            // 
            lblDpi.AutoSize = true;
            lblDpi.Location = new Point(12, 18);
            lblDpi.Name = "lblDpi";
            lblDpi.Size = new Size(94, 15);
            lblDpi.TabIndex = 0;
            lblDpi.Text = "DPI растеризации:";
            // 
            // numDpi
            // 
            numDpi.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            numDpi.Location = new Point(12, 40);
            numDpi.Maximum = new decimal(new int[] { 900, 0, 0, 0 });
            numDpi.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numDpi.Name = "numDpi";
            numDpi.Size = new Size(120, 23);
            numDpi.TabIndex = 1;
            numDpi.Value = new decimal(new int[] { 200, 0, 0, 0 });
            // 
            // lblDpiHint
            // 
            lblDpiHint.Location = new Point(140, 40);
            lblDpiHint.Name = "lblDpiHint";
            lblDpiHint.Size = new Size(294, 48);
            lblDpiHint.TabIndex = 2;
            lblDpiHint.Text = "Обычно 200–900. Выше — точнее распознавание, но дольше и больше памяти.";
            // 
            // chkCleanImage
            // 
            chkCleanImage.AutoSize = true;
            chkCleanImage.Checked = true;
            chkCleanImage.CheckState = CheckState.Checked;
            chkCleanImage.Location = new Point(12, 100);
            chkCleanImage.Name = "chkCleanImage";
            chkCleanImage.Size = new Size(344, 19);
            chkCleanImage.TabIndex = 3;
            chkCleanImage.Text = "Чистое изображение (без отладочных рамок детекций)";
            chkCleanImage.UseVisualStyleBackColor = true;
            // 
            // btnResetSettings
            // 
            btnResetSettings.Location = new Point(12, 134);
            btnResetSettings.Name = "btnResetSettings";
            btnResetSettings.Size = new Size(160, 29);
            btnResetSettings.TabIndex = 4;
            btnResetSettings.Text = "Сбросить настройки";
            btnResetSettings.UseVisualStyleBackColor = true;
            btnResetSettings.Click += btnResetSettings_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1114, 560);
            Controls.Add(tabControl1);
            Controls.Add(button1);
            Controls.Add(imageViewer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Document Recognition";
            tabControl1.ResumeLayout(false);
            tabPageTitleBlock.ResumeLayout(false);
            tabPageTitleBlock.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSignatures).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTitleBlock).EndInit();
            tabPageRequirements.ResumeLayout(false);
            tabPageRequirements.PerformLayout();
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numDpi).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private ImageViewerControl imageViewer1;
        private Button button1;
        private TabControl tabControl1;
        private TabPage tabPageTitleBlock;
        private DataGridView dgvTitleBlock;
        private DataGridViewTextBoxColumn colField;
        private DataGridViewTextBoxColumn colValue;
        private Label lblSignatures;
        private DataGridView dgvSignatures;
        private DataGridViewTextBoxColumn colSignatory;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colDate;
        private TabPage tabPageRequirements;
        private TextBox txtTechnicalRequirements;
        private TabPage tabPageSettings;
        private Label lblDpi;
        private NumericUpDown numDpi;
        private Label lblDpiHint;
        private CheckBox chkCleanImage;
        private Button btnResetSettings;
    }
}
