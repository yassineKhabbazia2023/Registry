// <copyright file="JsonStreamHelper.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Helpers
{
    public static class JsonStreamHelper
    {
        public static async Task<Stream> CreateJsonStreamAsync(Func<StreamWriter, Task> writeJsonFunc)
        {
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream) { AutoFlush = true };

            await writeJsonFunc(streamWriter);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
