using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Namespace
{
    class DataSection
    {
        public int SectionNumber { get; set; }
        public int SectionSize { get; set; }
        public byte[] SectionData { get; set; }

        public DataSection(byte[] byteArray, int index)
        {
            SectionNumber = ByteConverter.ConvertIntBytes(byteArray[index..(index + 4)]);
            SectionSize = ByteConverter.ConvertIntBytes(byteArray[(index + 4)..(index + 8)]);

            int sectionStart = index + 8;
            int sectionEnd = index + 8 + SectionSize;

            SectionData = byteArray[sectionStart..sectionEnd];
        }
    }
}