using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    // this is needed to enable the record feature in .NET framework and .NET core <= 3.1 projects
    // (as in "public record Thing(int field);", not the recording feature)
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit { }
}
