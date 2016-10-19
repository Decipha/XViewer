using System;
using System.Collections;

namespace Binary.Conversion
{
    public class BitMan
    {
        /// <summary>
        /// masks for detecting or setting individual bits within a 32 bit unsigned integer
        /// </summary>
        private static uint[] _masks = new uint[] {
            1,2,4,8,16,32,64,128,256,512,1024,2048,4096,8192,16384,32768,65536, 131072, 262144,524288,1048576,2097152, 4194304,
            8388608,16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824, 2147483648
        };

        /// <summary>
        /// returns a to the power b using integers (Math.Pow uses Doubles!!!)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Pow(int a, int b)
        {
            int temp = 1;
            for (int i = 0; i < b; i++)
                temp *= a;
            return temp;
        }

        #region Set Bit and Get Bit

        /// <summary>
        /// get the bit at the specified position. 0 is the least significant bit.
        /// uses bitwise logical operations to detect individual bits.
        /// </summary>
        /// <param name="value">the value of the specified bit within the 32 bit integer</param>
        /// <param name="position">the position of the bit to test, 0 = least significant bit</param>
        /// <returns></returns>
        public static bool GetBit(uint value, int position)
        {
            // calculate the bit mask (eg 0 = 0000-0001, 1 = 0000-0010, 2 = 0000-0100 etc)
            // int mask = Pow(2, position);
            // replaced with a static array of precalculated masks.

            // do a bitwise and on the mask and the value. if the bit at the correct position in the mask (0= least sig)
            // is true, then the result will be the same as the mask.
            // eg:
            // value     = 0101 0101
            // mask      = 0000 0100
            // bitwise & = 0000 0100
            return ((_masks[position] & value) == _masks[position]);
        }

        /// <summary>
        /// get the bit at the specified position. 0 is the least significant bit.
        /// uses bitwise logical operations to detect individual bits.
        /// </summary>
        /// <param name="value">the value of the specified bit within the 32 bit integer</param>
        /// <param name="position">the position of the bit to test, 0 = least significant bit</param>
        /// <returns></returns>
        public static bool GetBit(byte value, int position)
        {
            // calculate the bit mask (eg 0 = 0000-0001, 1 = 0000-0010, 2 = 0000-0100 etc)
            // int mask = Pow(2, position);
            // replaced with a static array of precalculated masks.

            // do a bitwise and on the mask and the value. if the bit at the correct position in the mask (0= least sig)
            // is true, then the result will be the same as the mask.
            // eg:
            // value     = 0101 0101
            // mask      = 0000 0100
            // bitwise & = 0000 0100
            return ((_masks[position] & value) == _masks[position]);
        }

        /// <summary>
        /// set the value of a specific bit within an un-signed 32 bit integer.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public static void SetBit(ref uint target, int position, bool value)
        {
            if (position > 31 || position < 0)
                throw new ArgumentException("Out of Range: " + position, "position");

            if (value)
                SetBitTrue(ref target, position);
            else
                SetBitFalse(ref target, position);
        }

        /// <summary>
        /// set the value of a specific bit within an un-signed 32 bit integer.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public static void SetBit(ref byte target, int position, bool value)
        {
            if (position > 7 || position < 0)
                throw new ArgumentException("Out of Range: " + position, "position");

            if (value)
                SetBitTrue(ref target, position);
            else
                SetBitFalse(ref target, position);
        }

        /// <summary>
        /// set the bit at the specified position (0 = least significant bit) to true.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position">the bit to set. 0 is the least significant bit, 32 is the most significant bit</param>
        public static void SetBitTrue(ref uint target, int position)
        {
            // create a mask that sets a single bit to true at the specified position:
            // int mask = Pow(2, position);

            // use a bitwise or to toggle the bit on:
            // target value:    1010 1010
            //  mask  value:    0000 0100
            // logic or result: 1010 1110
            target = (target | _masks[position]);
        }

        /// <summary>
        /// set the bit at the specified position (0 = least significant bit) to true.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position">the bit to set. 0 is the least significant bit, 32 is the most significant bit</param>
        public static void SetBitTrue(ref byte target, int position)
        {
            // create a mask that sets a single bit to true at the specified position:
            // int mask = Pow(2, position);

            // use a bitwise or to toggle the bit on:
            // target value:    1010 1010
            //  mask  value:    0000 0100
            // logic or result: 1010 1110
            target = (byte)(target | _masks[position]);
        }

        /// <summary>
        /// use bitwise masks and operators to set any given bit to false;
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position"></param>
        public static void SetBitFalse(ref uint target, int position)
        {
            // use an and-not operation to set the particular bit to false;

            // eg set bit position 3 (zero based) to false:
            // index    0 1 2 3
            // mask     0 0 0 1

            // not mask 1 1 1 0
            // target   1 0 1 1
            // ----------------
            // and oper 1 0 1 0
            target = (target & ~_masks[position]);
        }

        /// <summary>
        /// use bitwise masks and operators to set any given bit to false;
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position"></param>
        public static void SetBitFalse(ref byte target, int position)
        {
            // use an and-not operation to set the particular bit to false;

            // eg set bit position 3 (zero based) to false:
            // index    0 1 2 3
            // mask     0 0 0 1

            // not mask 1 1 1 0
            // target   1 0 1 1
            // ----------------
            // and oper 1 0 1 0
            target = (byte)(target & ~_masks[position]);
        }

        #endregion Set Bit and Get Bit

        /// <summary>
        /// pack the first bitsToPack, starting at offset (zero based) from the input array into another (shorter) byte array.
        /// ie to pack the last two (the most significant bits) of each byte into another array:
        /// byte[] packed = PackBits(data, 2, 6);
        /// </summary>
        /// <param name="input">source data array</param>
        /// <param name="bitsToPack">number of bits to select</param>
        /// <param name="offset">starting bit position (0 based, starting from least significant)</param>
        /// <returns></returns>
        public static byte[] PackBits(byte[] input, int bitsToPack, int offset)
        {
            if (bitsToPack + offset > 8)
                throw new ArgumentException("Out of Range: bitsToPack + offset must be <= 8");

            // create the output bit-array:
            BitArray bits = new BitArray(input.Length * bitsToPack);
            int index = 0;
            foreach (byte inputByte in input)
            {
                for (int pos = offset; pos < (offset + bitsToPack); pos++)
                {
                    bits[index++] = GetBit(inputByte, pos);
                }
            }

            // calculate the byte length of the output:
            int remainder = bits.Length % 8;

            // get the number of whole bytes:
            int byteLen = (bits.Length + (8 - remainder)) / 8;

            // create an output buffer:
            byte[] output = new byte[byteLen];

            // copy the bit array into the byte array:
            bits.CopyTo(output, 0);

            // return the byte array:
            return output;
        }

        /// <summary>
        /// unpack bits from the input byte array to the output byte array.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="bitsToUnPack"></param>
        /// <param name="target_offset">the target start index to write the bits into the output bytes</param>
        /// <returns></returns>
        public static byte[] UnPackBits(byte[] input, int bitsToUnPack, int target_offset)
        {
            // access the input byte array bit at a time:
            BitArray data = new BitArray(input);

            // how many bits in the array?
            int bitcount = data.Length;

            // how many extra bits? (there could be up to 7 due to fitting the packed bits into a byte)
            int leftover = bitcount % bitsToUnPack;

            // get the exact bit count:
            bitcount -= leftover;

            // calculate the number of whole output bytes
            int byteCount = bitcount / bitsToUnPack;

            // create the output array:
            byte[] output = new byte[byteCount];

            // populate the output array:
            // track the current read index.
            int read_index = 0; int write_index = 0;

            // while data is still available:
            while (read_index < data.Length && write_index < output.Length)
            {
                // create the next byte:
                byte buffer = 0;

                // read the next bitsToUnPack from the bit-array:
                for (int r = target_offset; r < (target_offset + bitsToUnPack); r++)
                {
                    // toggle the bit on in the integer if the bit-array index is true.
                    if (data[read_index++])
                        SetBitTrue(ref buffer, r);

                    // if the index has exceeded the length, quit.
                    if (read_index >= data.Length)
                        break;
                }

                // store the result:
                output[write_index++] = buffer;
            }

            return output;
        }

        /// <summary>
        /// write the source bytes into selected bit positions of the target array, starting at source_index (of the source array) and writing source_length bytes.
        /// the bits are written into bit stegoBitOffset to stegoBitOffset + stegoBits.
        ///
        /// to write over the two least significant bits  stegoBits =2, stegoBitOffset = 0
        /// to write over the two most  significant bits, stegoBits =2, stegoBitOffset = 5 (zero based indexing)
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="source_index">byte index to start reading in the source</param>
        /// <param name="source_length">number of bytes to write from the source</param>
        /// <param name="target_index">byte index to start writing in the target</param>
        /// <param name="stegoBits">the number of bits to encode into
        /// the higher this number, the more data is encoded into the same target array, but the larger the change, and therefore the
        /// more likely the data is to be detected or to corrupt the output data. this can only be run over
        /// data that is loss tolerant (such as image or sound data) without causing corruption.
        /// </param>
        /// <param name="stegoBitOffset">the starting bit position for the stegano bit</param>
        public static int SteganoWrite(byte[] target, byte[] source, int source_index, int source_length, int target_index, int stegoBits = 2, int stegoBitOffset = 0)
        {
            // calculate the number of bits to be written:
            int writeBits = (source_length) * 8;

            // calculate the byte requirement:
            int requiredBytes = (writeBits / stegoBits);

            // throw an exception if there isn't enough space:
            if (target.Length < requiredBytes)
            {
                throw new ArgumentException("Insufficient Space in Target Array");
            }

            int write_byte = target_index;
            int write_bit = stegoBitOffset;
            int read_index = source_index * 8;
            int read_index_end = read_index + (source_length * 8);

            // need to access the source array by bit:
            BitArray sourceBits = new BitArray(source);

            // keep moving through the source bits:
            while (read_index < read_index_end)
            {
                // set the particular bit on the current target byte:
                SetBit(ref target[write_byte], write_bit++, sourceBits[read_index]);

                // if write_bit has exceeded upper position limit, reset the write bit to the start and move to the next byte.
                if (write_bit >= (stegoBits + stegoBitOffset))
                {
                    write_bit = stegoBitOffset;
                    write_byte++;
                }

                // move to the next read-bit
                read_index++;
            }

            return write_byte;
        }

        /// <summary>
        /// read selected bits (stegoBitOffset to stegoBitOffset + stegoBits) from each byte of the source array into a bit array, which is
        /// then copied into a byte array.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start_index">the starting index in the source array</param>
        /// <param name="read_target_bytes">the number of source bytes to process</param>
        /// <param name="stegoBits">the number of bits to decode</param>
        /// <returns></returns>
        public static byte[] SteganoRead(byte[] source, int start_index, int read_target_bytes, int stegoBits = 2, int stegoBitOffset = 0)
        {
            if (start_index + read_target_bytes > source.Length)
                throw new ArgumentException("source length insufficient");

            // read int the specified bits from each byte in the source
            // and pack into a byte array;
            int outputbits = read_target_bytes * 8;

            // create an empty bit array:
            BitArray output = new BitArray(outputbits);

            // track the read and write locations;
            int outputIndex = 0;
            int sourceIndex = start_index;
            int read_bit = stegoBitOffset;

            // walk through the source byte array:
            while (outputIndex < outputbits)
            {
                // set the output bit based on the value of the bit at current position in the source byte:
                output[outputIndex++] = GetBit(source[sourceIndex], read_bit++);

                if (read_bit >= (stegoBits + stegoBitOffset))
                {
                    read_bit = stegoBitOffset;
                    sourceIndex++;
                }
            }

            // declare the results array:
            byte[] results = new byte[read_target_bytes];

            // copy the bits into it:
            output.CopyTo(results, 0);

            // return the results:
            return results;
        }

        /// <summary>
        /// add redundant bits to the data in order to assist in recovering it from a noisy result.
        /// triples the length of the data in the process which is inefficient.
        ///
        /// used to attempt to write steganographic data into a jpeg. jpeg is normally too lossy to be accurate in the low order bits.
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] Add31RepetitionRedundancy(byte[] source)
        {
            // each bit goes into the output 3 times.
            byte[] outputArray = new byte[source.Length * 3];

            BitArray src = new BitArray(source);
            BitArray dst = new BitArray(src.Length * 3);

            int read_index = 0; int write_index = 0; int count = 0;

            while (read_index < src.Length)
            {
                dst[write_index++] = src[read_index];
                count++;
                if (count == 3)
                {
                    read_index++; count = 0;
                }
            }

            // copy to the output array:
            dst.CopyTo(outputArray, 0);

            // return the output array:
            return outputArray;
        }

        /// <summary>
        /// recovers the original data from the source array that was encoded using 31 repetition redundancy.
        /// each triplet of bits is recovered, and the most common value is used.
        ///
        /// ie:
        ///
        /// 000 = 0 // clean
        /// 010 = 0 // dirty, result 0
        /// 001 = 0 // dirty, result 0
        /// 100 = 0 // dirty, result 0
        ///
        /// 111 = 1 // clean
        /// 101 = 1 // dirty, result 1
        /// 110 = 1 // dirty, result 1
        /// 011 = 1 // dirty, result 1
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] Decode31RepetitionRedundancy(byte[] source)
        {
            BitArray output = new BitArray((source.Length * 8) / 3);
            BitArray input = new BitArray(source);

            int write_index = 0;
            int read_index = 0;

            while (read_index < input.Length)
            {
                // read three bits from the source:
                bool a = input[read_index++];
                bool b = input[read_index++];
                bool c = input[read_index++];

                // get the democratic result & write that to the output:
                output[write_index++] = CommonOfThree(a, b, c);
            }

            byte[] result = new byte[source.Length / 3];
            output.CopyTo(result, 0);
            return result;
        }

        public static bool CommonOfThree(bool a, bool b, bool c)
        {
            int total = (a ? 1 : 0) + (b ? 1 : 0) + (c ? 1 : 0);

            return (total >= 2);
        }
    }
}