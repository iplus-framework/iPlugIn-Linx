using System.Reflection;

namespace linx.core.reporthandlerwpf
{
    public class MappingAttributeWrapper
    {
        public PropertyInfo PropertyInfo { get; set; }
        public LinxByteMappingAttribute LinxByteMapping { get; set; }
    }
}
