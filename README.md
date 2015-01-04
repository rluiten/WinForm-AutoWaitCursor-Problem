WinForm AutoWaitCursor Debugger Problem

This is a minimal Windows Forms project that I added AutoWaitCursor to.
Please note AutoWaitCursor is no longer available at the original link I got it from which was http://www.vbusers.com/codecsharp/codeget.asp?ThreadID=58&PostID=1&NumReplies=0

If I launch this Windows Forms Project inside Visual Studio with the Debugger and platform at "Any CPU" or "x64" on my 64 bit Windows 7 PC I get a "vshost.exe has stopped working" dialog appear and the application has crashed.

Launching with Debugger and with platform set to "x86" is not a problem.
Launching without the Debugger is not a problem for any platform.
Launching resultant compiled binary outside Visual Studio has not yet exhibited this problem.

It appears to be a coupling of AutoWaitCursor and Visual Studio 2013 Debugger.
I believe currently that VS2012 whcih I have used in the past did not exhibit this behaviour but I cannot verify that at the moment.
While this problem is not a show stopper, I would like to understand or fix the issue but have not made any progress in that direction yet.

Another reason I created this was to have a copy of AutoWaitCursor source around for people to find if they search for it.
