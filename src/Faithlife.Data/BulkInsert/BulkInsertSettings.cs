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
		/// <remarks>If neither <see cref="MaxParametersPerBatch"/> nor <see cref="MaxRecordsPerBatch"/> are specified,
		/// the maximum number of parameters is 999.</remarks>
		public int? MaxParametersPerBatch { get; set; }

		/// <summary>
		/// Specifies the maximum number of records to insert per batch.
		/// </summary>
		/// <remarks>If neither <see cref="MaxParametersPerBatch"/> nor <see cref="MaxRecordsPerBatch"/> are specified,
		/// the maximum number of parameters is 999.</remarks>
		public int? MaxRecordsPerBatch { get; set; }
	}
}
