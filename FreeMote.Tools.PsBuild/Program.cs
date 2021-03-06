﻿using System;
using System.IO;
using FreeMote.PsBuild;

namespace FreeMote.Tools.PsBuild
{
    class Program
    {
        //Not thread safe
        private static PsbSpec? _platform = null;
        //private static PsbPixelFormat _pixelFormat = PsbPixelFormat.None;
        private static uint? _key = null;
        private static ushort? _version = null;

        static void Main(string[] args)
        {
            Console.WriteLine("FreeMote PSB Compiler");
            Console.WriteLine("by Ulysses, wdwxy12345@gmail.com");
            Console.WriteLine();

            if (args.Length <= 0 || args[0].ToLowerInvariant() == "/h" || args[0].ToLowerInvariant() == "?")
            {
                PrintHelp();
                return;
            }

            foreach (var s in args)
            {
                if (File.Exists(s))
                {
                    Compile(s);
                }
                else if (s.StartsWith("/v"))
                {
                    if (ushort.TryParse(s.Replace("/v", ""), out var ver))
                    {
                        _version = ver;
                    }
                }
                else if (s.StartsWith("/p"))
                {
                    if (Enum.TryParse(s.Replace("/p", ""), true, out PsbSpec platform))
                    {
                        _platform = platform;
                    }
                }
                //else if (s.StartsWith("/f"))
                //{
                //    if (Enum.TryParse(s.Replace("/f", ""), true, out PsbPixelFormat format))
                //    {
                //        _pixelFormat = format;
                //    }
                //}
                else if (s.StartsWith("/k"))
                {
                    if (uint.TryParse(s.Replace("/k", ""), out var key))
                    {
                        _key = key;
                    }
                    else
                    {
                        _key = null;
                    }
                }
            }

            Console.WriteLine("Done.");
        }

        private static void Compile(string s)
        {
            var name = Path.GetFileNameWithoutExtension(s);
            var ext = Path.GetExtension(s);
            Console.WriteLine($"Compiling {name} ...");
            try
            {
                PsbCompiler.CompileToFile(s, s + (_key == null ? "-pure.psb" : ".psb"), null, _version, _key, _platform);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Compile {name} failed.\r\n{e}");
            }
            Console.WriteLine($"Compile {name} succeed.");
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: .exe [Param] <PSB json path>");
            Console.WriteLine(@"Param:
/v<VerNumber> : Set compile version from [2,4] . Default: 3.
/k<CryptKey> : Set CryptKey. Default: none(Pure PSB). Requirement: uint, dec.
/p<Platform> : Set platform. Default: keep original platform. Support: krkr/win/common/ems.
Warning: Platform ONLY works with .bmp/.png format textures.
");
            Console.WriteLine("Example: PsBuild /v4 /k123456789 /pkrkr emote_sample.psb.json");
        }
    }
}
