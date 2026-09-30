// Integration tests share a single WebApplicationFactory + LocalDB; class-level parallelism.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
