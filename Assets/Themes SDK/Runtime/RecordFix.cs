using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    // This is needed to enable the record feature in .NET framework and .NET core <= 3.1 projects
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit { }
}
