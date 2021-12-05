using System;
using System.IO;
using DynamicParser;

namespace DynamicMosaicExample
{
    internal sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        protected override Processor GetAddingProcessor(string fullPath) => ImageRect.GetProcessor(LoadBitmap(fullPath), GetProcessorTag(fullPath));

        protected override string GetProcessorTag(string fullPath) => Path.GetFileNameWithoutExtension(fullPath);
    }
}
