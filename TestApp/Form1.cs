using System;
using System.Drawing;
using System.Windows.Forms;
using wyDay.Controls;

namespace TestApp
{
    public partial class Form1 : Form
    {
        public Form1(string[] args)
        {
            Font = SystemFonts.MessageBoxFont;

            InitializeComponent();

            lblVersion.Font = new Font(Font, FontStyle.Bold);

            if (!automaticUpdater.ClosingForInstall)
            {
                LoadStuff();
            }

            // print out the passed arguments
            foreach (var s in args)
            {
                textBox1.Text += s + "\r\n";
            }
        }

        void LoadStuff()
        {

        }

        private void automaticUpdater_ClosingAborted(object sender, EventArgs e)
        {
            // your app was preparing to close
            // however the update wasn't ready so your app is going to show itself
            LoadStuff();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            automaticUpdater.ForceCheckForUpdate(true);
        }
    }
}
