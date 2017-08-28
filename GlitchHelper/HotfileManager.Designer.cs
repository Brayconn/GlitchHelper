namespace GlitchHelper
{
    partial class HotfileManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HotfileManager));
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setOutputFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoExportModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.overwriteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.iterateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteEmptyHotfilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ignoreHotfileRenamingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ignoreHotfileDeletionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.AllowDrop = true;
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.Location = new System.Drawing.Point(13, 27);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(259, 222);
            this.treeView1.TabIndex = 0;
            this.treeView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeView1_KeyDown);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(284, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setOutputFileToolStripMenuItem,
            this.autoExportToolStripMenuItem,
            this.autoExportModeToolStripMenuItem,
            this.toolStripSeparator1,
            this.deleteEmptyHotfilesToolStripMenuItem,
            this.ignoreHotfileRenamingToolStripMenuItem,
            this.ignoreHotfileDeletionsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // setOutputFileToolStripMenuItem
            // 
            this.setOutputFileToolStripMenuItem.Enabled = false;
            this.setOutputFileToolStripMenuItem.Name = "setOutputFileToolStripMenuItem";
            this.setOutputFileToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.setOutputFileToolStripMenuItem.Text = "Set Auto Export File...";
            this.setOutputFileToolStripMenuItem.Click += new System.EventHandler(this.setOutputFileToolStripMenuItem_Click);
            // 
            // autoExportToolStripMenuItem
            // 
            this.autoExportToolStripMenuItem.Checked = true;
            this.autoExportToolStripMenuItem.CheckOnClick = true;
            this.autoExportToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoExportToolStripMenuItem.Enabled = false;
            this.autoExportToolStripMenuItem.Name = "autoExportToolStripMenuItem";
            this.autoExportToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.autoExportToolStripMenuItem.Text = "Auto Export";
            this.autoExportToolStripMenuItem.CheckedChanged += new System.EventHandler(this.autoExportToolStripMenuItem_CheckedChanged);
            // 
            // autoExportModeToolStripMenuItem
            // 
            this.autoExportModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.overwriteToolStripMenuItem,
            this.iterateToolStripMenuItem});
            this.autoExportModeToolStripMenuItem.Enabled = false;
            this.autoExportModeToolStripMenuItem.Name = "autoExportModeToolStripMenuItem";
            this.autoExportModeToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.autoExportModeToolStripMenuItem.Text = "Auto Export Mode";
            // 
            // overwriteToolStripMenuItem
            // 
            this.overwriteToolStripMenuItem.Checked = true;
            this.overwriteToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.overwriteToolStripMenuItem.Enabled = false;
            this.overwriteToolStripMenuItem.Name = "overwriteToolStripMenuItem";
            this.overwriteToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.overwriteToolStripMenuItem.Text = "Overwrite";
            this.overwriteToolStripMenuItem.Click += new System.EventHandler(this.overwriteToolStripMenuItem_Click);
            // 
            // iterateToolStripMenuItem
            // 
            this.iterateToolStripMenuItem.Enabled = false;
            this.iterateToolStripMenuItem.Name = "iterateToolStripMenuItem";
            this.iterateToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.iterateToolStripMenuItem.Text = "Iterate";
            this.iterateToolStripMenuItem.Click += new System.EventHandler(this.iterateToolStripMenuItem_Click);
            // 
            // deleteEmptyHotfilesToolStripMenuItem
            // 
            this.deleteEmptyHotfilesToolStripMenuItem.Checked = true;
            this.deleteEmptyHotfilesToolStripMenuItem.CheckOnClick = true;
            this.deleteEmptyHotfilesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.deleteEmptyHotfilesToolStripMenuItem.Name = "deleteEmptyHotfilesToolStripMenuItem";
            this.deleteEmptyHotfilesToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.deleteEmptyHotfilesToolStripMenuItem.Text = "Delete Orphaned Hotfiles";
            this.deleteEmptyHotfilesToolStripMenuItem.Click += new System.EventHandler(this.deleteOrphanedEmptyHotfilesToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // ignoreHotfileRenamingToolStripMenuItem
            // 
            this.ignoreHotfileRenamingToolStripMenuItem.CheckOnClick = true;
            this.ignoreHotfileRenamingToolStripMenuItem.Name = "ignoreHotfileRenamingToolStripMenuItem";
            this.ignoreHotfileRenamingToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.ignoreHotfileRenamingToolStripMenuItem.Text = "Ignore Hotfile Renaming";
            this.ignoreHotfileRenamingToolStripMenuItem.Click += new System.EventHandler(this.ignoreHotfileRenamingToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(204, 6);
            // 
            // ignoreHotfileDeletionsToolStripMenuItem
            // 
            this.ignoreHotfileDeletionsToolStripMenuItem.CheckOnClick = true;
            this.ignoreHotfileDeletionsToolStripMenuItem.Name = "ignoreHotfileDeletionsToolStripMenuItem";
            this.ignoreHotfileDeletionsToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.ignoreHotfileDeletionsToolStripMenuItem.Text = "Ignore Hotfile Deletions";
            this.ignoreHotfileDeletionsToolStripMenuItem.Click += new System.EventHandler(this.ignoreHotfileDeletionsToolStripMenuItem_Click);
            // 
            // HotfileManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "HotfileManager";
            this.Text = "Hotfile Manager";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setOutputFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        public System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ToolStripMenuItem autoExportToolStripMenuItem;
        public System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem autoExportModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem overwriteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem iterateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteEmptyHotfilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem ignoreHotfileRenamingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ignoreHotfileDeletionsToolStripMenuItem;
    }
}