using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace JPGCorrupt 
{
    public sealed class FileStreamWithBackup : FileStream
    {
        public FileStreamWithBackup(string path, long maxFileLength, int maxFileCount, FileMode mode)
            : base(path, BaseFileMode(mode), FileAccess.Write)
        {
            Init(path, maxFileLength, maxFileCount, mode);
        }

        public FileStreamWithBackup(string path, long maxFileLength, int maxFileCount, FileMode mode, FileShare share)
            : base(path, BaseFileMode(mode), FileAccess.Write, share)
        {
            Init(path, maxFileLength, maxFileCount, mode);
        }

        public FileStreamWithBackup(string path, long maxFileLength, int maxFileCount, FileMode mode, FileShare share, int bufferSize)
            : base(path, BaseFileMode(mode), FileAccess.Write, share, bufferSize)
        {
            Init(path, maxFileLength, maxFileCount, mode);
        }

        public FileStreamWithBackup(string path, long maxFileLength, int maxFileCount, FileMode mode, FileShare share, int bufferSize, bool isAsync)
            : base(path, BaseFileMode(mode), FileAccess.Write, share, bufferSize, isAsync)
        {
            Init(path, maxFileLength, maxFileCount, mode);
        }

        public override bool CanRead { get { return false; } }

        public override void Write(byte[] array, int offset, int count)
        {
            int actualCount = System.Math.Min(count, array.GetLength(0));
            if (Position + actualCount <= m_maxFileLength)
            {
                base.Write(array, offset, count);
            }
            else
            {
                if (CanSplitData)
                {
                    int partialCount = (int)(System.Math.Max(m_maxFileLength, Position) - Position);
                    base.Write(array, offset, partialCount);
                    offset += partialCount;
                    count = actualCount - partialCount;
                }
                else
                {
                    if (count > m_maxFileLength)
                        throw new ArgumentOutOfRangeException("Buffer size exceeds maximum file length");
                }
                BackupAndResetStream();
                Write(array, offset, count);
            }
        }

        public long MaxFileLength { get { return m_maxFileLength; } }
        public int MaxFileCount { get { return m_maxFileCount; } }
        public bool CanSplitData { get { return m_canSplitData; } set { m_canSplitData = value; } }

        private void Init(string path, long maxFileLength, int maxFileCount, FileMode mode)
        {
            if (maxFileLength <= 0)
                throw new ArgumentOutOfRangeException("Invalid maximum file length");
            if (maxFileCount <= 0)
                throw new ArgumentOutOfRangeException("Invalid maximum file count");

            m_maxFileLength = maxFileLength;
            m_maxFileCount = maxFileCount;
            m_canSplitData = true;

            string fullPath = Path.GetFullPath(path);
            m_fileDir = Path.GetDirectoryName(fullPath);
            m_fileBase = Path.GetFileNameWithoutExtension(fullPath);
            m_fileExt = Path.GetExtension(fullPath);

            m_fileDecimals = 1;
            int decimalBase = 10;
            while (decimalBase < m_maxFileCount)
            {
                ++m_fileDecimals;
                decimalBase *= 10;
            }

            switch (mode)
            {
                case FileMode.Create:
                case FileMode.CreateNew:
                case FileMode.Truncate:
                    // Delete old files
                    for (int iFile = 0; iFile < m_maxFileCount; ++iFile)
                    {
                        string file = GetBackupFileName(iFile);
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    break;

                default:
                    // Position file pointer to the last backup file
                    for (int iFile = 0; iFile < m_maxFileCount; ++iFile)
                    {
                        if (File.Exists(GetBackupFileName(iFile)))
                            m_nextFileIndex = iFile + 1;
                    }
                    if (m_nextFileIndex == m_maxFileCount)
                        m_nextFileIndex = 0;
                    Seek(0, SeekOrigin.End);
                    break;
            }
        }

        private void BackupAndResetStream()
        {
            Flush();
            File.Copy(Name, GetBackupFileName(m_nextFileIndex), true);
            SetLength(0);

            ++m_nextFileIndex;
            if (m_nextFileIndex >= m_maxFileCount)
                m_nextFileIndex = 0;
        }

        private string GetBackupFileName(int index)
        {
            StringBuilder format = new StringBuilder();
            format.AppendFormat("D{0}", m_fileDecimals);
            StringBuilder sb = new StringBuilder();
            if (m_fileExt.Length > 0)
                sb.AppendFormat("{0}{1}{2}", m_fileBase, index.ToString(format.ToString()), m_fileExt);
            else
                sb.AppendFormat("{0}{1}", m_fileBase, index.ToString(format.ToString()));
            return Path.Combine(m_fileDir, sb.ToString());
        }

        private static FileMode BaseFileMode(FileMode mode)
        {
            return mode == FileMode.Append ? FileMode.OpenOrCreate : mode;
        }

        private long m_maxFileLength;
        private int m_maxFileCount;
        private string m_fileDir;
        private string m_fileBase;
        private string m_fileExt;
        private int m_fileDecimals;
        private bool m_canSplitData;
        private int m_nextFileIndex;
    }


    public class TextWriterTraceListenerWithTime : TextWriterTraceListener
    {
        public TextWriterTraceListenerWithTime()
            : base()
        {
        }

        public TextWriterTraceListenerWithTime(Stream stream)
            : base(stream)
        {
        }

        public TextWriterTraceListenerWithTime(string path)
            : base(path)
        {
        }

        public TextWriterTraceListenerWithTime(TextWriter writer)
            : base(writer)
        {
        }

        public TextWriterTraceListenerWithTime(Stream stream, string name)
            : base(stream, name)
        {
        }

        public TextWriterTraceListenerWithTime(string path, string name)
            : base(path, name)
        {
        }

        public TextWriterTraceListenerWithTime(TextWriter writer, string name)
            : base(writer, name)
        {
        }

        public override void WriteLine(string message)
        {
            base.Write(DateTime.Now.ToString());
            base.Write(" ");
            base.WriteLine(message);
        }
    }
}
