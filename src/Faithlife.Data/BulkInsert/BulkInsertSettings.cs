namespace Faithlife.Data.BulkInsert
{
	/// <summary>
	/// Settings for bulk insert.
	/// </summary>
	public sealed class BulkInsertSettings
	{
		/// <summary>
		/// Specifies the maximum number of parameters to use per batch.
		/// </summary>
		/// <remarks>If neither <see cref="MaxParametersPerBatch"/> nor <see cref="MaxRowsPerBatch"/> are specified,
		/// the maximum number of parameters is 999.</remarks>
		public int? MaxParametersPerBatch { get; set; }

		/// <summary>
		/// Specifies the maximum number of rows to insert per batch.
		/// </summary>
		/// <remarks>If neither <see cref="MaxParametersPerBatch"/> nor <see cref="MaxRowsPerBatch"/> are specified,
		/// the maximum number of parameters is 999.</remarks>
		public int? MaxRowsPerBatch { get; set; }
	}
}
