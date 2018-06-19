# efcoreextensions
support for OPENJSON sql server syntax with linq to entities through EFCore

# Context
- Your project use EF Core (2.1.1).
- You need some JSON features of SQL Server (JSON_VALUE, JSON_QUERY, OPENJSON).
- You don't want use FromSql
You can use this extension library with a new extension method : ValueFromOpenJson. 

# Configuration
You have to declare some fake DbSet (due to actual limitation of EFCore) :
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<JsonResult<string>>();
    modelBuilder.Entity<JsonResult<int>>();
    modelBuilder.Entity<JsonResult<bool>>();
}
```
You can use SQLServer or InMemory provider :
```csharp
optionsBuilder.UseExtensions(extensions =>
{
    //extensions.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=testdbapp2;Trusted_Connection=True;");
    extensions.UseInMemoryDatabase("inmemory");
});
```

# Usages
If you want EF Core generate valid SQL with right usage of OPENJSON, you have to use the method ValueFromOpenJson on a fake DbSet of JsonResult. You have to declare this DbSet as a variable :
```csharp
var json = appContext.Set<JsonResult<string>>();
```
Next you can query a real DbSet (mapped to an existing table) :
```csharp
var query = appContext.Persons
    .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(jr => jr.Value).Contains("kind2"));
```
The variable query is a classic IQueryable, you trigger the SQL Command via ToList or ToListAsync from EFCore :
```csharp
var result = await query.ToListAsync();
```
The SQL generated :
```SQL
SELECT [p].[Id], [p].[Kinds], [p].[Name]
FROM [Persons] AS [p]
WHERE N'kind2' IN (
    SELECT [jr].[Value]
    FROM (
        SELECT [Key], [Value], [Type] FROM OPENJSON([p].[Kinds], N'$')
    ) AS [jr]
)
```

# TODOs
- [ ] declare JsonResult DbSet automaticaly
- [ ] SQLite support
