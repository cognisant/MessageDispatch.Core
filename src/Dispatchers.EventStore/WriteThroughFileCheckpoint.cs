﻿// <copyright file="WriteThroughFileCheckpoint.cs" company="Cognisant">
// Copyright (c) Cognisant. All rights reserved.
// </copyright>

namespace CR.MessageDispatch.Dispatchers.EventStore
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Writes a checkpoint to file (pulled from event store)
    /// </summary>
    internal class WriteThroughFileCheckpoint
    {
        private readonly string _filename;
        private readonly string _name;
        private readonly bool _cached;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;
        private readonly MemoryStream _memStream;
        private readonly byte[] _buffer;
        private long _last;
        private long _lastFlushed;
        private FileStream _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteThroughFileCheckpoint"/> class.
        /// </summary>
        /// <param name="filename">Checkpoint filename.</param>
        public WriteThroughFileCheckpoint(string filename)
            : this(filename, Guid.NewGuid().ToString(), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteThroughFileCheckpoint"/> class.
        /// </summary>
        /// <param name="filename">Checkpoint filename.</param>
        /// <param name="name">Checkpoint name.</param>
        public WriteThroughFileCheckpoint(string filename, string name)
            : this(filename, name, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteThroughFileCheckpoint"/> class.
        /// </summary>
        /// <param name="filename">Checkpoint filename.</param>
        /// <param name="name">Checkpoint name.</param>
        /// <param name="cached">Chached?</param>
        /// <param name="initValue">Initial value.</param>
        public WriteThroughFileCheckpoint(string filename, string name, bool cached, long initValue = 0)
        {
            _filename = filename;
            _name = name;
            _cached = cached;
            _buffer = new byte[4096];
            _memStream = new MemoryStream(_buffer);

            var handle = Filenative.CreateFile(
                _filename,
                (uint)FileAccess.ReadWrite,
                (uint)FileShare.ReadWrite,
                IntPtr.Zero,
                (uint)FileMode.OpenOrCreate,
                Filenative.FILE_FLAG_NO_BUFFERING | (int)FileOptions.WriteThrough,
                IntPtr.Zero);

            _stream = new FileStream(handle, FileAccess.ReadWrite, 4096);
            var exists = _stream.Length == 4096;
            _stream.SetLength(4096);
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_memStream);
            if (!exists)
            {
                Write(initValue);
                Flush();
            }

            _last = _lastFlushed = ReadCurrent();
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Closes the checkpoint file stream.
        /// </summary>
        public void Close()
        {
            Flush();
            _stream.Close();
            _stream.Dispose();
        }

        /// <summary>
        /// Writes the checkpoint.
        /// </summary>
        /// <param name="checkpoint">New checkpoint.</param>
        public void Write(long checkpoint)
        {
            Interlocked.Exchange(ref _last, checkpoint);
        }

        /// <summary>
        /// Flushes the checkpoint streams.
        /// </summary>
        public void Flush()
        {
            _memStream.Seek(0, SeekOrigin.Begin);
            _stream.Seek(0, SeekOrigin.Begin);
            var last = Interlocked.Read(ref _last);
            _writer.Write(last);
            _stream.Write(_buffer, 0, _buffer.Length);

            Interlocked.Exchange(ref _lastFlushed, last);

            // FlushFileBuffers(_file.SafeMemoryMappedFileHandle.DangerousGetHandle());
        }

        /// <summary>
        /// Reads the current checkpoint.
        /// </summary>
        /// <returns>Current checkpoint.</returns>
        public long Read()
        {
            return _cached ? Interlocked.Read(ref _lastFlushed) : ReadCurrent();
        }

        /// <summary>
        /// Reads non flushed checkpoint.
        /// </summary>
        /// <returns>Current nonflushed checkpoint.</returns>
        public long ReadNonFlushed()
        {
            return Interlocked.Read(ref _last);
        }

        /// <summary>
        /// Closes and disposes of the streams.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        [DllImport("kernel32.dll")]
        private static extern bool FlushFileBuffers(IntPtr hFile);

        private long ReadCurrent()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            return _reader.ReadInt64();
        }

        private static class Filenative
        {
#pragma warning disable SA1310 // Field names should not contain underscore - Gets pulled from the eventstore DLLs
            public const int FILE_FLAG_NO_BUFFERING = 0x20000000;
#pragma warning restore SA1310 // Field names should not contain underscore

            [DllImport("kernel32", SetLastError = true)]
            internal static extern SafeFileHandle CreateFile(
                string fileName,
                uint desiredAccess,
                uint shareMode,
                IntPtr securityAttributes,
                uint creationDisposition,
                int flagsAndAttributes,
                IntPtr hTemplate);
        }
    }
}
