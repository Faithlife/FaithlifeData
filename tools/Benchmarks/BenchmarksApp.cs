using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace Benchmarks;

internal sealed class BenchmarksApp
{
	public static void Main(string[] args)
	{
		var config = DefaultConfig.Instance.WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared));
		var switcher = BenchmarkSwitcher.FromAssembly(typeof(BenchmarksApp).Assembly);
		if (args.Length == 0)
			switcher.RunAllJoined(config);
		else
			switcher.Run(args, config);
	}
}
