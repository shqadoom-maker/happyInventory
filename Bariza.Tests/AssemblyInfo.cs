using Xunit;

// Integration test factories configure the process-wide environment before the
// API host is built. Run test collections sequentially so two factories cannot
// overwrite each other's configuration.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
