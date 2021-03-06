﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// This found at http://www.vbusers.com/codecsharp/codeget.asp?ThreadID=58&PostID=1&NumReplies=0
// link no longer works.

namespace WinForm_AutoWaitCursor_Problem
{
    /// <summary>
    /// This static utility class can be used to automatically show a wait cursor when the application 
    /// is busy (ie not responding to user input). The class automatically monitors the application
    /// state, removing the need for manually changing the cursor.
    /// </summary>
    /// <example>
    /// To use, simply insert the following line in your Application startup code
    /// 
    ///		private void Form1_Load(object sender, System.EventArgs e)
    ///		{
    ///			AutoWaitCursor.Cursor = Cursors.WaitCursor;
    ///			AutoWaitCursor.Delay = new TimeSpan(0, 0, 0, 0, 25);
    ///			// Set the window handle to the handle of the main form in your application 
    ///			AutoWaitCursor.MainWindowHandle = this.Handle;
    ///			AutoWaitCursor.Start();
    ///		}
    ///	
    /// This installs changes to cursor after 100ms of blocking work (ie. work carried out on the main application thread).
    /// 
    /// Note, the above code GLOBALLY replaces the following:
    /// 
    /// public void DoWork()
    /// {
    ///		try
    ///		{
    ///			Screen.Cursor = Cursors.Wait;
    ///			GetResultsFromDatabase();
    ///		}
    ///		finally
    ///		{
    ///			Screen.Cursor = Cursors.Default;
    ///		}
    /// }
    /// </example>
    [DebuggerStepThrough]
    public class AutoWaitCursor
    {
        #region Member Variables

        private static readonly TimeSpan DefaultDelay = new TimeSpan(0, 0, 0, 0, 25);
        /// <summary>
        /// The application state monitor class (which monitors the application busy status).
        /// </summary>
        private static ApplicationStateMonitor _appStateMonitor = new ApplicationStateMonitor(Cursors.WaitCursor, DefaultDelay);

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor.
        /// </summary>
        private AutoWaitCursor()
        {
            // Intentionally blank
        }

        #endregion

        #region Public Static Properties

        /// <summary>
        /// Returns the amount of time the application has been idle.
        /// </summary>
        public TimeSpan ApplicationIdleTime
        {
            get { return _appStateMonitor.ApplicationIdleTime; }
        }

        /// <summary>
        /// Returns true if the auto wait cursor has been started.
        /// </summary>
        public static bool IsStarted
        {
            get { return _appStateMonitor.IsStarted; }
        }

        /// <summary>
        /// Gets or sets the Cursor to use during Application busy periods.
        /// </summary>
        public static Cursor Cursor
        {
            get { return _appStateMonitor.Cursor; }
            set
            {
                _appStateMonitor.Cursor = value;
            }
        }

        /// <summary>
        /// Enables or disables the auto wait cursor.
        /// </summary>
        public static bool Enabled
        {
            get { return _appStateMonitor.Enabled; }
            set
            {
                _appStateMonitor.Enabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the period of Time to wait before showing the WaitCursor whilst Application is working
        /// </summary>
        public static TimeSpan Delay
        {
            get { return _appStateMonitor.Delay; }
            set { _appStateMonitor.Delay = value; }
        }

        /// <summary>
        /// Gets or sets the main window handle of the application (ie the handle of an MDI form).
        /// This is the window handle monitored to detect when the application becomes busy.
        /// </summary>
        public static IntPtr MainWindowHandle
        {
            get { return _appStateMonitor.MainWindowHandle; }
            set { _appStateMonitor.MainWindowHandle = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the auto wait cursor monitoring the application.
        /// </summary>
        public static void Start()
        {
            _appStateMonitor.Start();
        }

        /// <summary>
        /// Stops the auto wait cursor monitoring the application.
        /// </summary>
        public static void Stop()
        {
            _appStateMonitor.Stop();
        }

        #endregion

        #region Private Class ApplicationStateMonitor

        /// <summary>
        /// Private class that monitors the state of the application and automatically
        /// changes the cursor accordingly.
        /// </summary>
        private class ApplicationStateMonitor : IDisposable
        {
            #region Member Variables

            /// <summary>
            /// The time the application became inactive.
            /// </summary>
            private DateTime _inactiveStart = DateTime.Now;
            /// <summary>
            /// If the monitor has been started.
            /// </summary>
            private bool _isStarted;// = false;
            /// <summary>
            /// Delay to wait before calling back
            /// </summary>
            private TimeSpan _delay;
            /// <summary>
            /// The windows handle to the main process window.
            /// </summary>
            private IntPtr _mainWindowHandle = IntPtr.Zero;
            /// <summary>
            /// Thread to perform the wait and callback
            /// </summary>
            private Thread _callbackThread;// = null;
            /// <summary>
            /// Stores if the class has been disposed of.
            /// </summary>
            private bool _isDisposed;// = false;
            /// <summary>
            /// Stores if the class is enabled or not.
            /// </summary>
            private bool _enabled = true;
            /// <summary>
            /// GUI Thread Id .
            /// </summary>
            private uint _mainThreadId;
            /// <summary>
            /// Callback Thread Id.
            /// </summary>
            private uint _callbackThreadId;
            /// <summary>
            /// Stores the old cursor.
            /// </summary>
            private Cursor _oldCursor;
            /// <summary>
            /// Stores the new cursor.
            /// </summary>
            private Cursor _waitCursor;

            #endregion

            #region PInvokes

            [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SendMessageTimeout(IntPtr hWnd, int msg, int wParam, string lParam, int fuFlags, int uTimeout, out int lpdwResult);

            [DllImport("USER32.DLL")]
            private static extern uint AttachThreadInput(uint attachTo, uint attachFrom, bool attach);

            [DllImport("KERNEL32.DLL")]
            private static extern uint GetCurrentThreadId();

            //private const int SMTO_NORMAL = 0x0000;
            private const int SMTO_BLOCK = 0x0001;
            //private const int SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

            #endregion

            #region Constructors

            /// <summary>
            /// Default member initialising Constructor
            /// </summary>
            /// <param name="waitCursor">The wait cursor to use.</param>
            /// <param name="delay">The delay before setting the cursor to the wait cursor.</param>
            public ApplicationStateMonitor(Cursor waitCursor, TimeSpan delay)
            {
                // Constructor is called from (what is treated as) the main thread
                _mainThreadId = GetCurrentThreadId();
                _delay = delay;
                _waitCursor = waitCursor;
                // Gracefully shuts down the state monitor
                Application.ThreadExit += _OnApplicationThreadExit;
            }

            #endregion

            #region IDisposable

            /// <summary>
            /// On Disposal terminates the Thread, calls Finish (on thread) if Start has been called
            /// </summary>
            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }
                // Kills the Thread loop
                _isDisposed = true;
            }

            #endregion IDisposable

            #region Public Methods

            /// <summary>
            /// Starts the application state monitor.
            /// </summary>
            public void Start()
            {
                if (!_isStarted)
                {
                    _isStarted = true;
                    CreateMonitorThread();
                }
            }

            /// <summary>
            /// Stops the application state monitor.
            /// </summary>
            public void Stop()
            {
                if (_isStarted)
                {
                    _isStarted = false;
                }
            }

            /// <summary>
            /// Set the Cursor to wait.
            /// </summary>
            public void SetWaitCursor()
            {
                // Start is called in a new Thread, grab the new Thread Id so we can attach to Main thread's input
                _callbackThreadId = GetCurrentThreadId();

                // Have to call this before calling Cursor.Current
                AttachThreadInput(_callbackThreadId, _mainThreadId, true);

                _oldCursor = Cursor.Current;
                Cursor.Current = _waitCursor;
            }

            /// <summary>
            /// Finish showing the Cursor (switch back to previous Cursor)
            /// </summary>
            public void RestoreCursor()
            {
                // Restore the cursor
                Cursor.Current = _oldCursor;
                // Detach from Main thread input
                AttachThreadInput(_callbackThreadId, _mainThreadId, false);
            }

            /// <summary>
            /// Enable/Disable the call to Start (note, once Start is called it *always* calls the paired Finish)
            /// </summary>
            public bool Enabled
            {
                get { return _enabled; }
                set { _enabled = value; }
            }

            /// <summary>
            /// Gets or sets the period of Time to wait before calling the Start method
            /// </summary>
            public TimeSpan Delay
            {
                get { return _delay; }
                set { _delay = value; }
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// Returns true if the auto wait cursor has been started.
            /// </summary>
            public bool IsStarted
            {
                get { return _isStarted; }
            }

            /// <summary>
            /// Gets or sets the main window handle of the application (ie the handle of an MDI form).
            /// This is the window handle monitored to detect when the application becomes busy.
            /// </summary>
            public IntPtr MainWindowHandle
            {
                get { return _mainWindowHandle; }
                set { _mainWindowHandle = value; }
            }

            /// <summary>
            /// Gets or sets the Cursor to show
            /// </summary>
            public Cursor Cursor
            {
                get { return _waitCursor; }
                set { _waitCursor = value; }
            }

            /// <summary>
            /// Returns the amount of time the application has been idle.
            /// </summary>
            public TimeSpan ApplicationIdleTime
            {
                get { return DateTime.Now.Subtract(_inactiveStart); }
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Prepares the class creating a Thread that monitors the main application state.
            /// </summary>
            private void CreateMonitorThread()
            {
                // Create the monitor thread
                _callbackThread = new Thread(ThreadCallbackLoop) {Name = "AutoWaitCursorCallback", IsBackground = true};
                // Start the thread
                _callbackThread.Start();
            }

            /// <summary>
            /// Thread callback method. 
            /// Loops calling SetWaitCursor and RestoreCursor until Disposed.
            /// </summary>
            private void ThreadCallbackLoop()
            {
                try
                {
                    do
                    {
                        if (!_enabled || _mainWindowHandle == IntPtr.Zero)
                        {
                            // Just sleep
                            Thread.Sleep(_delay);
                        }
                        else
                        {
                            // Wait for start
                            if (_IsApplicationBusy(_delay, _mainWindowHandle))
                            {
                                try
                                {
                                    SetWaitCursor();
                                    WaitForIdle();
                                }
                                finally
                                {
                                    // Always calls Finish (even if we are Disabled)
                                    RestoreCursor();
                                    // Store the time the application became inactive
                                    _inactiveStart = DateTime.Now;
                                }
                            }
                            else
                            {
                                // Wait before checking again
                                Thread.Sleep(25);
                            }
                        }
                    } while (!_isDisposed && _isStarted);
                }
                catch (ThreadAbortException)
                {
                    // The thread is being aborted, just reset the abort and exit gracefully
                    Thread.ResetAbort();
                }
            }

            /// <summary>
            /// Blocks until the application responds to a test message.
            /// If the application doesn't respond with the timespan, will return false,
            /// else returns true.
            /// </summary>
            private bool _IsApplicationBusy(TimeSpan delay, IntPtr windowHandle)
            {
                const int INFINITE = Int32.MaxValue;
                const int WM_NULL = 0;
                int result;// = 0;
                //bool success;

                // See if the application is responding
                if (delay == TimeSpan.MaxValue)
                {
                    /*success = */SendMessageTimeout(windowHandle, WM_NULL, 0, null,
                        SMTO_BLOCK, INFINITE, out result);
                }
                else
                {
                    /*success = */SendMessageTimeout(windowHandle, WM_NULL, 0, null,
                        SMTO_BLOCK, Convert.ToInt32(delay.TotalMilliseconds), out result);
                }

                if (result != 0)
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Waits for the ResetEvent (set by Dispose and Reset), 
            /// since Start has been called we *have* to call RestoreCursor once the thread is idle again.
            /// </summary>
            private void WaitForIdle()
            {
                // Wait indefinately until the application is idle
                _IsApplicationBusy(TimeSpan.MaxValue, _mainWindowHandle);
            }

            /// <summary>
            /// The application is closing, shut the state monitor down.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _OnApplicationThreadExit(object sender, EventArgs e)
            {
                Dispose();
            }

            #endregion
        }

        #endregion
    }
}
