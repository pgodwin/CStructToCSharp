using System;
using System.IO;
using System.Linq;

namespace CStructToCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader inputStream;
            StreamWriter outputStream;

            if (args.Length == 0 && !(Console.IsInputRedirected || Console.IsOutputRedirected))
            {
                Console.WriteLine("usage: CStructToCSharp <infile> <outfile>");
                Console.WriteLine("or usage: CStructToCSharp /dir <directory of struct files>");
                return;
            }

            if (args[0] == "/dir")
            {
                var files = Directory.GetFiles(args[1], "*.struct");
                foreach (var file in files.Reverse())
                {
                    var converter = new CStructToCSharpClass(File.OpenText(file), new StreamWriter(File.Open(Path.GetFileNameWithoutExtension(file) + ".cs", FileMode.Create)));
                    converter.Convert("DiscUtils.Hfs.Types");                    
                }
                return;
            }


            if (Console.IsInputRedirected)
                inputStream = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding);
            else
                inputStream = File.OpenText(args[0]);
            if (Console.IsOutputRedirected)
                outputStream = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding);
            else
                outputStream = new StreamWriter(File.OpenWrite(args[1]));
            using (inputStream)
            using (outputStream)
            {
                var converter = new CStructToCSharpClass(inputStream, outputStream);
                converter.Convert("DiscUtils.Hfs.Types");
            }
        }
    }
}
