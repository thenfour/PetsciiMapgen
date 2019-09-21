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
  public class Log
  {
    static int indentLevel = 0;
    static readonly object fileLock = new object();
    static string logFilePath = null;
    static List<string> lines = new List<string>();

    public static void IncreaseIndent()
    {
      Interlocked.Increment(ref indentLevel);
    }
    public static void DecreaseIndent()
    {
      Interlocked.Decrement(ref indentLevel);
    }

    public static void WriteLine(string msg)
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
    public static void WriteLine(string fmt, params object[] args)
    {
      WriteLine(string.Format(fmt, args));
    }

    public static void SetLogFile(string path)
    {
      List<string> l = Interlocked.Exchange<List<string>>(ref lines, null);

      using (var sw = System.IO.File.CreateText(path))
      {
        foreach (var line in l)
        {
          sw.WriteLine(line);
        }
      }
      // there is a condition where log lines would be lost but don't care atm.

      logFilePath = path;

      Log.WriteLine("Log path set to {0}", path);
    }
  }
}

