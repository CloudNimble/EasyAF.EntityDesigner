// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Microsoft.Data.Entity.Design.EntityDesigner.Utils
{
    internal static class FileUtils
    {
        // NOTE: Returned Stream should be disposed when done!
        public static Stream StringToStream(string inputContents, Encoding encoding)
        {
            //MDF Serialization requires the preamble
            var preamble = encoding.GetPreamble();
            var body = encoding.GetBytes(inputContents);

            MemoryStream stream = new MemoryStream(preamble.Length + body.Length);

            stream.Write(preamble, 0, preamble.Length);
            stream.Write(body, 0, body.Length);
            stream.Position = 0;

            return stream;
        }
    }
}
