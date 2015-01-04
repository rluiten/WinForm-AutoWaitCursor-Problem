using System.Windows.Forms;

namespace WinForm_AutoWaitCursor_Problem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //AutoWaitCursor.Cursor = Cursors.WaitCursor;
            //AutoWaitCursor.Delay = new TimeSpan(0, 0, 0, 0, 25);
            AutoWaitCursor.MainWindowHandle = Handle;
            AutoWaitCursor.Start();
        }
    }
}
