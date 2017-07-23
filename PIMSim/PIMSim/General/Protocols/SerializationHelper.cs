using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace PIMSim.General.Protocols
{
    public static class SerializationHelper
    {
        public static byte[] SerializeObject(object obj)
        {
            if (obj == null)
                return null;

            MemoryStream ms = new MemoryStream();

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;
            byte[] bytes = ms.GetBuffer();
            ms.Read(bytes, 0, bytes.Length);
            ms.Close();
            return bytes;
        }


        public static object DeserializeObject(byte[] bytes)
        {
            object obj = null;
            if (bytes == null)
                return obj;
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            BinaryFormatter formatter = new BinaryFormatter();
            obj = formatter.Deserialize(ms);
            ms.Close();
            return obj;
        }
    }
}
