// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================

using System;
using Sharpen;

namespace Com.Adobe.Xmp.Impl
{
    /// <summary>Byte buffer container including length of valid data.</summary>
    /// <since>11.10.2006</since>
    public class ByteBuffer
    {
        private sbyte[] _buffer;

        private int _length;

        private string _encoding = null;

        /// <param name="initialCapacity">the initial capacity for this buffer</param>
        public ByteBuffer(int initialCapacity)
        {
            this._buffer = new sbyte[initialCapacity];
            this._length = 0;
        }

        /// <param name="buffer">a byte array that will be wrapped with <code>ByteBuffer</code>.</param>
        public ByteBuffer(sbyte[] buffer)
        {
            this._buffer = buffer;
            this._length = buffer.Length;
        }

        /// <param name="buffer">a byte array that will be wrapped with <code>ByteBuffer</code>.</param>
        /// <param name="length">the length of valid bytes in the array</param>
        public ByteBuffer(sbyte[] buffer, int length)
        {
            if (length > buffer.Length)
            {
                throw new IndexOutOfRangeException("Valid length exceeds the buffer length.");
            }
            this._buffer = buffer;
            this._length = length;
        }

        /// <summary>Loads the stream into a buffer.</summary>
        /// <param name="in">an InputStream</param>
        /// <exception cref="System.IO.IOException">If the stream cannot be read.</exception>
        public ByteBuffer(InputStream @in)
        {
            // load stream into buffer
            int chunk = 16384;
            this._length = 0;
            this._buffer = new sbyte[chunk];
            int read;
            while ((read = @in.Read(this._buffer, this._length, chunk)) > 0)
            {
                this._length += read;
                if (read == chunk)
                {
                    EnsureCapacity(_length + chunk);
                }
                else
                {
                    break;
                }
            }
        }

        /// <param name="buffer">a byte array that will be wrapped with <code>ByteBuffer</code>.</param>
        /// <param name="offset">the offset of the provided buffer.</param>
        /// <param name="length">the length of valid bytes in the array</param>
        public ByteBuffer(sbyte[] buffer, int offset, int length)
        {
            if (length > buffer.Length - offset)
            {
                throw new IndexOutOfRangeException("Valid length exceeds the buffer length.");
            }
            this._buffer = new sbyte[length];
            Array.Copy(buffer, offset, this._buffer, 0, length);
            this._length = length;
        }

        /// <returns>Returns a byte stream that is limited to the valid amount of bytes.</returns>
        public virtual InputStream GetByteStream()
        {
            return new ByteArrayInputStream(_buffer, 0, _length);
        }

        /// <returns>
        /// Returns the length, that means the number of valid bytes, of the buffer;
        /// the inner byte array might be bigger than that.
        /// </returns>
        public virtual int Length()
        {
            return _length;
        }

        //    /**
        //     * <em>Note:</em> Only the byte up to length are valid!
        //     * @return Returns the inner byte buffer.
        //     */
        //    public byte[] getBuffer()
        //    {
        //        return buffer;
        //    }
        /// <param name="index">the index to retrieve the byte from</param>
        /// <returns>Returns a byte from the buffer</returns>
        public virtual sbyte ByteAt(int index)
        {
            if (index < _length)
            {
                return _buffer[index];
            }
            else
            {
                throw new IndexOutOfRangeException("The index exceeds the valid buffer area");
            }
        }

        /// <param name="index">the index to retrieve a byte as int or char.</param>
        /// <returns>Returns a byte from the buffer</returns>
        public virtual int CharAt(int index)
        {
            if (index < _length)
            {
                return _buffer[index] & unchecked((int)(0xFF));
            }
            else
            {
                throw new IndexOutOfRangeException("The index exceeds the valid buffer area");
            }
        }

        /// <summary>Appends a byte to the buffer.</summary>
        /// <param name="b">a byte</param>
        public virtual void Append(sbyte b)
        {
            EnsureCapacity(_length + 1);
            _buffer[_length++] = b;
        }

        /// <summary>Appends a byte array or part of to the buffer.</summary>
        /// <param name="bytes">a byte array</param>
        /// <param name="offset">an offset with</param>
        /// <param name="len"/>
        public virtual void Append(sbyte[] bytes, int offset, int len)
        {
            EnsureCapacity(_length + len);
            Array.Copy(bytes, offset, _buffer, _length, len);
            _length += len;
        }

        /// <summary>Append a byte array to the buffer</summary>
        /// <param name="bytes">a byte array</param>
        public virtual void Append(sbyte[] bytes)
        {
            Append(bytes, 0, bytes.Length);
        }

        /// <summary>Append another buffer to this buffer.</summary>
        /// <param name="anotherBuffer">another <code>ByteBuffer</code></param>
        public virtual void Append(ByteBuffer anotherBuffer)
        {
            Append(anotherBuffer._buffer, 0, anotherBuffer._length);
        }

        /// <summary>Detects the encoding of the byte buffer, stores and returns it.</summary>
        /// <remarks>
        /// Detects the encoding of the byte buffer, stores and returns it.
        /// Only UTF-8, UTF-16LE/BE and UTF-32LE/BE are recognized.
        /// <em>Note:</em> UTF-32 flavors are not supported by Java, the XML-parser will complain.
        /// </remarks>
        /// <returns>Returns the encoding string.</returns>
        public virtual string GetEncoding()
        {
            if (_encoding == null)
            {
                // needs four byte at maximum to determine encoding
                if (_length < 2)
                {
                    // only one byte length must be UTF-8
                    _encoding = "UTF-8";
                }
                else
                {
                    if (_buffer[0] == 0)
                    {
                        // These cases are:
                        //   00 nn -- -- - Big endian UTF-16
                        //   00 00 00 nn - Big endian UTF-32
                        //   00 00 FE FF - Big endian UTF 32
                        if (_length < 4 || _buffer[1] != 0)
                        {
                            _encoding = "UTF-16BE";
                        }
                        else
                        {
                            if ((_buffer[2] & unchecked((int)(0xFF))) == unchecked((int)(0xFE)) && (_buffer[3] & unchecked((int)(0xFF))) == unchecked((int)(0xFF)))
                            {
                                _encoding = "UTF-32BE";
                            }
                            else
                            {
                                _encoding = "UTF-32";
                            }
                        }
                    }
                    else
                    {
                        if ((_buffer[0] & unchecked((int)(0xFF))) < unchecked((int)(0x80)))
                        {
                            // These cases are:
                            //   nn mm -- -- - UTF-8, includes EF BB BF case
                            //   nn 00 -- -- - Little endian UTF-16
                            if (_buffer[1] != 0)
                            {
                                _encoding = "UTF-8";
                            }
                            else
                            {
                                if (_length < 4 || _buffer[2] != 0)
                                {
                                    _encoding = "UTF-16LE";
                                }
                                else
                                {
                                    _encoding = "UTF-32LE";
                                }
                            }
                        }
                        else
                        {
                            // These cases are:
                            //   EF BB BF -- - UTF-8
                            //   FE FF -- -- - Big endian UTF-16
                            //   FF FE 00 00 - Little endian UTF-32
                            //   FF FE -- -- - Little endian UTF-16
                            if ((_buffer[0] & unchecked((int)(0xFF))) == unchecked((int)(0xEF)))
                            {
                                _encoding = "UTF-8";
                            }
                            else
                            {
                                if ((_buffer[0] & unchecked((int)(0xFF))) == unchecked((int)(0xFE)))
                                {
                                    _encoding = "UTF-16";
                                }
                                else
                                {
                                    // in fact BE
                                    if (_length < 4 || _buffer[2] != 0)
                                    {
                                        _encoding = "UTF-16";
                                    }
                                    else
                                    {
                                        // in fact LE
                                        _encoding = "UTF-32";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // in fact LE
            return _encoding;
        }

        /// <summary>
        /// Ensures the requested capacity by increasing the buffer size when the
        /// current length is exceeded.
        /// </summary>
        /// <param name="requestedLength">requested new buffer length</param>
        private void EnsureCapacity(int requestedLength)
        {
            if (requestedLength > _buffer.Length)
            {
                sbyte[] oldBuf = _buffer;
                _buffer = new sbyte[oldBuf.Length * 2];
                Array.Copy(oldBuf, 0, _buffer, 0, oldBuf.Length);
            }
        }
    }
}