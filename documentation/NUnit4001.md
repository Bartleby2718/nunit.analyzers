# NUnit4001

## Simplify the ValuesAttribute

| Topic    | Value
| :--      | :--
| Id       | NUnit4001
| Severity | Info
| Enabled  | True
| Category | Style
| Code     | [SimplifyValuesAnalyzer](https://github.com/nunit/nunit.analyzers/blob/master/src/nunit.analyzers/SimplifyValues/SimplifyValuesAnalyzer.cs)

## Description

Consider simplifying the `ValuesAttribute` when you're using the `CombinatorialAttribute` (which is the default, if you don't specify the `PairwiseAttribute` or the `SequentialAttribute`).

## Motivation

When used without any arguments, the [Values] attribute on a (nullable) boolean or an (nullable) enum parameter will automatically include all possible values.

Therefore the `Values` attribute like
```csharp
[Test]
public void MyBoolTest([Values(true, false)] bool value) { /* ... */ } 
```
can be simplified to
```csharp
[Test]
public void MyBoolTest([Values] bool value) { /* ... */ } 
```

## How to fix violations

Remove all arguments of the `Values` attribute.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file

Configure the severity per project, for more info see
[MSDN](https://learn.microsoft.com/en-us/visualstudio/code-quality/using-rule-sets-to-group-code-analysis-rules?view=vs-2022).

### Via .editorconfig file

```ini
# NUnit4001: Simplify the ValuesAttribute
dotnet_diagnostic.NUnit4001.severity = chosenSeverity
```

where `chosenSeverity` can be one of `none`, `silent`, `suggestion`, `warning`, or `error`.

### Via #pragma directive

```csharp
#pragma warning disable NUnit4001 Simplify the ValuesAttribute
Code violating the rule here
#pragma warning restore NUnit4001 Simplify the ValuesAttribute
```

Or put this at the top of the file to disable all instances.

```csharp
#pragma warning disable NUnit4001 Simplify the ValuesAttribute
```

### Via attribute `[SuppressMessage]`

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Assertion",
    "NUnit4001: Simplify the ValuesAttribute",
    Justification = "Reason...")]
```
<!-- end generated config severity -->
