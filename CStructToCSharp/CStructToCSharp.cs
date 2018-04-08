using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CStructToCSharp
{

    public class StructMember
    {

        public long bp;
        public int size;
        public int arrayLength; //-1 if not an array
        public string typeIdentifier;
        public string identifier;
        public string description; // optional
        public Boolean idIsClassName = false;
        public string dotNetType;
        public bool isClassType;

        public int TotalSize => size * (arrayLength < 0 ? 1 : arrayLength);

        public string FullTypeIdentifier => dotNetType + (arrayLength < 0 ? "" : "[" + arrayLength + "]");

        public string SizeString => size + (arrayLength< 0?"":"*"+arrayLength);
    }
    

    public class CStructToCSharpClass
    {
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        static Dictionary<string, int> typeSizeCache = new Dictionary<string, int>();

        public CStructToCSharpClass(StreamReader reader, StreamWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public void Convert(string namespaceName)
        {
            string currentLine = _reader.ReadLine();

            // First line is the classname
            var className = currentLine.Split(' ')[1];
            Console.WriteLine($"className: {className}");

            var members = ReadMembers(_reader);

            if (!typeSizeCache.ContainsKey(className))
                typeSizeCache.Add(className, members.Sum(m=>m.TotalSize));

            WriteClass(namespaceName, className, members, _writer);
        }

        /// <summary>
        /// Reads the members from the struct definition file and returns a LinkedList of StructMember
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal LinkedList<StructMember> ReadMembers(StreamReader reader)
        {
            LinkedList<StructMember> members = new LinkedList<StructMember>();

            long totalSize = 0;
            string currentLine = reader.ReadLine();
            while (!string.IsNullOrEmpty(currentLine))
            {
                if (currentLine.Trim().Equals("};"))
                    break;

                StructMember current = new StructMember();

                string[] components = Regex.Split(currentLine.Trim(), "\\s+");

                current.bp = totalSize;
                current.typeIdentifier = components[0];
                current.size = typeIDToSize(current.typeIdentifier);

                if (current.size < 0)
                {
                    // Check the cache for the size
                    if (typeSizeCache.ContainsKey(current.typeIdentifier))
                    {
                        current.isClassType = true;
                        current.size = typeSizeCache[current.typeIdentifier];
                    }
                }

                if (current.size < 0)
                {
                    
                    // Can't work out the size, ask the user!
                    Console.WriteLine($"Could not determine size for: \"{current.typeIdentifier}\"");
                    while (true)
                    {
                        Console.WriteLine("Input type size (\"-\" first means id is a class): ");
                        String numberOfBytes = Console.ReadLine();
                        try
                        {
                            current.size = int.Parse(numberOfBytes);
                            current.isClassType = true;

                            typeSizeCache.Add(current.typeIdentifier, current.size);

                            if (current.size < 0)
                            {
                                current.idIsClassName = true;
                                current.size = -current.size;
                            }

                            break;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Invalid input!");
                        }
                    }
                }

                current.arrayLength = parseArraySyntax(components[1]);
                current.identifier = extractIdentifier(components[1]);
                current.description = currentLine.Trim().Substring(components[0].Length + 1).Trim().Substring(components[1].Length).Trim();
                current.dotNetType = ToDotNetType(current.typeIdentifier);

                members.AddLast(current);
                Console.WriteLine($"Finished processing: \"{current.identifier}\"");
                totalSize += current.TotalSize;
                currentLine = reader.ReadLine();
            }

            return members;
        }

        /// <summary>
        /// Builds an ASCI table of members to use int he class
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        public static string[] BuildMemberTable(LinkedList<StructMember> members)
        {
            const string bpColHeader = "BP";
            const string sizeColHeader = "Size";
            const string typeColHeader = "Type";
            const string identifierColHeader = "Identifier";
            const string descriptionColHeader = "Description";

            int bpColWidth = Math.Max(bpColHeader.Length, members.Max(m => m.bp.ToString().Length));
            int sizeColWidth = Math.Max(sizeColHeader.Length, members.Max(m => m.SizeString.Length));
            int typeColWidth = Math.Max(typeColHeader.Length, members.Max(m => m.FullTypeIdentifier.Length));
            int identiferColWidth = Math.Max(identifierColHeader.Length, members.Max(m => m.identifier.Length));
            int descriptionColWidth = Math.Max(descriptionColHeader.Length, members.Max(m => m.description.Length));

            StringBuilder result = new StringBuilder();
            // Write the header
            var header = string.Join(" ", bpColHeader.PadRight(bpColWidth),
                                       sizeColHeader.PadRight(sizeColWidth),
                                       typeColHeader.PadRight(typeColWidth),
                                       identifierColHeader.PadRight(identiferColWidth),
                                       descriptionColHeader.PadRight(descriptionColWidth));

            result.AppendLine(header);
            result.AppendLine(new String('-', header.Length));
            foreach (var member in members)
            {
                result.AppendLine(string.Join(" ", member.bp.ToString().PadLeft((bpColWidth)),
                                                   member.SizeString.PadLeft(sizeColWidth),
                                                   member.FullTypeIdentifier.PadRight(typeColWidth),
                                                   member.identifier.PadRight(identiferColWidth),
                                                   member.description.PadRight(descriptionColWidth)));
            }

            return result.ToString().Split(Environment.NewLine, StringSplitOptions.None);

        }

        public static void WriteClass(string namespaceName, string className, LinkedList<StructMember> members, StreamWriter writer)
        {
            var builder = writer;

            builder.WriteLine("using System;");
            builder.WriteLine("using DiscUtils.Streams;");

            builder.WriteLine();
            builder.WriteLine();

            builder.WriteLine($"namespace {namespaceName}");
            builder.WriteLine("{");

            builder.WriteLine($"\tinternal sealed class {className} : IByteArraySerializable");
            builder.WriteLine("\t{");

            // Add the struct table
            builder.WriteLine("\t\t/*");
            foreach (var line in BuildMemberTable(members))
            {
                builder.WriteLine($"\t\t * {line}");
            }

            builder.WriteLine("\t\t */");

            builder.WriteLine();
            builder.WriteLine($"\t\tpublic const int Structsize = {members.Sum(m => m.TotalSize)};");
            builder.WriteLine();

            foreach (var member in members)
            {
                builder.WriteLine("\t\t/// <summary>");
                builder.WriteLine($"\t\t/// {member.description}");
                builder.WriteLine("\t\t/// </summary>");
                if (member.arrayLength > 0)
                    builder.WriteLine($"\t\tpublic {member.dotNetType}[] {member.identifier} = new {member.FullTypeIdentifier};");
                else
                    builder.WriteLine($"\t\tpublic {member.FullTypeIdentifier} {member.identifier};");
                builder.WriteLine();
            }

            // Build the Reader
            builder.WriteLine("\t\tpublic int ReadFrom(byte[] buffer, int offset)");
            builder.WriteLine("\t\t{");
            foreach (var member in members)
            {
                if (member.arrayLength == -1)
                {
                    builder.Write($"\t\t\t{member.identifier} = ");
                    if (member.isClassType)
                        builder.WriteLine($"EndianUtilities.ToStruct<{member.typeIdentifier}>(buffer, offset + {member.bp});");
                    else if (member.size > 1)
                        builder.WriteLine($"EndianUtilities.To{member.dotNetType}BigEndian(buffer, offset + {member.bp});");
                    else
                        builder.WriteLine($"buffer[offset+{member.bp}];");
                }
                else
                {
                    // We have an array
                    builder.WriteLine($"\t\t\tArray.Copy(buffer, offset + {member.bp}, {member.identifier}, 0, {member.size});");
                }

            }

            builder.WriteLine("\t\t\treturn Structsize;");
            builder.WriteLine("\t\t}");

            // Build the writer
            builder.WriteLine("\t\tpublic void WriteTo(byte[] buffer, int offset)");
            builder.WriteLine("\t\t{");
            foreach (var member in members)
            {
                if (member.isClassType)
                {
                    builder.WriteLine($"\t\t\t{member.identifier}.WriteTo(buffer, offset + {member.bp});");
                }
                else if (member.arrayLength < 0)
                {
                    builder.WriteLine($"\t\t\tEndianUtilities.WriteBytesBigEndian({member.identifier}, buffer, offset + {member.bp});");
                }
                else
                {
                    // We have an array
                    builder.WriteLine($"\t\t\tArray.Copy({member.identifier}, 0, buffer, offset + {member.bp}, {member.identifier}.Length);");
                }

            }

            builder.WriteLine("\t\t}");

            // Add Size Property
            builder.WriteLine("\t\tpublic int Size => Structsize;");

            builder.WriteLine("\t}");

            builder.WriteLine("\t}");

            builder.Flush();
            
        }

        /// <summary>
        /// Returns the bytesize for the specified type.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int typeIDToSize(string typeID)
        {
            if (typeID.Equals("UInt8"))
                return 1;
            else if (typeID.Equals("UInt16"))
                return 2;
            else if (typeID.Equals("UInt32"))
                return 4;
            else if (typeID.Equals("UInt64"))
                return 8;
            else if (typeID.Equals("SInt8"))
                return 1;
            else if (typeID.Equals("SInt16"))
                return 2;
            else if (typeID.Equals("SInt32"))
                return 4;
            else if (typeID.Equals("SInt64"))
                return 8;
            else if (typeID.Equals("Char"))
                return 1;
            else if (typeID.Equals("be32"))
                return 4;
            else if (typeID.Equals("be64"))
                return 8;
            /*
            else if(typeID.equals(""))
                return ;
            else if(typeID.equals(""))
                return ;
            else if(typeID.equals(""))
                return ;
            */
            else
                return -1;
        }

        public static string ToDotNetType(string typeID)
        {
            switch (typeID)
            {
                case "UInt8":
                case "Char":
                    return "byte";
                case "SInt8":
                    return "byte";
                case "SInt16":
                    return "Int16";
                case "SInt32":
                case "be32":
                    return "Int32";
                case "SInt64":
                case "be64":
                    return "Int64";
                default:
                    return typeID;
            }
        }

        /// <summary>
        /// Returns the size of the array from the specified string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int parseArraySyntax(string s)
        {
            //System.out.println("parseArraySyntax(\"" + s + "\");");
            if (Regex.IsMatch(s, ".+\\[[\\d]+\\];"))
            {
                //System.out.println("MATCHES!");
                string numberString = SubstringJava(s, s.LastIndexOf("[") + 1, s.LastIndexOf("]"));
                try
                {
                    int i = int.Parse(numberString);
                    if (i < 0)
                        throw new Exception("Negative array size!");
                    else
                        return i;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            else
            {
                //System.out.println("No match :/");
                //System.out.println("s: \"" + s + "\"");
                return -1;
            }
        }

        /// <summary>
        /// Attempts to extract the member type 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string extractIdentifier(string s)
        {
           
            if (Regex.IsMatch(s, @".+\[[\d]+\]"))
                s = SubstringJava(s, 0, s.LastIndexOf("["));
            else if (s.EndsWith(";"))
                s = SubstringJava(s, 0, s.Length - 1);
            return s;
        }

        static string SubstringJava(string s, int beginIndex, int endIndex)
        {
            // simulates Java substring function
            int len = endIndex - beginIndex;
            return s.Substring(beginIndex, len);
        }
    }
}
