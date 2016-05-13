using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;

namespace ImageComparerSimple
{
    class Program
    {
        static List<string> iterated = new List<string>();
        static object threadLock = new object();
        static int todo = 0;
        static int done = 0;
        static int id = 0;
        static int current = 0;
        static int startCurrent = -1;
        static int currentTargetID = 0;
        static int startTarget = -1;
        static bool skipStart { get { return startCurrent >= 0; } }
        static bool skipTarget { get { return startTarget >= 0; } }

        static List<string> dirsToGoThrough = new List<string>();
        static List<string> dirsToGoCompare = new List<string>();

        static string[] filters = new string[] { "jpg", "jpeg", "png", "gif" };

        static object dataLock = new object();
        static Dictionary<string, Reference> datas = new Dictionary<string, Reference>();

        static void Main(string[] args)
        {
            Console.WriteLine("enters dirs separated by ; or a file that has the input");
            Console.WriteLine("Folder that has originals");
            string originDir = Console.ReadLine();
            Console.WriteLine("Folder in which to find");
            string findDir = Console.ReadLine();

            dirsToGoThrough.AddRange(originDir.Split(';').Where(x=>x.Trim().Count() > 0));
            dirsToGoCompare.AddRange(findDir.Split(';').Where(x => x.Trim().Count() > 0));

            if (dirsToGoCompare.Count == 0)
                dirsToGoCompare.Add("folders.txt");
            if (dirsToGoThrough.Count == 0)
                dirsToGoThrough.Add("folders.txt");

            GetFolders(dirsToGoThrough);
            GetFolders(dirsToGoCompare);

            Console.WriteLine("GO THROUGH:");
            foreach (var item in dirsToGoThrough)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("AND COMPARE:");
            foreach (var item in dirsToGoCompare)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Last id reached:");
            if(!int.TryParse(Console.ReadLine(), out startCurrent))
                startCurrent = -1;
            Console.WriteLine("Last target id reached:");
            if (!int.TryParse(Console.ReadLine(), out startTarget))
                startTarget -= 1;

            if (Directory.Exists(Directory.GetCurrentDirectory()+"/results"))
                Directory.Delete(Directory.GetCurrentDirectory() + "/results", true);
            if (File.Exists("log.txt"))
                File.Delete("log.txt");

            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/results");

            foreach (var item in dirsToGoThrough)
            {
                IterateFolder(item, dirsToGoCompare);
                Wait(() => done);
            }
        }

        static void GetFolders(List<string> target)
        {
            int extra = 0;
            for (int i = target.Count - 1; i >= 0; i--)
            {
                string dir = target[i];
                if (File.Exists(dir))
                {
                    target.RemoveAt(i);
                    int c = target.Count;
                    target.AddRange(File.ReadAllText(dir).Split(';'));
                    extra += target.Count - c;
                }
                else if (!Directory.Exists(dir))
                {
                    target.RemoveAt(i);
                }
            }
            while (extra != 0)
            {
                int e = extra;
                int j = target.Count - 1;
                for (int i = 0; i < extra; i++)
                {
                    string dir = target[j];
                    if (File.Exists(dir))
                    {
                        target.RemoveAt(j);
                        int c = target.Count;
                        target.AddRange(File.ReadAllText(dir).Split(';'));
                        extra += target.Count - c;
                    }
                    else if (!Directory.Exists(dir))
                    {
                        target.RemoveAt(j);
                    }
                    j -= 1;
                }
                extra -= e;
            }
        }

        static void Wait(Func<int> t)
        {
            while (true)
            {
                if (t() == 0) return;
                System.Threading.Thread.Sleep(100);
            }
        }

        static void IterateFolder(string folder, IEnumerable<string> dirsToCompare)
        {
            Console.WriteLine();
            Console.WriteLine(folder);

            done -= 1;
            foreach (var filter in filters)
                foreach (string file in Directory.GetFiles(folder, "*." + filter))
                {
                    if (skipStart)
                    {
                        current += 1;
                        startCurrent -= 1;
                        continue;
                    }
                    currentTargetID = 0;
                    Console.WriteLine((current++)+" " + file);
                    Console.WriteLine();
                    lock (printLock)
                        lastPrint = 0;
                    foreach (string findDir in dirsToCompare)
                        FindFileWithinFolder(file, findDir);

                    Wait(() => { lock (threadLock) return todo; });

                    List<string> results = datas[file].results;
                    if (datas[file].AlreadyProcessed) continue;
                    if (results.Count > 0)
                    {
                        int idd = 0;
                        lock (threadLock)
                            idd = id++;
                        using (StreamWriter w = new StreamWriter("results/" + idd + ".txt"))
                        {
                            iterated.Add(file);
                            w.WriteLine(file);
                            w.WriteLine(new string('-', 10));
                            foreach (var item in results)
                                w.WriteLine(item);
                        }
                    }
                }

            foreach (var item in Directory.GetDirectories(folder))
                IterateFolder(item, dirsToCompare);
            Wait(() => { lock (threadLock) return todo; });
            done += 1;
        }

        static void InnerIterateFolder(string targetFile, string folder, Queue<string> children)
        {
            foreach (var filter in filters)
                try
                {
                    foreach (string file in Directory.GetFiles(folder, "*." + filter))
                        if (file == targetFile) continue;
                        else
                        {
                            Compare(targetFile, file);
                        }
                }
                catch (Exception ex)
                {
                    using (System.IO.StreamWriter w = new StreamWriter("log.txt", true))
                        w.Write(ex.Message);
                }
            foreach (var item in Directory.GetDirectories(folder))
                children.Enqueue(item);
        }

        private static object printLock = new object();
        static int lastPrint = 0;
        static void Print(string msg)
        {
            lock (printLock)
            {
                Console.CursorLeft = 0;
                Console.CursorTop += -lastPrint / Console.BufferWidth - 1;

                Console.Write(new string(' ', lastPrint));

                Console.CursorLeft = 0;
                Console.CursorTop += -lastPrint / Console.BufferWidth;

                Console.WriteLine(msg);

                lastPrint = msg.Length;
            }
        }

        static void Compare(string file, string other, bool stopOnFirstFound = false)
        {
            if (skipTarget)
            {
                currentTargetID += 1;
                startTarget -= 1;
                return;
            }

            while (true)
            {
                lock (threadLock)
                    if (todo < Environment.ProcessorCount * 3) { todo += 1; break; }
                System.Threading.Thread.Sleep(100);
            }

            System.Threading.ThreadPool.QueueUserWorkItem((x) =>
            {
                Comparison c = x as Comparison;
                c.Initialize();
                c.Compare();
                c.ClearTarget();
                lock (threadLock)
                {
                    if (todo == 0) return;
                    todo -= 1;
                    if (stopOnFirstFound)
                        if (c.results.Count > 0)
                            todo = 0;
                }
            }, new Comparison() { File = file, Target = other });
        }

        static void FindFileWithinFolder(string file, string folder)
        {
            if (iterated.Count > 0)
            {
                foreach (var item in iterated)
                {
                    Compare(file, item, true);
                }
                Wait(() => { lock (threadLock) return todo; });
                List<string> results = datas[file].results;

                if (results.Count > 0)
                {
                    datas[file].AlreadyProcessed = true;
                    return;
                }
            }

            Queue<string> children = new Queue<string>();
            children.Enqueue(folder);
            while(children.Count > 0)
                InnerIterateFolder(file, children.Dequeue(), children);
        }

        class Reference
        {
            public string Target;
            public object InUseLock = new object();
            public bool AlreadyProcessed;
            public BitmapData Data;
            public System.Drawing.Bitmap Bitmap;
            public List<string> results;
        }

        class Comparison
        {
            public string File;
            public string Target;
            public List<string> results { get { return FileRef.results; } }
            bool matchingingDimensions = true;

            Reference FileRef { get { lock (dataLock) { if (datas.ContainsKey(File)) return datas[File]; } return null; } }
            Reference TargetRef { get { lock (dataLock) { if (datas.ContainsKey(Target)) return datas[Target]; } return null; } }

            System.Drawing.Bitmap FileBitmap { get { Reference reference = FileRef; if (reference == null) return null; return reference.Bitmap; } }
            System.Drawing.Bitmap TargetBitmap { get { Reference reference = TargetRef; if (reference == null) return null; return reference.Bitmap; } }

            BitmapData FileData { get { Reference reference = FileRef; if (reference == null) return null; return reference.Data; } }
            BitmapData TargetData { get { Reference reference = TargetRef; if (reference == null) return null; return reference.Data; } }

            public void ClearTarget()
            {
                lock (dataLock)
                {
                    Reference reference = TargetRef;
                    datas.Remove(Target);
                    try
                    {
                        reference.Bitmap.UnlockBits(reference.Data);
                        reference.Bitmap.Dispose();
                        reference.results.Clear();
                        reference.results = null;
                    }
                    catch { }
                }
            }

            public void Initialize()
            {
                lock (dataLock)
                {
                    matchingingDimensions = true;
                    Reference fileRef = FileRef, targetRef = TargetRef;
                    bool initFile = fileRef == null;
                    bool initTarget = targetRef == null;
                    if (fileRef == null)
                    {
                        fileRef = new Reference();
                        try
                        {
                            fileRef.Bitmap = new System.Drawing.Bitmap(File);
                        }
                        catch { matchingingDimensions = false; return; }
                    }
                    if (targetRef == null)
                    {
                        targetRef = new Reference();
                        try
                        {
                            targetRef.Bitmap = new System.Drawing.Bitmap(Target);
                        }
                        catch { matchingingDimensions = false; return; }
                    }

                    if (!matchingingDimensions) return;
                    if (fileRef.Bitmap.Width != targetRef.Bitmap.Width) matchingingDimensions = false;
                    if (fileRef.Bitmap.Height != targetRef.Bitmap.Height) matchingingDimensions = false;

                    if (initFile)
                    {
                        fileRef.Data = fileRef.Bitmap.LockBits(new System.Drawing.Rectangle(0, 0, fileRef.Bitmap.Width, fileRef.Bitmap.Height), ImageLockMode.ReadOnly, fileRef.Bitmap.PixelFormat);
                        datas.Add(File, fileRef);
                        fileRef.results = new List<string>();
                        fileRef.Target = File;
                    }

                    if (initTarget)
                    {
                        targetRef.Data = targetRef.Bitmap.LockBits(new System.Drawing.Rectangle(0, 0, targetRef.Bitmap.Width, targetRef.Bitmap.Height), ImageLockMode.ReadOnly, targetRef.Bitmap.PixelFormat);
                        datas.Add(Target, targetRef);
                        targetRef.results = new List<string>();
                        targetRef.Target = Target;
                    }
                }
            }

            byte[] Get(Reference data)
            {
                Reference reference = data;
                if (data == null) return null;
                IntPtr ptr = reference.Data.Scan0;
                int bytes = Math.Abs(reference.Data.Stride) * reference.Bitmap.Height; ;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                return rgbValues;
            }

            bool Compare(byte[] a, byte[] b)
            {
                if (a == null || b == null) return false;

                for (int i = 0; i < a.Length; i += 1)
                {
                    if (a[i] != b[i])
                        return false;
                }
                return true;
            }

            public void Compare()
            {
                lock (printLock)
                {
                    Print((currentTargetID++) + " " + Target);
                }
                if (matchingingDimensions)
                    if (Compare(Get(FileRef), Get(TargetRef)))
                        lock (threadLock)
                            results.Add(Target);
            }
        }
    }
}