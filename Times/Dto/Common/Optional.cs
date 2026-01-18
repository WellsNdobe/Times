namespace Times.Dto.Common
{
	// Represents a JSON field that may be omitted entirely.
	public sealed class Optional<T>
	{
		public bool IsProvided { get; set; }
		public T? Value { get; set; }

		public static Optional<T> NotProvided() => new Optional<T> { IsProvided = false };
		public static Optional<T> Provided(T? value) => new Optional<T> { IsProvided = true, Value = value };
	}
}
