using System.ComponentModel.DataAnnotations;

namespace EFCore.Extensions
{
    public enum JsonType
    {
        Null = (byte)0,
        String = (byte)1,
        Number = (byte)2,
        Boolean = (byte)3,
        Array = (byte)4,
        Object = (byte)5
    }

    public class JsonResult<T>
    {
        [Key]
        public string Key { get; set; }
        public T Value { get; set; }
        public JsonType Type { get; set; }
    }
}
