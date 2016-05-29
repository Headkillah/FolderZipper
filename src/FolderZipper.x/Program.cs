using System;
using System.Threading;
using FolderZipper;

namespace FolderZipper5
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            Console.WriteLine("Start");
            var p = new ProgressBar();
            for (int i = 0; i <= 100; i++)
            {
                p.Draw(i);
                Thread.Sleep(10);
            }

            Console.WriteLine("End");
        }
    }
}