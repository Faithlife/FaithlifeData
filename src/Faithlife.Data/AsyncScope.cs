using System;
using System.Threading.Tasks;

namespace Faithlife.Data
{
	internal readonly struct AsyncScope : IAsyncDisposable
	{
		public AsyncScope(IDisposable disposable)
		{
			m_disposable = disposable;
		}

		public ValueTask DisposeAsync()
		{
			if (m_disposable is IAsyncDisposable asyncDisposable)
				return asyncDisposable.DisposeAsync();

			m_disposable.Dispose();
			return default;
		}

		private readonly IDisposable m_disposable;
	}
}
