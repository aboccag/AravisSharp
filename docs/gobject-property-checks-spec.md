# Spec: reject unknown or mistyped GObject properties, read back writes

Issue #14, level complet (zone `interop`), type changement. Written on 2026-11-16 against
`main` at `e1341e8`, before any code. Source material: the branch
`fix/native-leak-and-stream-pop` (not merged, older base), its
`AravisSharp/Native/GLibNative.cs`, `AravisSharp.Tests/GioTestNative.cs` and
`docs/gobject-properties-spec.md` (addenda A, B, C of 14.11.2026). `NON VÉRIFIÉ` marks what
was not checked.

## Premises checked on `e1341e8`

- `GLibNative` has six **public** helpers over the non-variadic GValue path:
  `SetIntProperty`, `SetUIntProperty`, `SetEnumProperty`, `GetIntProperty`,
  `GetUIntProperty`, `GetEnumProperty` (`AravisSharp/Native/GLibNative.cs:140-202`). None
  checks that the property exists or has the helper's type, and no setter checks that the
  write was applied. GLib then logs a warning; a setter does nothing and a getter returns 0.
- They were added after v0.8.36 (commit `128d025`) and were never released. v0.8.36 shipped
  six public variadic imports instead, `g_object_set_int/uint/bool` and
  `g_object_get_int/uint/bool` (`git show v0.8.36:AravisSharp/Native/GLibNative.cs:97-132`).
  `main` removed them: an assembly built against v0.8.36 that calls one of them gets
  `MissingMethodException` on the next release.
- The only callers are the eleven GigE members of `Stream` (`AravisSharp/Stream.cs:70-196`),
  each behind `CheckGigE()`. Their property types match the helpers they use
  (`aravis/src/arvgvstream.c:1824-1910`: `socket-buffer` and `packet-resend` enums,
  `socket-buffer-size` int, the three timeouts uint). No test, example or smoke project
  calls the helpers.
- `GLibNative` declares no `g_type_class_peek`, `g_object_class_find_property` or
  `g_value_type_compatible` today.
- GIO objects need no camera: `GSocketClient` has `timeout` (guint), `family` (enum) and
  `tls` (gboolean); `GSocket` has `listen-backlog` (gint with a declared range). The old
  branch's tests used them through `GioTestNative.cs`.

## Definitions

*Property check*: before the GValue call, look up the property on the instance's class with
`g_object_class_find_property` and compare its value type with the helper's
(`g_value_type_compatible`, with an explicit path for enum properties). *Read-back*: after a
write, read the property again and compare it with the value written.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | Every setter and getter throws `ArgumentException` naming the type and the property when the object has no such property. | A GLib warning followed by a silent no-op or a 0. |
| AC-2 | Every setter and getter throws `ArgumentException` when the property's value type does not match the helper (a uint helper on a gint property, an int helper on a guint property, an enum helper with another enum type). | An implicit conversion between int and uint. |
| AC-3 | A setter whose value GLib refuses (outside the declared range) throws `ArgumentOutOfRangeException` after the read-back; the property keeps its previous value. | A refused write reported as success. |
| AC-4 | Round trips on GIO objects for int, uint and enum (and bool if question 3 adds it) pass with no GLib warning. | A test that needs a camera. |
| AC-5 | The lookup uses GLib functions only; no managed mirror of `GParamSpec` or `GTypeClass`. | A struct layout copied from a GLib header. |
| AC-6 | The GigE members of `Stream` behave as today on a GigE stream (the simulated GigE camera tests pass) and still throw `InvalidOperationException` on a non-GigE stream. | A GigE member that now throws on a valid value. |
| AC-7 | Each new check is shown red first: the test fails against `main`'s helpers, then passes. | — |
| AC-8 | The whole suite passes on the Fake camera on net8.0 and net10.0, Debug and Release. | — |

Known gap, as ruled on 14.11.2026 (old addendum B.1): readable and writable flags are not
checked (GLib has no accessor for them); a refused write to a read-only property is caught
by the read-back. A getter on a write-only property gets GLib's "not readable" message and
returns the GValue's zero; a setter on a write-only property reads it back the same way, so
GLib logs "not readable" and the setter throws `ArgumentOutOfRangeException` even when the
write was applied (see the note of 16.11.2026 below).

**16.11.2026, review.** The previous wording ("a read of a write-only property stays
silent") was wrong for writes: the read-back after a write to a write-only property logs a
GLib "not readable" message and throws `ArgumentOutOfRangeException`, though the write may
have been applied. Documented in the XML docs of the setters; the code is unchanged (no
`Stream` property is write-only, `aravis/src/arvgvstream.c`).

## Risks

- Behaviour change for callers of the helpers: silent today, throwing tomorrow. No released
  caller exists (premise 2), only `Stream`, whose types match.
- One extra property lookup per call, and one extra read per write: negligible next to a
  GigE stream setting (`NON VÉRIFIÉ`, no budget).
- `g_type_class_peek` needs the class to be referenced; it is, while an instance is alive.

## Open questions

1. **Visibility of the six helpers.** (a) `internal`: they were never released, `Native/` is
   documented as not for user code, and the ruling of 14.11.2026 (old A.1) was internal.
   (b) Keep them public, with the new exceptions documented. Recommendation: (a).
2. **Type check.** (a) Strict match (`g_value_type_compatible`) with an enum path, as ruled
   on 14.11.2026 (old B.2). (b) `g_value_type_transformable`, which accepts int for uint.
   Recommendation: (a).
3. **The six v0.8.36 entry points.** (a) Restore `g_object_set_int/uint/bool` and
   `g_object_get_int/uint/bool` as `[Obsolete]` managed shims (same signatures, no
   `DllImport`) that forward to the checked helpers, adding the bool helpers, as ruled on
   14.11.2026 (old A.2, C.3): assemblies built against v0.8.36 keep working. (b) Leave them
   removed and list the break in the changelog. Recommendation: (a).

## Decisions

- **16.11.2026, gate 1, alex.** Spec and plan approved with the three recommendations.
  Question 1: the six helpers become `internal`. Question 2: strict type match with an enum
  path. Question 3: the six v0.8.36 entry points come back as `[Obsolete]` managed shims
  forwarding to the checked helpers, with bool helpers added. Replaces: the open questions
  above. Recorded here and on issue #14.

## Plan

1. `AravisSharp.Tests/GioTestNative.cs` ported (GIO constructors bound through the
   library's resolver, now internal).
2. Red tests in a new `AravisSharp.Tests/GObjectPropertyTests.cs` for AC-1 to AC-4 against
   `main`'s helpers; then the lookup, type check and read-back in `GLibNative`.
3. Question 1 and 3 changes, with a reflection test for each.
4. XML docs of the helpers (exceptions); changelog entry.
5. Whole suite Debug and Release on both frameworks; the GigE simulator tests in
   particular.
6. OCR review, C# review, silent-failure hunter and test analysis, as level complet asks.
