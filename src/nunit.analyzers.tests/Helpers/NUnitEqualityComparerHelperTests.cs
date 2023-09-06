using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Analyzers.Helpers;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NUnit.Analyzers.Tests.Helpers
{
    public class NUnitEqualityComparerHelperTests
    {
        private static readonly Type[] NumericTypes = new[]
        {
            typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(short), typeof(ushort)
        };

        [TestCase(typeof(string), typeof(int))]
        [TestCase(typeof(string), typeof(bool))]
        [TestCase(typeof(int), typeof(bool))]
        [TestCase(typeof(decimal), typeof(bool))]
        [TestCase(typeof(List<string>), typeof(List<int>), TestName = "FalseWhenCollectionsWithIncompatibleItemTypesProvided")]
        [TestCase(typeof(Dictionary<string, string>), typeof(Dictionary<string, double>), TestName = "FalseWhenDictionariesWithIncompatibleValueTypesProvided")]
        [TestCase(typeof(Dictionary<int, string>), typeof(Dictionary<double, string>), TestName = "FalseWhenDictionariesWithDifferentNumericKeyTypesProvided")]
        [TestCase(typeof(Tuple<string, string>), typeof(Tuple<string, int>), TestName = "FalseWhenTuplesWithIncompatibleElementTypesProvided")]
        public void FalseWhenIncompatibleTypesProvided(Type leftType, Type rightType)
        {
            var compilation = TestHelpers.CreateCompilation();
            var leftTypeSymbol = GetTypeSymbol(compilation, leftType);
            var rightTypeSymbol = GetTypeSymbol(compilation, rightType);

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.False);
        }

        [Test]
        public void FalseWhenValueTuplesWithIncompatibleElementTypesProvided()
        {
            var compilation = TestHelpers.CreateCompilation();

            var intTypeSymbol = compilation.GetSpecialType(SpecialType.System_Int32);
            var stringTypeSymbol = compilation.GetSpecialType(SpecialType.System_String);
            var leftTypeSymbol = compilation.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(intTypeSymbol, intTypeSymbol));
            var rightTypeSymbol = compilation.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(intTypeSymbol, stringTypeSymbol));

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.False);
        }

        [Test]
        public void FalseWhenDifferentEnumsProvided()
        {
            var compilation = TestHelpers.CreateCompilation(@"
                enum EnumOne { A, B, C }
                enum EnumTwo { A, B, C }");
            var leftTypeSymbol = compilation.GetTypeByMetadataName("EnumOne");
            var rightTypeSymbol = compilation.GetTypeByMetadataName("EnumTwo");

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.False);
        }

        [Test]
        public void FalseWhenDifferentNullableEnumsProvided()
        {
            var compilation = TestHelpers.CreateCompilation(@"
                enum EnumOne { A, B, C }
                enum EnumTwo { A, B, C }");
            var nullableTypeSymbol = compilation.GetSpecialType(SpecialType.System_Nullable_T);
            var enumOneSymbol = compilation.GetTypeByMetadataName("EnumOne");
            Assert.That(enumOneSymbol, Is.Not.Null, "Cannot find type 'EnumOne'");
            var leftTypeSymbol = nullableTypeSymbol.Construct(enumOneSymbol);
            var rightTypeSymbol = compilation.GetTypeByMetadataName("EnumTwo");

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.False);
        }

        [Test]
        public void FalseWhenIncompatibleTypesWithCyclicTypesProvided()
        {
            var compilation = TestHelpers.CreateCompilation(@"
                class A : IEnumerable<A>
                {
                    public IEnumerator<A> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }

                class B : IEnumerable<B>
                {
                    public IEnumerator<B> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }");
            var leftTypeSymbol = compilation.GetTypeByMetadataName("A");
            var rightTypeSymbol = compilation.GetTypeByMetadataName("B");

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.False);
        }

        [TestCase(typeof(FileStream), typeof(MemoryStream), TestName = "TrueForStreams")]
        [TestCase(typeof(List<string>), typeof(string[]), TestName = "TrueForDifferentCollectionsWithSameType")]
        [TestCase(typeof(List<string>), typeof(ArrayList), TestName = "TrueForNonGenericCollections")]
        [TestCase(typeof(List<int>), typeof(double[]), TestName = "TrueForDifferentCollectionsWithNumerics")]
        [TestCase(typeof(Dictionary<string, int>), typeof(Dictionary<string, decimal>), TestName = "TrueForDictionariesOfNumerics")]
        [TestCase(typeof(KeyValuePair<string, int>), typeof(KeyValuePair<string, double>), TestName = "TrueForKeyValuePairsOfNumerics")]
        [TestCase(typeof(Tuple<string, double>), typeof(Tuple<string, decimal>), TestName = "TrueForTuplesWithCompatibleElementTypes")]
        public void TrueWhenCompatibleTypesProvided(Type leftType, Type rightType)
        {
            var compilation = TestHelpers.CreateCompilation();
            var leftTypeSymbol = GetTypeSymbol(compilation, leftType);
            var rightTypeSymbol = GetTypeSymbol(compilation, rightType);

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [TestCase(typeof(string))]
        [TestCase(typeof(int))]
        [TestCase(typeof(bool))]
        public void TrueWhenTypesAreSame(Type type)
        {
            var compilation = TestHelpers.CreateCompilation();
            var typeSymbol = GetTypeSymbol(compilation, type);

            Assert.That(NUnitEqualityComparerHelper.CanBeEqual(typeSymbol, typeSymbol, compilation), Is.True);
        }

        [Test]
        public void TrueWhenOneTypeInheritsAnother()
        {
            var compilation = TestHelpers.CreateCompilation(@"
                class A { }
                class B : A { }");
            var leftTypeSymbol = compilation.GetTypeByMetadataName("A");
            var rightTypeSymbol = compilation.GetTypeByMetadataName("B");

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void TrueForNumericTypes(
            [ValueSource(nameof(NumericTypes))] Type leftType,
            [ValueSource(nameof(NumericTypes))] Type rightType)
        {
            var compilation = TestHelpers.CreateCompilation();
            var leftTypeSymbol = GetTypeSymbol(compilation, leftType);
            var rightTypeSymbol = GetTypeSymbol(compilation, rightType);

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void TrueWhenSameNullableEnumsProvided()
        {
            var compilation = TestHelpers.CreateCompilation(@"
                enum EnumOne { A, B, C }");
            var nullableTypeSymbol = compilation.GetSpecialType(SpecialType.System_Nullable_T);
            var leftTypeSymbol = compilation.GetTypeByMetadataName("EnumOne");
            Assert.That(leftTypeSymbol, Is.Not.Null, "Cannot find type 'EnumOne'");
            var rightTypeSymbol = nullableTypeSymbol.Construct(leftTypeSymbol);

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void TrueWhenValueTuplesWithCompatibleElementTypesProvided()
        {
            var compilation = TestHelpers.CreateCompilation();

            var intTypeSymbol = compilation.GetSpecialType(SpecialType.System_Int32);
            var doubleTypeSymbol = compilation.GetSpecialType(SpecialType.System_Double);
            var leftTypeSymbol = compilation.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(intTypeSymbol, intTypeSymbol));
            var rightTypeSymbol = compilation.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(intTypeSymbol, doubleTypeSymbol));

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void TrueWhenActualHasIEquatableOfExpected()
        {
            var compilation = TestHelpers.CreateCompilation(@"
                class A : System.IEquatable<B>
                {
                    public bool Equals(B other) => true;
                }

                class B { }");
            var leftTypeSymbol = compilation.GetTypeByMetadataName("A");
            var rightTypeSymbol = compilation.GetTypeByMetadataName("B");

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void TrueForErrorType()
        {
            var compilation = TestHelpers.CreateCompilation();

            var leftTypeSymbol = compilation.GetSpecialType(SpecialType.System_Int32);
            var rightTypeSymbol = compilation.CreateErrorTypeSymbol(null, "ErrorType", 0);

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void TrueForArrayWithElementErrorType()
        {
            var compilation = TestHelpers.CreateCompilation();

            var errorType = compilation.CreateErrorTypeSymbol(null, "ErrorType", 0);
            var leftTypeSymbol = GetTypeSymbol(compilation, typeof(IEnumerable<int>));
            var rightTypeSymbol = compilation.CreateArrayTypeSymbol(errorType);

            AssertThatCommutativeEqual(leftTypeSymbol, rightTypeSymbol, compilation, Is.True);
        }

        [Test]
        public void UriCanEqualStringButStringCanNotEqualUri()
        {
            var compilation = TestHelpers.CreateCompilation();

            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            var uriSymbol = compilation.GetTypeByMetadataName("System.Uri");

            Assert.Multiple(() =>
            {
                Assert.That(NUnitEqualityComparerHelper.CanBeEqual(uriSymbol, stringSymbol, compilation), Is.True);
                Assert.That(NUnitEqualityComparerHelper.CanBeEqual(stringSymbol, uriSymbol, compilation), Is.False);
            });
        }

        private static void AssertThatCommutativeEqual(ITypeSymbol? leftTypeSymbol, ITypeSymbol? rightTypeSymbol, Compilation compilation, Constraint constraint)
        {
            Assert.Multiple(() =>
            {
                Assert.That(NUnitEqualityComparerHelper.CanBeEqual(leftTypeSymbol, rightTypeSymbol, compilation), constraint);
                Assert.That(NUnitEqualityComparerHelper.CanBeEqual(rightTypeSymbol, leftTypeSymbol, compilation), constraint);
            });
        }

        private static ITypeSymbol GetTypeSymbol(Compilation compilation, Type type)
        {
            if (type.IsArray)
            {
                // Unfortunately there is no way nullable knows that IsArray:true -> GetElementType:notnull
                var elementType = GetTypeSymbol(compilation, type.GetElementType()!);
                return compilation.CreateArrayTypeSymbol(elementType);
            }

            if (type.IsConstructedGenericType)
            {
                var fullyQualifiedGenericTypeDefinition = type.GetGenericTypeDefinition().FullName!;
                var genericTypeDefinitionSymbol = compilation.GetTypeByMetadataName(fullyQualifiedGenericTypeDefinition);
                Assert.That(genericTypeDefinitionSymbol, Is.Not.Null, $"Cannot find type '{fullyQualifiedGenericTypeDefinition}'");
                var genericArgumentsSymbols = type.GetGenericArguments().Select(t => GetTypeSymbol(compilation, t)).ToArray();

                return genericTypeDefinitionSymbol.Construct(genericArgumentsSymbols);
            }

            INamedTypeSymbol? namedTypeSymbol = compilation.GetTypeByMetadataName(type.FullName!);
            Assert.That(namedTypeSymbol, Is.Not.Null, $"Cannot find type '{type.FullName}'");
            return namedTypeSymbol;
        }
    }
}
