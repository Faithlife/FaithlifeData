namespace Faithlife.Data
{
	internal enum DbValueTypeStrategy
	{
		CastValue,
		DtoProperties,
		ByteArray,
		Tuple,
		Enum,
		Dynamic,
	}
}
