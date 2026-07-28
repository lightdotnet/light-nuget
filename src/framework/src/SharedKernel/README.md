# Lightsoft.SharedKernel

Root namespace: `Light` · Package/assembly: `Lightsoft.SharedKernel` · Target: `net10.0`

Small, dependency-light building blocks meant to be reused across independent downstream
solutions: DDD-ish entity/value-object base types, a string ID generator, a set of typed
HTTP-mappable exceptions, and a helper for mapping dynamic (EAV-style) columns onto POCOs.

Only dependency: [`Ulid`](https://www.nuget.org/packages/Ulid) (used by `LightId`).

Within this solution, `WebHost` is the only project that references `SharedKernel`.

## Contents

### Domain building blocks (`Light.Domain`, `Light.Domain.Entities`, `Light.Domain.ValueObjects`)

- **`LightId`** — static helper that generates string IDs from a ULID.

  ```csharp
  string id = LightId.NewId();
  ```

- **`IEntity` / `IEntity<TKey>`** — marker interfaces; `IEntity<TKey>` exposes `TKey Id`.
- **`IEvent`** — contract for domain-event bookkeeping (`DomainEvents`, `AddDomainEvent`,
  `RemoveDomainEvent`, `ClearDomainEvents`).
- **`BaseEvent`** — abstract record base for domain events; sets `TriggeredOn` (UTC) on creation.
- **`BaseEntity`** — abstract base implementing `IEntity` + `IEvent`; holds the domain-event list.
- **`BaseEntity<TId>`** — adds a settable `TId Id` property.
- **`Entity`** — `BaseEntity<string>` that auto-assigns `Id = LightId.NewId()` in its constructor.
  This is the base most entities should derive from unless a non-string key is required.

  ```csharp
  public class Product : Entity
  {
      public string Name { get; set; } = string.Empty;
  }
  ```

- **`BaseAuditableEntity` / `BaseAuditableEntity<TId>`** — adds `Created`, `CreatedBy`,
  `LastModified`, `LastModifiedBy` (implements `IAuditable`).
- **`AuditableEntity`** — `BaseAuditableEntity<string>` with the same auto-generated `LightId`
  behavior as `Entity`.
- Audit/tenant/soft-delete interfaces available for opt-in composition on your own entities:
  `IAuditable`, `IHasAuditTime`, `IHasCreationTime`, `IHasModificationTime`, `IHasAuditUser`,
  `ISoftDelete`, `ITenant`.
- **`ValueObject`** (`Light.Domain.ValueObjects`) — abstract base implementing structural
  equality via an overridden `GetEqualityComponents()`; also provides `EqualOperator`/
  `NotEqualOperator` helpers for use in derived `==`/`!=` operators.
- **`IgnoreMemberAttribute`** — marker attribute for value-object members, for consumers who
  want to flag properties/fields that should be excluded from equality comparisons.

### Contracts (`Light.Contracts`)

- **`IAggregateRoot`** — marker interface. Intended so repositories only operate on aggregate
  roots, not on child entities.

### Exceptions (`Light.Exceptions`)

All exceptions derive from `ExceptionBase`, which carries an `HttpStatusCode` alongside the
`Exception.Message` — useful for a global exception handler that maps these to HTTP responses.

| Exception | HTTP status |
|---|---|
| `ValidationException` | 400 Bad Request (also carries `IDictionary<string, string[]> ValidationErrors`) |
| `UnauthorizedException` | 401 Unauthorized |
| `ForbiddenException` | 403 Forbidden |
| `NotFoundException` | 404 Not Found |
| `ConflictException` | 409 Conflict |
| `InternalServerErrorException` | 500 Internal Server Error |

```csharp
throw new NotFoundException(id, nameof(Product));
// or
throw new ValidationException(new Dictionary<string, string[]>
{
    ["Name"] = ["Name is required."]
});
```

### Dynamic object mapping (`Light.Extensions.DynamicObject`)

For EAV-style storage where an object's properties are persisted as rows (object name / property
name / property type / property value) rather than columns.

- **`DynamicEntity`** — abstract base (derives from `Entity`, implements `IHasAuditTime`) with
  `ObjectName`, `PropName`, `PropType`, `PropValue`, `Created`, `LastModified`.
- **`DynamicColumnExporter.ConvertToDynamicColumns<T, TEntity>(obj, objectName)`** — reflects
  over `T`'s properties and produces a `List<TEntity>` of `DynamicEntity` rows (`PropValue` is
  stored as `.ToString()`, `PropType` as a coarse string tag such as `"int"`, `"datetime"`, etc.).
- **`DynamicMapper.MapToObject<T, TEntity>(columns)`** — the inverse: takes a `List<TEntity>` of
  `DynamicEntity` rows and populates a new `T` by matching `PropName` to `T`'s writable
  properties, parsing `PropValue` back into the property's CLR type.

  ```csharp
  var rows = DynamicColumnExporter.ConvertToDynamicColumns<Product, ProductColumn>(product, "Product");
  // ... persist rows ...
  var rebuilt = DynamicMapper.MapToObject<Product, ProductColumn>(rows);
  ```

- **`IDynamicEntityRepository<T>`** — repository contract (`Get(objectName)` /
  `Update(objectName, value)`) for consumers implementing storage of the above.

## Notes

- **`LightId` API shape change**: `LightId` is a `static class` (not a struct/value type). If any
  changelog or older doc still refers to it as a struct, that's stale — treat the static-class
  shape as current.
- **`DynamicMapper` conversion behavior**: `DynamicMapper.MapToObject` no longer silently
  swallows type-conversion failures. `ConvertToType` calls `int.Parse`, `bool.Parse`,
  `DateTime.Parse`, etc. (or falls back to `Convert.ChangeType`) directly, with no try/catch —
  a malformed `PropValue` for a given `PropType` will now throw (e.g. `FormatException`) instead
  of being silently dropped. Callers that previously relied on bad values being ignored need to
  handle/validate before calling this.
