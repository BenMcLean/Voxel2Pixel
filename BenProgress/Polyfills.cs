namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class RequiresLockAttribute(string lockName) : Attribute
{
	public string LockName { get; } = lockName;
}
