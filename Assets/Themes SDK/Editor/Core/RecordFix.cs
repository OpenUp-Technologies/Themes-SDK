using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    // this is needed to enable the record feature in .NET framework and .NET core <= 3.1 projects
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
