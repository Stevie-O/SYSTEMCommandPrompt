namespace SYSTEMCommandPrompt
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btnOpenCommandPromptSYSTEM = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(558, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "It\'s pretty simple. Click the button, get a command prompt running as the SYSTEM " +
    "user.";
            // 
            // btnOpenCommandPromptSYSTEM
            // 
            this.btnOpenCommandPromptSYSTEM.Location = new System.Drawing.Point(12, 45);
            this.btnOpenCommandPromptSYSTEM.Name = "btnOpenCommandPromptSYSTEM";
            this.btnOpenCommandPromptSYSTEM.Size = new System.Drawing.Size(181, 23);
            this.btnOpenCommandPromptSYSTEM.TabIndex = 1;
            this.btnOpenCommandPromptSYSTEM.Text = "Open Command Prompt";
            this.btnOpenCommandPromptSYSTEM.UseVisualStyleBackColor = true;
            this.btnOpenCommandPromptSYSTEM.Click += new System.EventHandler(this.btnOpenCommandPromptSYSTEM_Click);
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Location = new System.Drawing.Point(12, 74);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtOutput.Size = new System.Drawing.Size(611, 167);
            this.txtOutput.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(635, 253);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.btnOpenCommandPromptSYSTEM);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Command Prompt as SYSTEM User";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOpenCommandPromptSYSTEM;
        private System.Windows.Forms.TextBox txtOutput;
    }
}

