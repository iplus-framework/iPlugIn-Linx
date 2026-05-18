using System.Reflection;

namespace linx.core.reporthandler
{
    public class MappingAttributeWrapper
    {
        public PropertyInfo PropertyInfo { get; set; }
        public LinxByteMappingAttribute LinxByteMapping { get; set; }
    }
}
