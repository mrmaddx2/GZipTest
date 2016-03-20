using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SkillsTest.GZipTest.Core;

namespace SkillsTest.GZipTest.App
{
    class Program
    {
        static void Main(string[] args)
        {
            MrZipper zipper = null;
            try
            {
                Console.TreatControlCAsInput = true;
                Console.CancelKeyPress += ConsoleOnCancelKeyPress;

                string mode;
                string input;
                string output;
                if (args.Length >= 3)
                {
                    mode = args[0];
                    input = args[1];
                    output = args[2];
                }
                else
                {
                    throw new ArgumentException("В приложение необходимо передать как минимум 3 значения");
                }
                

                long? fragmentSize = null;
                if (args.Length >= 4)
                {
                    long tmpfragmentSize;
                    if (long.TryParse(args[3], out tmpfragmentSize))
                    {
                        fragmentSize = tmpfragmentSize;
                    }
                }

                zipper = new MrZipper();
                zipper.ConvertAsyncCompleted += ZipperOnConvertAsyncCompleted;
                zipper.ProgressChanged += ZipperOnProgressChanged;

                if (string.Equals(mode, "compress", StringComparison.InvariantCultureIgnoreCase))
                {
                    zipper.CompressAsync(input, output, fragmentSize);
                }
                else if (string.Equals(mode, "decompress", StringComparison.InvariantCultureIgnoreCase))
                {
                    zipper.DecompressAsync(input, output);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("args", @"В качестве значения первого параметра допускается только compress\decompress");
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                Environment.ExitCode = 1;
            }
            finally
            {
                do
                {
                    var readed = Console.ReadKey(true);

                    if ((readed.Modifiers & ConsoleModifiers.Control) != 0 && (readed.Key == ConsoleKey.Backspace || readed.Key == ConsoleKey.C))
                    {
                        if (zipper != null)
                        {
                            zipper.CancelConvertAsync();
                        }
                    }
                } while (Environment.ExitCode == 0);
            }
        }

        private static void ZipperOnConvertAsyncCompleted(ConvertAsyncCompletedEventArgs e)
        {
            int exitCode = Environment.ExitCode;

            try
            {
                if (e.Error != null)
                {
                    throw e.Error;
                }

                if (!e.Cancelled)
                {
                    Console.WriteLine("OK");
                }
                else
                {
                    Console.WriteLine("Cancelled");
                    exitCode = 1;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                exitCode = 1;
            }

            if (exitCode != 0)
            {
                Console.ReadKey(true);
            }
            Environment.Exit(exitCode);
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private static void ZipperOnProgressChanged(ProgressChangedEventArgs e)
        {
            string msgText = string.Format("Progress: {0}%", e.ProgressPercentage);

#if DEBUG
            msgText += " Thread " + e.UserState;
#endif

            Console.WriteLine(msgText);
        }
    }
}
