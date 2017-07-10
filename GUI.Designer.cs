namespace TeaseAIScriptChecker
{
    partial class GUI
    {
        /// <summary>
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.selectFolderButton = new System.Windows.Forms.Button();
            this.startScanButton = new System.Windows.Forms.Button();
            this.scanProgressBar = new System.Windows.Forms.ProgressBar();
            this.infoTextBox = new System.Windows.Forms.TextBox();
            this.debugLogCheckBox = new System.Windows.Forms.CheckBox();
            this.logCheckBox = new System.Windows.Forms.CheckBox();
            this.logTextbox = new System.Windows.Forms.TextBox();
            this.consoleLogCheckBox = new System.Windows.Forms.CheckBox();
            this.teaseAIFolderTextBox = new System.Windows.Forms.TextBox();
            this.teaseAIFolderButton = new System.Windows.Forms.Button();
            this.teaseAIBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.HelpRequest += new System.EventHandler(this.folderBrowserDialog1_HelpRequest);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(18, 492);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(606, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // selectFolderButton
            // 
            this.selectFolderButton.Location = new System.Drawing.Point(630, 492);
            this.selectFolderButton.Name = "selectFolderButton";
            this.selectFolderButton.Size = new System.Drawing.Size(169, 23);
            this.selectFolderButton.TabIndex = 1;
            this.selectFolderButton.Text = "Select Personality Folder to scan";
            this.selectFolderButton.UseVisualStyleBackColor = true;
            this.selectFolderButton.Click += new System.EventHandler(this.selectFolderButton_Click);
            // 
            // startScanButton
            // 
            this.startScanButton.Location = new System.Drawing.Point(630, 518);
            this.startScanButton.Name = "startScanButton";
            this.startScanButton.Size = new System.Drawing.Size(169, 23);
            this.startScanButton.TabIndex = 2;
            this.startScanButton.Text = "Start Scan";
            this.startScanButton.UseVisualStyleBackColor = true;
            this.startScanButton.Click += new System.EventHandler(this.startScanButton_Click);
            // 
            // scanProgressBar
            // 
            this.scanProgressBar.Location = new System.Drawing.Point(18, 518);
            this.scanProgressBar.Name = "scanProgressBar";
            this.scanProgressBar.Size = new System.Drawing.Size(606, 23);
            this.scanProgressBar.TabIndex = 3;
            // 
            // infoTextBox
            // 
            this.infoTextBox.Location = new System.Drawing.Point(18, 547);
            this.infoTextBox.Name = "infoTextBox";
            this.infoTextBox.ReadOnly = true;
            this.infoTextBox.Size = new System.Drawing.Size(606, 20);
            this.infoTextBox.TabIndex = 4;
            // 
            // debugLogCheckBox
            // 
            this.debugLogCheckBox.AutoSize = true;
            this.debugLogCheckBox.Location = new System.Drawing.Point(712, 440);
            this.debugLogCheckBox.Name = "debugLogCheckBox";
            this.debugLogCheckBox.Size = new System.Drawing.Size(87, 17);
            this.debugLogCheckBox.TabIndex = 5;
            this.debugLogCheckBox.Text = "Debug mode";
            this.debugLogCheckBox.UseVisualStyleBackColor = true;
            this.debugLogCheckBox.CheckedChanged += new System.EventHandler(this.debugLogCheckBox_CheckedChanged);
            // 
            // logCheckBox
            // 
            this.logCheckBox.AutoSize = true;
            this.logCheckBox.Checked = true;
            this.logCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.logCheckBox.Location = new System.Drawing.Point(647, 440);
            this.logCheckBox.Name = "logCheckBox";
            this.logCheckBox.Size = new System.Drawing.Size(59, 17);
            this.logCheckBox.TabIndex = 6;
            this.logCheckBox.Text = "File log";
            this.logCheckBox.UseVisualStyleBackColor = true;
            // 
            // logTextbox
            // 
            this.logTextbox.Location = new System.Drawing.Point(18, 12);
            this.logTextbox.Multiline = true;
            this.logTextbox.Name = "logTextbox";
            this.logTextbox.ReadOnly = true;
            this.logTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.logTextbox.Size = new System.Drawing.Size(781, 422);
            this.logTextbox.TabIndex = 7;
            // 
            // consoleLogCheckBox
            // 
            this.consoleLogCheckBox.AutoSize = true;
            this.consoleLogCheckBox.Location = new System.Drawing.Point(521, 440);
            this.consoleLogCheckBox.Name = "consoleLogCheckBox";
            this.consoleLogCheckBox.Size = new System.Drawing.Size(120, 17);
            this.consoleLogCheckBox.TabIndex = 8;
            this.consoleLogCheckBox.Text = "Console log (slower)";
            this.consoleLogCheckBox.UseVisualStyleBackColor = true;
            // 
            // teaseAIFolderTextBox
            // 
            this.teaseAIFolderTextBox.Location = new System.Drawing.Point(18, 466);
            this.teaseAIFolderTextBox.Name = "teaseAIFolderTextBox";
            this.teaseAIFolderTextBox.Size = new System.Drawing.Size(606, 20);
            this.teaseAIFolderTextBox.TabIndex = 9;
            this.teaseAIFolderTextBox.TextChanged += new System.EventHandler(this.teaseAIFolderTextBox_TextChanged);
            // 
            // teaseAIFolderButton
            // 
            this.teaseAIFolderButton.Location = new System.Drawing.Point(631, 466);
            this.teaseAIFolderButton.Name = "teaseAIFolderButton";
            this.teaseAIFolderButton.Size = new System.Drawing.Size(168, 23);
            this.teaseAIFolderButton.TabIndex = 10;
            this.teaseAIFolderButton.Text = "Select Tease AI folder";
            this.teaseAIFolderButton.UseVisualStyleBackColor = true;
            this.teaseAIFolderButton.Click += new System.EventHandler(this.teaseAIFolderButton_Click);
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(811, 579);
            this.Controls.Add(this.teaseAIFolderButton);
            this.Controls.Add(this.teaseAIFolderTextBox);
            this.Controls.Add(this.consoleLogCheckBox);
            this.Controls.Add(this.logTextbox);
            this.Controls.Add(this.logCheckBox);
            this.Controls.Add(this.debugLogCheckBox);
            this.Controls.Add(this.infoTextBox);
            this.Controls.Add(this.scanProgressBar);
            this.Controls.Add(this.startScanButton);
            this.Controls.Add(this.selectFolderButton);
            this.Controls.Add(this.textBox1);
            this.Name = "GUI";
            this.Text = "TeaseAIScriptChecker (1.2) by GodDragon";
            this.Load += new System.EventHandler(this.GUI_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button selectFolderButton;
        private System.Windows.Forms.Button startScanButton;
        private System.Windows.Forms.ProgressBar scanProgressBar;
        private System.Windows.Forms.TextBox infoTextBox;
        private System.Windows.Forms.CheckBox debugLogCheckBox;
        private System.Windows.Forms.CheckBox logCheckBox;
        private System.Windows.Forms.TextBox logTextbox;
        private System.Windows.Forms.CheckBox consoleLogCheckBox;
        private System.Windows.Forms.TextBox teaseAIFolderTextBox;
        private System.Windows.Forms.Button teaseAIFolderButton;
        private System.Windows.Forms.FolderBrowserDialog teaseAIBrowserDialog;
    }
}

