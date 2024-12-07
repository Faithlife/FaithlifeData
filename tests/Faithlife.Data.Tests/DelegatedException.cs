using System.Diagnostics.CodeAnalysis;

namespace Faithlife.Data.Tests;

[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "For testing.")]
internal sealed class DelegatedException : Exception
{
}
