CStructToCSharp
===============

This is a quick tool to convert CStruct definitions to C# Classes for use with DiscUtils.

It is based on [CStructToJavaClass](https://sourceforge.net/p/catacombae/catacombae/ci/master/tree/CStructToJavaClass/CStructToJavaClass.java) by [Erik Larsson](http://www.catacombae.org/), which was used in his excellent HFSExplorer application. 

## Usage
The application supports reading data from stdin and stdout.
Alternatively you can pass an input file and output filename to write the files out
`CStructToCSharp.exe <infile> <outfile>`

Or you can specify a directory and process an entire directory at once.
`CstructToCSharp.exe /dir <dirpath>`
 
### Namespaces
Currently there is no way to specify the namespace from the arguments, instead in `Program.cs` change `converter.Convert("DiscUtils.Hfs.Types");` to the appropriate namespace for your usage.


## Example Input

```
struct BTHdrRec {
        SInt16          bthDepth;       current depth of tree (Integer)
        SInt32          bthRoot;        number of root node (LongInt)
        SInt32          bthNRecs;       number of leaf records in tree (LongInt)
        SInt32          bthFNode;       number of first leaf node (LongInt)
        SInt32          bthLNode;       number of last leaf node (LongInt)
        SInt16          bthNodeSize;    size of a node (Integer)
        SInt16          bthKeyLen;      maximum length of a key (Integer)
        SInt32          bthNNodes;      total number of nodes in tree (LongInt)
        SInt32          bthFree;        number of free nodes (LongInt)
        SInt8           bthResv[76];    reserved (ARRAY[1..76] OF SignedByte)
};
```

## Example Output

```csharp

using System;
using DiscUtils.Streams;


namespace DiscUtils.Hfs.Types
{
	internal sealed class BTHdrRec : IByteArraySerializable
	{
		/*
		 * BP Size Type     Identifier  Description                             
		 * ---------------------------------------------------------------------
		 *  0    2 Int16    bthDepth    current depth of tree (Integer)         
		 *  2    4 Int32    bthRoot     number of root node (LongInt)           
		 *  6    4 Int32    bthNRecs    number of leaf records in tree (LongInt)
		 * 10    4 Int32    bthFNode    number of first leaf node (LongInt)     
		 * 14    4 Int32    bthLNode    number of last leaf node (LongInt)      
		 * 18    2 Int16    bthNodeSize size of a node (Integer)                
		 * 20    2 Int16    bthKeyLen   maximum length of a key (Integer)       
		 * 22    4 Int32    bthNNodes   total number of nodes in tree (LongInt) 
		 * 26    4 Int32    bthFree     number of free nodes (LongInt)          
		 * 30 1*76 byte[76] bthResv     reserved (ARRAY[1..76] OF SignedByte)   
		 * 
		 */

		public const int Structsize = 106;

		/// <summary>
		/// current depth of tree (Integer)
		/// </summary>
		public Int16 bthDepth;

		/// <summary>
		/// number of root node (LongInt)
		/// </summary>
		public Int32 bthRoot;

		/// <summary>
		/// number of leaf records in tree (LongInt)
		/// </summary>
		public Int32 bthNRecs;

		/// <summary>
		/// number of first leaf node (LongInt)
		/// </summary>
		public Int32 bthFNode;

		/// <summary>
		/// number of last leaf node (LongInt)
		/// </summary>
		public Int32 bthLNode;

		/// <summary>
		/// size of a node (Integer)
		/// </summary>
		public Int16 bthNodeSize;

		/// <summary>
		/// maximum length of a key (Integer)
		/// </summary>
		public Int16 bthKeyLen;

		/// <summary>
		/// total number of nodes in tree (LongInt)
		/// </summary>
		public Int32 bthNNodes;

		/// <summary>
		/// number of free nodes (LongInt)
		/// </summary>
		public Int32 bthFree;

		/// <summary>
		/// reserved (ARRAY[1..76] OF SignedByte)
		/// </summary>
		public byte[] bthResv = new byte[76];

		public int ReadFrom(byte[] buffer, int offset)
		{
			bthDepth = EndianUtilities.ToInt16BigEndian(buffer, offset + 0);
			bthRoot = EndianUtilities.ToInt32BigEndian(buffer, offset + 2);
			bthNRecs = EndianUtilities.ToInt32BigEndian(buffer, offset + 6);
			bthFNode = EndianUtilities.ToInt32BigEndian(buffer, offset + 10);
			bthLNode = EndianUtilities.ToInt32BigEndian(buffer, offset + 14);
			bthNodeSize = EndianUtilities.ToInt16BigEndian(buffer, offset + 18);
			bthKeyLen = EndianUtilities.ToInt16BigEndian(buffer, offset + 20);
			bthNNodes = EndianUtilities.ToInt32BigEndian(buffer, offset + 22);
			bthFree = EndianUtilities.ToInt32BigEndian(buffer, offset + 26);
			Array.Copy(buffer, offset + 30, bthResv, 0, 1);
			return Structsize;
		}
		public void WriteTo(byte[] buffer, int offset)
		{
			EndianUtilities.WriteBytesBigEndian(bthDepth, buffer, offset + 0);
			EndianUtilities.WriteBytesBigEndian(bthRoot, buffer, offset + 2);
			EndianUtilities.WriteBytesBigEndian(bthNRecs, buffer, offset + 6);
			EndianUtilities.WriteBytesBigEndian(bthFNode, buffer, offset + 10);
			EndianUtilities.WriteBytesBigEndian(bthLNode, buffer, offset + 14);
			EndianUtilities.WriteBytesBigEndian(bthNodeSize, buffer, offset + 18);
			EndianUtilities.WriteBytesBigEndian(bthKeyLen, buffer, offset + 20);
			EndianUtilities.WriteBytesBigEndian(bthNNodes, buffer, offset + 22);
			EndianUtilities.WriteBytesBigEndian(bthFree, buffer, offset + 26);
			Array.Copy(bthResv, 0, buffer, offset + 30, bthResv.Length);
		}
		public int Size => Structsize;
	}
}
```