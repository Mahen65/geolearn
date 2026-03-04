# 07 — Fix keyless entity — add ToView(null)

## Problem
After adding `WorkObjectRow` as a keyless entity (`HasNoKey()`), EF Core 10 still
generated a migration that created a `WorkObjectRows` table.

**Root cause:** `HasNoKey()` alone does not suppress table creation. EF Core
infers a default table name from the `DbSet` property name (`WorkObjectRows`) and
includes the entity in migrations. A backing table is created even though the
entity has no PK and is only meant for raw SQL result mapping.

**Wrong migration deleted:** `20260226054511_AddWorkObjectRowKeylessEntity`

## Fix
Add `.ToView(null)` after `HasNoKey()`:

```csharp
modelBuilder.Entity<WorkObjectRow>(entity =>
{
    entity.HasNoKey();
    entity.ToView(null); // ← no backing store; excluded from migrations
    // column mappings...
});
```

`ToView(null)` tells EF Core: "this entity is not backed by any table or view —
never include it in migrations." The `DbSet<WorkObjectRow>` still works with
`FromSqlRaw` / `FromSqlInterpolated` because those bypass the backing-store check.

## Rule learned
For a `FromSqlRaw` result type registered as a keyless entity:
```csharp
entity.HasNoKey();   // no PK, no change tracking
entity.ToView(null); // no backing store, no migration
```
Both lines are required.
