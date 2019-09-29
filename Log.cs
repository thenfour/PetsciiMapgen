using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace PetsciiMapgen
{
  public static class Log
  {
    static LogCore _log = new LogCore();
    public static void WriteLine(string msg)
    {
      _log.WriteLine(msg);
    }
    public static void WriteLine(string fmt, params object[] args)
    {
      _log.WriteLine(fmt, args);
    }
    public static void SetLogFile(string path)
    {
      _log.SetLogFile(path);
    }
    public static void EnterTask(string s, params object[] o)
    {
      _log.EnterTask(s, o);
    }
    public static void EnterTask(string s)
    {
      _log.EnterTask(s);
    }
    public static void EndTask()
    {
      _log.EndTask();
    }
  }

  public class LogCore
  {
    int indentLevel = 0;
    readonly object fileLock = new object();
    string logFilePath = null;
    List<string> lines = new List<string>();

    public struct Task
    {
      public Stopwatch sw;
      public string name;
    }
    Stack<Task> tasks = new Stack<Task>();
    public void EnterTask(string s, params object[] o)
    {
      EnterTask(string.Format(s, o));
    }
    public void EnterTask(string s)
    {
      WriteLine("==> Enter task {0}", s);
      IncreaseIndent();
      Task n;
      if (!tasks.Any())
      {
        n = new Task
        {
          name = "root",
          sw = new Stopwatch()
        };
        n.sw.Start();
        tasks.Push(n);
      }
      n = new Task
      {
        name = s,
        sw = new Stopwatch()
      };
      n.sw.Start();
      tasks.Push(n);
    }
    public void EndTask()
    {
      Debug.Assert(this.tasks.Count > 0);
      Task n = this.tasks.Pop();
      TimeSpan ts = n.sw.Elapsed;
      DecreaseIndent();
      WriteLine("<== {1} (end {0})", n.name, ts);
    }

    public void IncreaseIndent()
    {
      Interlocked.Increment(ref indentLevel);
    }
    public void DecreaseIndent()
    {
      Interlocked.Decrement(ref indentLevel);
    }

    public void WriteLine(string msg)
    {
      int indent = indentLevel;

      msg = string.Format("{0} {2}{1}", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss'Z'"), msg, new string(' ', indent * 2));
      Console.WriteLine(msg);
      Debug.WriteLine(msg);
      if (lines != null)
      {
        lines.Add(msg);// this is useful so when we initialize some file-based log later, we can drop in existing messages
      }
      if (logFilePath != null)
      {
        lock(fileLock)
        {
          using (var sw = System.IO.File.AppendText(logFilePath))
          {
            sw.WriteLine(msg);
          }
        }
      }
    }
    public void WriteLine(string fmt, params object[] args)
    {
      WriteLine(string.Format(fmt, args));
    }

    public void SetLogFile(string path)
    {
      if (path == logFilePath)
        return;
      List<string> l = Interlocked.Exchange<List<string>>(ref lines, new List<string>());

      using (var sw = System.IO.File.AppendText(path))
      {
        foreach (var line in l)
        {
          sw.WriteLine(line);
        }
      }
      // there is a condition where log lines would be lost but don't care atm.

      logFilePath = path;

      WriteLine("Log path set to {0}", path);
    }
  }
}

