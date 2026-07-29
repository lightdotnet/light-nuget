# Lightsoft.Extensions

`Light.Extensions` namespace. Targets **netstandard2.1** (unlike the rest of the `Framework` solution, which targets `net10.0`) — this is intentional, to keep the package usable by older/broader .NET runtimes. It has no `ProjectReference`s to other projects in this repo; it only depends on `System.ComponentModel.Annotations` and `System.Text.Json` (see `Extensions.csproj`).

A grab-bag of static helper/extension classes for common data-shaping tasks: argument guards, date/time checks, random generation, string/number conversion, reflection-based object helpers, JSON (including Unix-timestamp converters), and small utilities for streams, XML, query strings, and enums.

> **Public API changes (this session):** several methods below were renamed as breaking changes, and a few had incorrect behavior fixed. If you're upgrading, check the "Renamed" callouts under each class.

## Guards / Validation

### `ArgumentChecker`
- `string ThrowIfNull(this string input, string? paramName = null, string? message = null)` — throws `ArgumentNullException` if the string is null, empty, or whitespace; otherwise returns it.
- `T ThrowIfNull<T>(this T input, string? paramName = null, string? message = null)` — throws `ArgumentNullException` if `input` is `null` **or** equal to `default(T)` (via `EqualityComparer<T>.Default.Equals(input, default!)`). This now correctly catches default-valued value types (e.g. `0`, `Guid.Empty`, default structs) too, not just reference-type nulls.

```csharp
int id = requestId.ThrowIfNull(nameof(requestId)); // throws if requestId == 0
```

## Date & Time

### `DateTimeHelper`
- `IsNearlyInMinutes/Seconds/Hours/Days(this DateTime dateTime, int amount)` — true only when `dateTime` is **in the future** and within `amount` units from now (`diff >= 0 && diff <= amount`, where `diff = dateTime - DateTime.Now`). A `dateTime` in the past now correctly returns `false` (previously any past `DateTime` returned `true`, which was a bug).
- `ToUnixTimeSeconds()` / `ToUnixTimeMilliseconds()` — `DateTime` → Unix timestamp.
- `GetDateTimeFromSeconds(long)` / `GetDateTimeFromMilliseconds(long)` — Unix timestamp → `DateTime`.

### `Month`
- `Month.ByDate(DateTime date)` — readonly struct exposing `FirstDay`, `LastDay` (last tick of the month), and `TotalDays` for the month containing `date`.

### `Light.Extensions.Json` — Unix timestamp JSON converters
- `UnixSecondsToDateTimeConverter` / `UnixMilliSecondsToDateTimeConverter` (`System.Text.Json.Serialization.JsonConverter<DateTime>`), applied via `[UnixSecondsDateTime]` / `[UnixMilliSecondsDateTime]` attributes. **Asymmetric behavior**: on read, they parse a numeric Unix timestamp (seconds/ms since `DateTime.UnixEpoch`) into a `DateTime`; on write, they call `WriteStringValue(DateTime)`, i.e. they serialize back out as an ISO-8601 string, not as a Unix number.

```csharp
public class Event
{
    [UnixSecondsDateTime]
    public DateTime CreatedAt { get; set; }
}
```

## Random

### `RandomHelper`
- `GenerateString(int length)` — random alphanumeric string.
- `GenerateNumber(int length)` — random numeric string (digits only, may contain leading zeros).

Renamed from `String`/`Number`. Both now draw from a single shared `Random` instance guarded by a lock, instead of allocating a new time-seeded `Random` per call (which could produce identical sequences when called in quick succession). Note: netstandard2.1 has no `Random.Shared` (added in .NET 6), hence the manual lock.

## Data Conversion

### `DataConverter`
- `ToArray(this string? value, string splitChar = "|")` / `ToList(this string? value, string splitChar = "|")` — split a delimited string.
- `JoinToString(this IList<string> / IEnumerable<string> values, string splitChar = "|")` — join back into a delimited string.
- `SplitLines(this string value)` → `string[]` / `ToLines(this string value)` → `List<string>` — split multi-line text (e.g. an HTML `<textarea>` value) on `\r\n`/`\n`. Renamed from `AreaTextToArray` / `AreaTextToList`.
- `ToBase(this long value, int toBase)`, plus `ToBase2/8/10/16()` shortcuts — number → string in the given base.
- `ToInt64(this string baseString, int fromBase = 16)` — parses a number string in the given base (2, 8, 10, 16) back to `long`; returns `0` for null/empty input. Renamed from `ToInt64FromBinary` (the old name was misleading since it took an arbitrary base, defaulting to 16, not specifically binary).

```csharp
"1F".ToInt64();       // fromBase defaults to 16 -> 31
"1F".ToInt64(16);     // same, explicit
255L.ToBase16();      // "ff"
```

### `Base64StringHelper`
- Instance class with `string Decode(string base64EncodedData)` — Base64 → UTF-8 string. (No corresponding `Encode` method exists here.)

### `StreamHelper`
- `ToBase64String(this Stream stream)` — reads the stream fully and Base64-encodes it.
- `FromBase64String(string base64String)` → `MemoryStream` — decodes Base64 into a stream.

### `DataTableHelper`
- `ConvertToType<T>(DataTable dt)` — maps each `DataRow` to a new `T` by matching column names to property names (via reflection; requires a public parameterless constructor).
- `Load<T>(IList<T> values, params Type[] excludeTypes)` — builds a `DataTable` from a list of `T`, one column per public property; properties whose *type* is in `excludeTypes` are skipped.
- `ConvertToObjects(DataTable dt)` — rows → `List<Dictionary<string, object>>`, with special column-name suffix conventions: a column named e.g. `"Age(int)"` is converted with `Convert.ToInt64` and stored under `"Age"`; `"(date)"` → `Convert.ToDateTime`; `"(bool)"` → `Convert.ToBoolean`.

## String / Text

### `StringHelper`
- `Left(this string value, int length)` / `Right(this string value, int length)` — first/last N characters (length is `Math.Abs`'d; returns the whole string if shorter than `length`).
- `Left(this string value, string c)` / `Right(this string value, string c)` — substring up to the first/after the last occurrence of `c`; returns the original string unchanged if `c` is not found.

### `TextHelper`
- `ConvertToUnSign3(string s)` — strips diacritics (e.g. `"Tiếng Việt"` → `"Tieng Viet"`), including special-cased Đ/đ.

### `RegexExtensions`
- `RemoveEmoji(this string text)` — strips Unicode surrogate (`\p{Cs}`) characters, which covers most emoji.

## Object / Reflection

### `ObjectHelper`
- `GetPropertyName<T>(Expression<Func<T, object?>> expr)` — extracts a member name from a lambda, e.g. `ObjectHelper.GetPropertyName<Order>(o => o.Id)` → `"Id"`.
- `GetValues(this object obj)` — non-null property name/value pairs via `TypeDescriptor`.
- `GetStaticValues(this Type typeOfObj)` — public static field name/value pairs.
- `IsListOfT(this object obj)` — true only for concrete `List<T>` instances. Renamed from `IsList`; the doc comment now explicitly clarifies it does **not** match other `IList` implementors such as arrays or custom collections.
- `IsDictionary(this object obj)` — analogous check for concrete `Dictionary<TKey, TValue>`.

### `AttributeExtensions`
- `GetAttribute<T>(this MemberInfo memberInfo)` — first custom attribute of type `T`, or `null`.
- `GetDisplayName`, `GetDescription` — `[DisplayName]` / `[Description]` attribute values.
- `GetNameOfDisplay`, `GetDescriptionOfDisplay` — `Name`/`Description` from `[Display]`.

### `EnumHelper`
- `GetDescription(this Enum)`, `GetNameOfDisplay(this Enum)`, `GetDescriptionOfDisplay(this Enum)` — reads `[Description]`/`[Display]` off the matching enum field.
- `GetOptions<T>()` — returns `IEnumerable<EnumData>` (`Value`, `StringValue`, `Description`) for every value of enum `T`.
- `GetAll<T>()` — all values of enum `T` as `IEnumerable<T>`.

### `ValueHandler`
- `MaximumCharHandler<T>(this T data, int length)` — truncates every non-empty `string` property of `data` to `length` chars (via `StringHelper.Left`), mutating and returning `data`.
- `NullDateTimeHandler<T>(this T data, DateTime? defaultTime = null)` — sets every `DateTime` property that is `<= 1753-01-01` (SQL Server's minimum date) to `defaultTime` (defaults to `1753-01-01`).
- `NullStringHandler<T>(this T data)` — sets every null/empty `string` property to `string.Empty`.

All three now cache each type's `Type.GetProperties()` result in a `ConcurrentDictionary<Type, PropertyInfo[]>` instead of re-reflecting on every call.

```csharp
var dto = repository.Get(id)
    .NullStringHandler()
    .NullDateTimeHandler()
    .MaximumCharHandler(255);
```

Note: these three methods mutate via reflection and assume `T` has settable properties (`pi.SetValue`); they will throw for read-only properties or records with init-only setters.

## JSON

### `JsonHelper`
Uses a shared `JsonSerializerOptions` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.
- `Serialize<T>(T obj)` / `Deserialize<T>(string json)`.
- `ConvertToBase64<T>(T obj)` / `ReadFromBase64As<T>(string value)` — JSON serialized as UTF-8 then Base64-encoded, and back.

## Claims

### `ClaimExtensions`
- `Add(this List<Claim> claims, string key, string? value)` — appends a `Claim` only if `value` is non-empty; returns `claims` for chaining. `FindFirstValue` was removed (it was dead code, always shadowed by the BCL's own `ClaimsPrincipal.FindFirstValue`).

## Web / Misc

### `UriQueryBuilder`
- `ToQueryString<T>(T data)` — builds a `key=value&...` query string (URL-encoded) from an object's public properties, skipping `null` values.
- `ToQueryString(Dictionary<string, string> queryParams)` / `ToQueryString(Dictionary<string, object> queryParams)` — same, from a dictionary.

### `XmlHelper`
- `LoadXml(string xml)` → `XmlDocument`.
- `GetFirstElementByTagName(this XmlDocument xmlDocument, string tagName)` — returns the first matching node (throws `IndexOutOfRangeException`-equivalent if none found, since it indexes `[0]` directly).

### `Scheduler`
Computes the next run time for a recurring job confined to a daily time window.
- `new Scheduler(int runEveryMins)` — interval between runs; throws `ArgumentOutOfRangeException` if `<= 0`.
- `StartTime` / `EndTime` (`TimeSpan`, default `00:00:00`–`23:59:59`) — validated so `StartTime < EndTime` and both stay within a single day.
- `NextTime()` — from `DateTime.Now`: if before `StartTime`, returns today's `StartTime`; if after `EndTime`, returns tomorrow's `StartTime`; otherwise returns `now + RunEveryMins`, rolling over to tomorrow's `StartTime` if that would land outside the window.

## Attributes

- `Light.Extensions.Attributes.ValidatedNotNullAttribute` — parameter-only marker attribute used internally by `ArgumentChecker` so static analyzers understand `ThrowIfNull` validates its input for null.
