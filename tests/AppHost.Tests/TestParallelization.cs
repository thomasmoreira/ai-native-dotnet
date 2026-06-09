// The fake-provider tests and the real-embedding eval each start their own distributed app.
// Running them in parallel would race on the services' launch-profile ports, so serialize.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
