using Durandal.Common.File;
using Durandal.Common.IO;
using Durandal.Common.Utils;
using Durandal.Extensions.Compression.Brotli;

namespace ScriptureGraph.Core.Training
{
    public class WebPageCache
    {
        private VirtualPath _cacheDir;
        private IFileSystem _fileSystem;

        public WebPageCache(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _cacheDir = VirtualPath.Root;

            // Temp: Compress all existing files
            //foreach (VirtualPath file in fileSystem.ListFiles(_cacheDir))
            //{
            //    if (!string.Equals(".br", file.Extension))
            //    {
            //        VirtualPath outputFile = new VirtualPath(file.FullName + ".br");
            //        using (Stream fileIn = _fileSystem.OpenStream(file, FileOpenMode.Open, FileAccessMode.Read))
            //        using (Stream fileOut = _fileSystem.OpenStream(outputFile, FileOpenMode.CreateNew, FileAccessMode.Write))
            //        using (BrotliCompressorStream brotliCompressor = new BrotliCompressorStream(fileOut))
            //        {
            //            fileIn.CopyToPooled(brotliCompressor);
            //        }
            //    }
            //}
        }

        public async Task<string?> GetCachedWebpageIfExists(Uri pageUrl)
        {
            VirtualPath targetFile = ConvertUrlToFileName(pageUrl);
            if (!(await _fileSystem.ExistsAsync(targetFile)))
            {
                return null;
            }

            using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
            using (StringBuilderTextWriter writer = new StringBuilderTextWriter(pooledSb.Builder))
            using (Stream fileIn = await _fileSystem.OpenStreamAsync(targetFile, FileOpenMode.Open, FileAccessMode.Read))
            using (BrotliDecompressorStream brotliDecompressor = new BrotliDecompressorStream(fileIn))
            using (Utf8StreamReader reader = new Utf8StreamReader(brotliDecompressor))
            using (PooledBuffer<char> scratch = BufferPool<char>.Rent())
            {
                while (!reader.EndOfStream)
                {
                    int charsRead = reader.Read(scratch.Buffer, 0, scratch.Buffer.Length);
                    if (charsRead > 0)
                    {
                        writer.Write(scratch.Buffer, 0, charsRead);
                    }
                    else
                    {
                        break;
                    }
                }

                return pooledSb.Builder.ToString();
            }
        }

        public async Task StorePage(Uri pageUri, string pageContents)
        {
            VirtualPath targetFile = ConvertUrlToFileName(pageUri);
            using (Stream fileOut = await _fileSystem.OpenStreamAsync(targetFile, FileOpenMode.CreateNew, FileAccessMode.Write))
            using (BrotliCompressorStream brotliCompressor = new BrotliCompressorStream(fileOut)) 
            using (Utf8StreamWriter writer = new Utf8StreamWriter(brotliCompressor))
            {
                writer.Write(pageContents);
            }
        }

        private VirtualPath ConvertUrlToFileName(Uri pageUri)
        {
            using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
            {
                pooledSb.Builder.Append(pageUri.AbsoluteUri);
                pooledSb.Builder.Replace('&', '_');
                foreach (char invalidFileChar in Path.GetInvalidFileNameChars())
                {
                    pooledSb.Builder.Replace(invalidFileChar, '_');
                }

                pooledSb.Builder.Append(".html.br");
                return _cacheDir.Combine(pooledSb.Builder.ToString());
            }
        }
    }
}
