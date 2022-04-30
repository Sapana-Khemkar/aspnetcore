// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace ResultsOfTGenerator;

public class Program
{
    private const int TYPE_ARG_COUNT = 6;

    public static void Main(string[] args)
    {
        // By default we assume we're being run in the context of the <repo>/src/Http/Http.Results/src
        var pwd = Directory.GetCurrentDirectory();
        var classTargetFilePath = Path.Combine(pwd, "ResultsOfT.Generated.cs");
        var testsTargetFilePath = Path.Combine(pwd, "..", "test", "ResultsOfTTests.Generated.cs");

        if (args.Length > 0)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid number of args specified. Must specify both class file path and test file path if args are passed.");
            }

            classTargetFilePath = args[0];
            testsTargetFilePath = args[1];
        }

        GenerateClassFile(classTargetFilePath, TYPE_ARG_COUNT, args.Length == 0);

        GenerateTestFiles(testsTargetFilePath, TYPE_ARG_COUNT, args.Length == 0);
    }

    public static void Run(string classFilePath, string testsFilePath)
    {
        GenerateClassFile(classFilePath, TYPE_ARG_COUNT, false);

        GenerateTestFiles(testsFilePath, TYPE_ARG_COUNT, false);
    }

    static void GenerateClassFile(string classFilePath, int typeArgCount, bool interactive = true)
    {
        Console.WriteLine($"Will generate class file at {classFilePath}");

        if (interactive)
        {
            Console.WriteLine("Press any key to continue or Ctrl-C to cancel");
            Console.ReadKey();
        }

        using var writer = new StreamWriter(classFilePath, append: false);

        // File header
        writer.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
        writer.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
        writer.WriteLine();
        writer.WriteLine("// This file is generated by a tool. See: src/Http/Http.Results/tools/ResultsOfTGenerator");
        writer.WriteLine();

        // Usings
        writer.WriteLine("using Microsoft.AspNetCore.Http.Metadata;");
        writer.WriteLine();

        // Namespace
        writer.WriteLine("namespace Microsoft.AspNetCore.Http.HttpResults;");
        writer.WriteLine();

        // Skip 1 as we don't have a Results<TResult1> class
        for (int i = 2; i <= typeArgCount; i++)
        {
            // Class summary doc
            writer.WriteLine("/// <summary>");
            writer.WriteLine($"/// An <see cref=\"IResult\"/> that could be one of {i.ToWords()} different <see cref=\"IResult\"/> types. On execution will");
            writer.WriteLine("/// execute the underlying <see cref=\"IResult\"/> instance that was actually returned by the HTTP endpoint.");
            writer.WriteLine("/// </summary>");

            // Class remarks doc
            writer.WriteLine("/// <remarks>");
            writer.WriteLine("/// An instance of this type cannot be created explicitly. Use the implicit cast operators to create an instance");
            writer.WriteLine("/// from an instance of one of the declared type arguments, e.g.");
            writer.WriteLine("/// <code>Results&lt;Ok, BadRequest&gt; result = TypedResults.Ok();</code>");
            writer.WriteLine("/// </remarks>");

            // Type params docs
            for (int j = 1; j <= i; j++)
            {
                writer.WriteLine(@$"/// <typeparam name=""TResult{j}"">The {j.ToOrdinalWords()} result type.</typeparam>");
            }

            // Class declaration
            writer.Write($"public sealed class Results<");

            // Type args
            for (int j = 1; j <= i; j++)
            {
                writer.Write($"TResult{j}");
                if (j != i)
                {
                    writer.Write(", ");
                }
            }
            writer.Write(">");

            // Interfaces
            writer.WriteLine(" : IResult, IEndpointMetadataProvider");

            // Type arg constraints
            for (int j = 1; j <= i; j++)
            {
                writer.WriteIndent($"where TResult{j} : IResult");
                if (j != i)
                {
                    writer.WriteLine();
                }
            }
            writer.WriteLine();
            writer.WriteLine("{");

            // Ctor
            writer.WriteIndentedLine("// Use implicit cast operators to create an instance");
            writer.WriteIndentedLine($"private Results(IResult activeResult)");
            writer.WriteIndentedLine("{");
            writer.WriteIndentedLine(2, "Result = activeResult;");
            writer.WriteIndentedLine("}");
            writer.WriteLine();

            // Result property
            writer.WriteIndentedLine("/// <summary>");
            writer.WriteIndentedLine($"/// Gets the actual <see cref=\"IResult\"/> returned by the <see cref=\"Endpoint\"/> route handler delegate.");
            writer.WriteIndentedLine("/// </summary>");
            writer.WriteIndentedLine("public IResult Result { get; }");
            writer.WriteLine();

            // ExecuteAsync method
            writer.WriteIndentedLine("/// <inheritdoc/>");
            writer.WriteIndentedLine("public Task ExecuteAsync(HttpContext httpContext)");
            writer.WriteIndentedLine("{");
            writer.WriteIndentedLine(2, "ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));");
            writer.WriteLine();
            writer.WriteIndentedLine(2, "if (Result is null)");
            writer.WriteIndentedLine(2, "{");
            writer.WriteIndentedLine(3, "throw new InvalidOperationException(\"The IResult assigned to the Result property must not be null.\");");
            writer.WriteIndentedLine(2, "}");
            writer.WriteLine();
            writer.WriteIndentedLine(2, "return Result.ExecuteAsync(httpContext);");
            writer.WriteIndentedLine("}");
            writer.WriteLine();

            // Implicit converter operators
            var sb = new StringBuilder();
            for (int j = 1; j <= i; j++)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "TResult{0}", j);
                if (j != i)
                {
                    sb.Append(", ");
                }
            }
            var typeArgsList = sb.ToString();

            for (int j = 1; j <= i; j++)
            {
                writer.WriteIndentedLine("/// <summary>");
                writer.WriteIndentedLine($"/// Converts the <typeparamref name=\"TResult{j}\"/> to a <see cref=\"Results{{{typeArgsList}}}\" />.");
                writer.WriteIndentedLine("/// </summary>");
                writer.WriteIndentedLine("/// <param name=\"result\">The result.</param>");
                writer.WriteIndentedLine($"public static implicit operator Results<{typeArgsList}>(TResult{j} result) => new(result);");

                if (i != j)
                {
                    writer.WriteLine();
                }
            }
            writer.WriteLine();

            // IEndpointMetadataProvider.PopulateMetadata
            writer.WriteIndentedLine("/// <inheritdoc/>");
            writer.WriteIndentedLine("static void IEndpointMetadataProvider.PopulateMetadata(EndpointMetadataContext context)");
            writer.WriteIndentedLine("{");
            writer.WriteIndentedLine(2, "ArgumentNullException.ThrowIfNull(context);");
            writer.WriteLine();
            for (int j = 1; j <= i; j++)
            {
                writer.WriteIndentedLine(2, $"ResultsOfTHelper.PopulateMetadataIfTargetIsIEndpointMetadataProvider<TResult{j}>(context);");
            }
            writer.WriteIndentedLine("}");

            // Class end
            writer.WriteLine("}");

            if (i != typeArgCount)
            {
                writer.WriteLine();
            }
        }

        writer.Flush();
        writer.Close();

        var file = new FileInfo(classFilePath);

        if (!file.Exists)
        {
            throw new FileNotFoundException(classFilePath);
        }

        Console.WriteLine();
        Console.WriteLine($"{file.Length:N0} bytes written to {file.FullName} successfully!");
        Console.WriteLine();
    }

    static void GenerateTestFiles(string testFilePath, int typeArgCount, bool interactive = true)
    {
        Console.WriteLine($"Will generate tests file at {testFilePath}");

        if (interactive)
        {
            Console.WriteLine("Press any key to continue or Ctrl-C to cancel");
            Console.ReadKey();
        }

        using var writer = new StreamWriter(testFilePath, append: false);

        // File header
        writer.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
        writer.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
        writer.WriteLine();
        writer.WriteLine("// This file is generated by a tool. See: src/Http/Http.Results/tools/ResultsOfTGenerator");

        // Using statements
        writer.WriteLine("using System.Reflection;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine("using Microsoft.AspNetCore.Http.Metadata;");
        writer.WriteLine("using Microsoft.AspNetCore.Http.HttpResults;");
        writer.WriteLine("using Microsoft.Extensions.DependencyInjection;");
        writer.WriteLine("using Microsoft.Extensions.Logging;");
        writer.WriteLine("using Microsoft.Extensions.Logging.Abstractions;");
        writer.WriteLine();

        // Namespace
        writer.WriteLine("namespace Microsoft.AspNetCore.Http.Result;");
        writer.WriteLine();

        // Class declaration
        writer.WriteLine($"public partial class ResultsOfTTests");
        writer.WriteLine("{");

        for (int i = 1; i <= typeArgCount; i++)
        {
            // Skip first as we don't have a Results<TResult1> class
            if (i == 1)
            {
                continue;
            }

            GenerateTest_Result_IsAssignedResult(writer, i);
            GenerateTest_ExecuteResult_ExecutesAssignedResult(writer, i);
            GenerateTest_Throws_ArgumentNullException_WhenHttpContextIsNull(writer, i);
            GenerateTest_Throws_InvalidOperationException_WhenResultIsNull(writer, i);
            GenerateTest_AcceptsIResult_AsAnyTypeArg(writer, i);
            GenerateTest_AcceptsNestedResultsOfT_AsAnyTypeArg(writer, i);
            GenerateTest_PopulateMetadata_PopulatesMetadataFromTypeArgsThatImplementIEndpointMetadataProvider(writer, i);
            GenerateTest_PopulateMetadata_Throws_ArgumentNullException_WhenContextIsNull(writer, i);
        }

        Generate_ChecksumResultClass(writer);

        // CustomResult classes
        writer.WriteLine();
        for (int i = 1; i <= typeArgCount + 1; i++)
        {
            Generate_ChecksumResultClass(writer, i);
            Generate_ProvidesMetadataResultClass(writer, i);

            if (i != typeArgCount)
            {
                writer.WriteLine();
            }
        }

        // End test class
        writer.WriteLine("}");

        writer.Flush();
        writer.Close();

        var file = new FileInfo(testFilePath);

        if (!file.Exists)
        {
            throw new FileNotFoundException(testFilePath);
        }

        Console.WriteLine();
        Console.WriteLine($"{file.Length:N0} bytes written to {file.FullName} successfully!");
    }

    static void GenerateTest_Result_IsAssignedResult(StreamWriter writer, int typeArgNumber)
    {
        //[Theory]
        //[InlineData(1, typeof(ChecksumResult1))]
        //[InlineData(2, typeof(ChecksumResult2))]
        //public void ResultsOfTResult1TResult2_Result_IsAssignedResult(int input, Type expectedResultType)
        //{
        //    // Arrange
        //    Results<CustomResult1, CustomResult2> MyApi(int id)
        //    {
        //        return id switch
        //        {
        //            1 => new CustomResult1(),
        //            _ => new CustomResult2()
        //        };
        //    }

        //    // Act
        //    var result = MyApi(input);

        //    // Assert
        //    Assert.IsType(expectedResultType, result.Result);
        //}

        // Attributes
        writer.WriteIndentedLine("[Theory]");

        // InlineData
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.WriteIndentedLine($"[InlineData({j}, typeof(ChecksumResult{j}))]");
        }

        // Method
        writer.WriteIndent(1, "public void ResultsOf");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine("_Result_IsAssignedResult(int input, Type expectedResultType)");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndent(2, "Results<");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"ChecksumResult{j}");
            if (typeArgNumber != j)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine("> MyApi(int id)");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "return id switch");
        writer.WriteIndentedLine(3, "{");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            if (j != typeArgNumber)
            {
                writer.WriteIndentedLine(4, $"{j} => new ChecksumResult{j}(),");
            }
            else
            {
                writer.WriteIndentedLine(4, $"_ => new ChecksumResult{j}()");
            }
        }
        writer.WriteIndentedLine(3, "};");
        writer.WriteIndentedLine(2, "}");
        writer.WriteLine();

        // Act
        writer.WriteIndentedLine(2, "// Act");
        writer.WriteIndentedLine(2, "var result = MyApi(input);");
        writer.WriteLine();

        // Assert
        writer.WriteIndentedLine(2, "// Assert");
        writer.WriteIndentedLine(2, "Assert.IsType(expectedResultType, result.Result);");

        // End of method
        writer.WriteIndentedLine("}");
        writer.WriteLine();
    }

    static void GenerateTest_ExecuteResult_ExecutesAssignedResult(StreamWriter writer, int typeArgNumber)
    {
        //[Theory]
        //[InlineData(1, 1)]
        //[InlineData(2, 2)]
        //[InlineData(-1, null)]
        //public async Task ResultsOfTResult1TResult2_ExecuteResult_ExecutesAssignedResult(int input, object expected)
        //{
        //    // Arrange
        //    Results<ChecksumResult1, ChecksumResult2, NoContent> MyApi(int checksum)
        //    {
        //        return checksum switch
        //        {
        //            1 => new ChecksumResult1(checksum),
        //            2 => new ChecksumResult2(checksum),
        //            _ => (NoContent)Results.NoContent()
        //        };
        //    }
        //    var httpContext = GetHttpContext();

        //    // Act
        //    var result = MyApi(input);
        //    await result.ExecuteAsync(httpContext);

        //    // Assert
        //    Assert.Equal(expected, httpContext.Items[nameof(ChecksumResult.Checksum)]);
        //}

        // Attributes
        writer.WriteIndentedLine("[Theory]");

        // InlineData
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.WriteIndentedLine($"[InlineData({j})]");
        }

        // Method
        // public void ResultsOfTResult1TResult2_ExecuteResult_ExecutesAssignedResult(int input, object expected)
        writer.WriteIndent(1, "public async Task ResultsOf");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine("_ExecuteResult_ExecutesAssignedResult(int input)");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndent(2, "Results<");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"ChecksumResult{j}");
            if (typeArgNumber != j)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine("> MyApi(int checksum)");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "return checksum switch");
        writer.WriteIndentedLine(3, "{");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            if (j < typeArgNumber)
            {
                writer.WriteIndentedLine(4, $"{j} => new ChecksumResult{j}(checksum),");
            }
            else
            {
                writer.WriteIndentedLine(4, $"_ => new ChecksumResult{j}(checksum)");
            }
        }
        writer.WriteIndentedLine(3, "};");
        writer.WriteIndentedLine(2, "}");
        writer.WriteIndentedLine(2, "var httpContext = GetHttpContext();");
        writer.WriteLine();

        // Act
        writer.WriteIndentedLine(2, "// Act");
        writer.WriteIndentedLine(2, "var result = MyApi(input);");
        writer.WriteIndentedLine(2, "await result.ExecuteAsync(httpContext);");
        writer.WriteLine();

        // Assert
        writer.WriteIndentedLine(2, "// Assert");
        writer.WriteIndentedLine(2, "Assert.Equal(input, httpContext.Items[nameof(ChecksumResult.Checksum)]);");

        // End of method
        writer.WriteIndentedLine("}");
        writer.WriteLine();
    }

    static void GenerateTest_Throws_ArgumentNullException_WhenHttpContextIsNull(StreamWriter writer, int typeArgNumber)
    {
        //[Fact]
        //public void ResultsOfTResult1TResult2_Throws_ArgumentNullException_WhenHttpContextIsNull()
        //{
        //    // Arrange
        //    Results<ChecksumResult1, NoContent> MyApi()
        //    {
        //        return new ChecksumResult1(1);
        //    }
        //    HttpContext httpContext = null;

        //    // Act & Assert
        //    var result = MyApi();

        //    Assert.ThrowsAsync<ArgumentNullException>(async () =>
        //    {
        //        await result.ExecuteAsync(httpContext);
        //    });
        //}

        // Attributes
        writer.WriteIndentedLine("[Fact]");

        // Start method
        writer.WriteIndent(1, "public void ResultsOf");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine("_Throws_ArgumentNullException_WhenHttpContextIsNull()");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndentedLine(2, "Results<ChecksumResult1, NoContent> MyApi()");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "return new ChecksumResult1(1);");
        writer.WriteIndentedLine(2, "}");
        writer.WriteIndentedLine(2, "HttpContext httpContext = null;");
        writer.WriteLine();

        // Act & Assert
        writer.WriteIndentedLine(2, "// Act & Assert");
        writer.WriteIndentedLine(2, "var result = MyApi();");
        writer.WriteLine();

        writer.WriteIndentedLine(2, "Assert.ThrowsAsync<ArgumentNullException>(async () =>");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "await result.ExecuteAsync(httpContext);");
        writer.WriteIndentedLine(2, "});");

        // Close method
        writer.WriteIndentedLine(1, "}");
        writer.WriteLine();
    }

    static void GenerateTest_Throws_InvalidOperationException_WhenResultIsNull(StreamWriter writer, int typeArgNumber)
    {
        //[Fact]
        //public void ResultsOfTResult1TResult2_Throws_InvalidOperationException_WhenResultIsNull()
        //{
        //    // Arrange
        //    Results<ChecksumResult1, NoContent> MyApi()
        //    {
        //        return (ChecksumResult1)null;
        //    }
        //    var httpContext = GetHttpContext();

        //    // Act & Assert
        //    var result = MyApi();

        //    Assert.ThrowsAsync<InvalidOperationException>(async () =>
        //    {
        //        await result.ExecuteAsync(httpContext);
        //    });
        //}

        // Attributes
        writer.WriteIndentedLine("[Fact]");

        // Start method
        writer.WriteIndent(1, "public void ResultsOf");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine("_Throws_InvalidOperationException_WhenResultIsNull()");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndentedLine(2, "Results<ChecksumResult1, NoContent> MyApi()");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "return new ChecksumResult1(1);");
        writer.WriteIndentedLine(2, "}");
        writer.WriteIndentedLine(2, "var httpContext = GetHttpContext();");
        writer.WriteLine();

        // Act & Assert
        writer.WriteIndentedLine(2, "// Act & Assert");
        writer.WriteIndentedLine(2, "var result = MyApi();");
        writer.WriteLine();

        writer.WriteIndentedLine(2, "Assert.ThrowsAsync<InvalidOperationException>(async () =>");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "await result.ExecuteAsync(httpContext);");
        writer.WriteIndentedLine(2, "});");

        // Close method
        writer.WriteIndentedLine(1, "}");
        writer.WriteLine();
    }

    static void GenerateTest_AcceptsIResult_AsAnyTypeArg(StreamWriter writer, int typeArgCount)
    {
        for (int i = 1; i <= typeArgCount; i++)
        {
            GenerateTest_AcceptsIResult_AsNthTypeArg(writer, typeArgCount, i);
        }
    }

    static void GenerateTest_AcceptsIResult_AsNthTypeArg(StreamWriter writer, int typeArgCount, int typeArgNumber)
    {
        //[Theory]
        //[InlineData(1, typeof(ChecksumResult1))]
        //[InlineData(2, typeof(ChecksumResult2))]
        //public async Task ResultsOfTResult1TResult2_AcceptsIResult_AsFirstTypeArg(int input, Type expectedResultType)
        //{
        //    // Arrange
        //    Results<IResult, ChecksumResult2> MyApi(int id)
        //    {
        //        return id switch
        //        {
        //            1 => new ChecksumResult1(1),
        //            _ => new ChecksumResult2(2)
        //        };
        //    }
        //    var httpContext = GetHttpContext();

        //    // Act
        //    var result = MyApi(input);
        //    await result.ExecuteAsync(httpContext);

        //    // Assert
        //    Assert.IsType(expectedResultType, result.Result);
        //    Assert.Equal(input, httpContext.Items[nameof(ChecksumResult.Checksum)]);
        //}

        // Attributes
        writer.WriteIndentedLine("[Theory]");

        // InlineData
        for (int j = 1; j <= typeArgCount; j++)
        {
            writer.WriteIndentedLine($"[InlineData({j}, typeof(ChecksumResult{j}))]");
        }

        // Start method
        writer.WriteIndent(1, "public async Task ResultsOf");
        for (int j = 1; j <= typeArgCount; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine($"_AcceptsIResult_As{typeArgNumber.ToOrdinalWords().TitleCase()}TypeArg(int input, Type expectedResultType)");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndent(2, "Results<");
        for (int j = 1; j <= typeArgCount; j++)
        {
            if (j == typeArgNumber)
            {
                writer.Write("IResult");
            }
            else
            {
                writer.Write($"ChecksumResult{j}");
            }

            if (j < typeArgCount)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine("> MyApi(int id)");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "return id switch");
        writer.WriteIndentedLine(3, "{");
        for (int j = 1; j <= typeArgCount; j++)
        {
            if (j < typeArgCount)
            {
                writer.WriteIndentedLine(4, $"{j} => new ChecksumResult{j}({j}),");
            }
            else
            {
                writer.WriteIndentedLine(4, $"_ => new ChecksumResult{j}({j})");
            }
        }
        writer.WriteIndentedLine(3, "};");
        writer.WriteIndentedLine(2, "}");
        writer.WriteIndentedLine(2, "var httpContext = GetHttpContext();");
        writer.WriteLine();

        // Act
        writer.WriteIndentedLine(2, "// Act");
        writer.WriteIndentedLine(2, "var result = MyApi(input);");
        writer.WriteIndentedLine(2, "await result.ExecuteAsync(httpContext);");
        writer.WriteLine();

        // Assert
        writer.WriteIndentedLine(2, "// Assert");
        writer.WriteIndentedLine(2, "Assert.IsType(expectedResultType, result.Result);");
        writer.WriteIndentedLine(2, "Assert.Equal(input, httpContext.Items[nameof(ChecksumResult.Checksum)]);");

        // Close method
        writer.WriteIndentedLine(1, "}");
        writer.WriteLine();
    }

    static void GenerateTest_AcceptsNestedResultsOfT_AsAnyTypeArg(StreamWriter writer, int typeArgCount)
    {
        for (int i = 1; i <= typeArgCount; i++)
        {
            GenerateTest_AcceptsNestedResultsOfT_AsNthTypeArg(writer, typeArgCount, i);
        }
    }

    static void GenerateTest_AcceptsNestedResultsOfT_AsNthTypeArg(StreamWriter writer, int typeArgCount, int typeArgNumber)
    {
        //[Theory]
        //[InlineData(1, typeof(Results<ChecksumResult1, ChecksumResult2>))]
        //[InlineData(2, typeof(Results<ChecksumResult1, ChecksumResult2>))]
        //[InlineData(3, typeof(ChecksumResult3))]
        //public async Task ResultsOfTResult1TResult2_AcceptsNestedResultsOfT_AsFirstTypeArg(int input, Type expectedResultType)
        //{
        //    // Arrange
        //    Results<Results<ChecksumResult1, ChecksumResult2>, ChecksumResult3> MyApi(int id)
        //    {
        //        return id switch
        //        {
        //            1 => (Results<ChecksumResult1, ChecksumResult2>)new ChecksumResult1(1),
        //            2 => (Results<ChecksumResult1, ChecksumResult2>)new ChecksumResult2(2),
        //            _ => new ChecksumResult3(3)
        //        };
        //    }
        //    var httpContext = GetHttpContext();

        //    // Act
        //    var result = MyApi(input);
        //    await result.ExecuteAsync(httpContext);

        //    // Assert
        //    Assert.IsType(expectedResultType, result.Result);
        //    Assert.Equal(input, httpContext.Items[nameof(ChecksumResult.Checksum)]);
        //}

        var sb = new StringBuilder("Results<");
        for (int j = 1; j <= typeArgCount; j++)
        {
            sb.Append(CultureInfo.InvariantCulture, $"ChecksumResult{j}");

            if (j < typeArgCount)
            {
                sb.Append(", ");
            }
        }
        sb.Append('>');
        var nestedResultTypeName = sb.ToString();

        // Attributes
        writer.WriteIndentedLine("[Theory]");

        // InlineData
        for (int j = 1; j <= typeArgCount + 1; j++)
        {
            if (j <= typeArgCount)
            {
                writer.WriteIndentedLine($"[InlineData({j}, typeof({nestedResultTypeName}))]");
            }
            else
            {
                writer.WriteIndentedLine($"[InlineData({j}, typeof(ChecksumResult{j}))]");
            }
        }

        // Start method
        writer.WriteIndent(1, "public async Task ResultsOf");
        for (int j = 1; j <= typeArgCount; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine($"_AcceptsNestedResultsOfT_As{typeArgNumber.ToOrdinalWords().TitleCase()}TypeArg(int input, Type expectedResultType)");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndent(2, "Results<");
        writer.WriteLine($"{nestedResultTypeName}, ChecksumResult{typeArgCount + 1}> MyApi(int id)");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, "return id switch");
        writer.WriteIndentedLine(3, "{");
        for (int j = 1; j <= typeArgCount; j++)
        {
            writer.WriteIndentedLine(4, $"{j} => ({nestedResultTypeName})new ChecksumResult{j}({j}),");
        }
        writer.WriteIndentedLine(4, $"_ => new ChecksumResult{typeArgCount + 1}({typeArgCount + 1})");
        writer.WriteIndentedLine(3, "};");
        writer.WriteIndentedLine(2, "}");
        writer.WriteIndentedLine(2, "var httpContext = GetHttpContext();");
        writer.WriteLine();

        // Act
        writer.WriteIndentedLine(2, "// Act");
        writer.WriteIndentedLine(2, "var result = MyApi(input);");
        writer.WriteIndentedLine(2, "await result.ExecuteAsync(httpContext);");
        writer.WriteLine();

        // Assert
        writer.WriteIndentedLine(2, "// Assert");
        writer.WriteIndentedLine(2, "Assert.IsType(expectedResultType, result.Result);");
        writer.WriteIndentedLine(2, "Assert.Equal(input, httpContext.Items[nameof(ChecksumResult.Checksum)]);");

        // Close method
        writer.WriteIndentedLine(1, "}");
        writer.WriteLine();
    }

    static void GenerateTest_PopulateMetadata_PopulatesMetadataFromTypeArgsThatImplementIEndpointMetadataProvider(StreamWriter writer, int typeArgNumber)
    {
        //[Fact]
        //public void ResultsOfTResult1TResult2_PopulateMetadata_PopulatesMetadataFromTypeArgsThatImplementIEndpointMetadataProvider()
        //{
        //    // Arrange
        //    Results<ProvidesMetadataResult1, ProvidesMetadataResult2> MyApi() { throw new NotImplementedException(); }
        //    var metadata = new List<object>();
        //    var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

        //    // Act
        //    PopulateMetadata<Results<ProvidesMetadataResult1, ProvidesMetadataResult2>>(context);

        //    // Assert
        //    Assert.Contains(context.EndpointMetadata, m => m is ResultTypeProvidedMetadata { SourceTypeName: nameof(ProvidesMetadataResult1) });
        //    Assert.Contains(context.EndpointMetadata, m => m is ResultTypeProvidedMetadata { SourceTypeName: nameof(ProvidesMetadataResult2) });
        //}

        // Attributes
        writer.WriteIndentedLine("[Fact]");

        // Start method
        writer.WriteIndent(1, "public void ResultsOf");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine("_PopulateMetadata_PopulatesMetadataFromTypeArgsThatImplementIEndpointMetadataProvider()");
        writer.WriteIndentedLine("{");

        // Arrange
        writer.WriteIndentedLine(2, "// Arrange");
        writer.WriteIndent(2, "Results<");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"ProvidesMetadataResult{j}");

            if (j != typeArgNumber)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine("> MyApi() { throw new NotImplementedException(); }");
        writer.WriteIndentedLine(2, "var metadata = new List<object>();");
        writer.WriteIndentedLine(2, "var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);");
        writer.WriteLine();

        // Act
        writer.WriteIndentedLine(2, "// Act");
        writer.WriteIndent(2, "PopulateMetadata<Results<");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"ProvidesMetadataResult{j}");

            if (j != typeArgNumber)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine(">>(context);");
        writer.WriteLine();

        // Assert
        writer.WriteIndentedLine(2, "// Assert");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.WriteIndentedLine(2, $"Assert.Contains(context.EndpointMetadata, m => m is ResultTypeProvidedMetadata {{ SourceTypeName: nameof(ProvidesMetadataResult{j}) }});");
        }

        // Close method
        writer.WriteIndentedLine(1, "}");
        writer.WriteLine();
    }

    static void GenerateTest_PopulateMetadata_Throws_ArgumentNullException_WhenContextIsNull(StreamWriter writer, int typeArgNumber)
    {
        //[Fact]
        //public void ResultsOfTResult1TResult2_PopulateMetadata_Throws_ArgumentNullException_WhenContextIsNull()
        //{
        //    // Act & Assert
        //    Assert.Throws<ArgumentNullException>("context", () => PopulateMetadata<Results<ProvidesMetadataResult1, ProvidesMetadataResult2>>(null));
        //}

        // Attributes
        writer.WriteIndentedLine("[Fact]");

        // Start method
        writer.WriteIndent(1, "public void ResultsOf");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"TResult{j}");
        }
        writer.WriteLine("_PopulateMetadata_Throws_ArgumentNullException_WhenContextIsNull()");
        writer.WriteIndentedLine("{");

        // Act & Assert
        writer.WriteIndentedLine(2, "// Act & Assert");
        writer.WriteIndent(2, "Assert.Throws<ArgumentNullException>(\"context\", () => PopulateMetadata<Results<");
        for (int j = 1; j <= typeArgNumber; j++)
        {
            writer.Write($"ProvidesMetadataResult{j}");

            if (j != typeArgNumber)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine(">>(null));");

        // Close method
        writer.WriteIndentedLine(1, "}");
        writer.WriteLine();
    }

    static void Generate_ChecksumResultClass(StreamWriter writer, int typeArgNumber = -1)
    {
        if (typeArgNumber <= 0)
        {
            writer.WriteIndentedLine(1, "abstract class ChecksumResult : IResult");
            writer.WriteIndentedLine(1, "{");
            writer.WriteIndentedLine(2, "public ChecksumResult(int checksum = 0)");
            writer.WriteIndentedLine(2, "{");
            writer.WriteIndentedLine(3, "Checksum = checksum;");
            writer.WriteIndentedLine(2, "}");
            writer.WriteLine();
            writer.WriteIndentedLine(2, "public int Checksum { get; }");
            writer.WriteLine();
            writer.WriteIndentedLine(2, "public Task ExecuteAsync(HttpContext httpContext)");
            writer.WriteIndentedLine(2, "{");
            writer.WriteIndentedLine(3, "httpContext.Items[nameof(ChecksumResult.Checksum)] = Checksum;");
            writer.WriteIndentedLine(3, "return Task.CompletedTask;");
            writer.WriteIndentedLine(2, "}");
            writer.WriteIndentedLine(1, "}");
        }
        else
        {
            // ChecksumResult class
            //class ChecksumResult1 : ChecksumResult
            //{
            //    public ChecksumResult1(int checksum = 0) : base(checksum) { }
            //}
            writer.WriteIndentedLine(1, $"class ChecksumResult{typeArgNumber} : ChecksumResult");
            writer.WriteIndentedLine(1, "{");
            writer.WriteIndentedLine(2, $"public ChecksumResult{typeArgNumber}(int checksum = 0) : base(checksum) {{ }}");
            writer.WriteIndentedLine(1, "}");
        }
    }

    static void Generate_ProvidesMetadataResultClass(StreamWriter writer, int typeArgNumber)
    {
        //private sealed class ProvidesMetadataResult1 : IResult, IEndpointMetadataProvider
        //{
        //    public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;

        //    public static void PopulateMetadata(EndpointMetadataContext context)
        //    {
        //        context.EndpointMetadata.Add(new ResultTypeProvidedMetadata { SourceTypeName = nameof(ProvidesMetadataResult1) });
        //    }
        //}
        writer.WriteIndentedLine(1, $"class ProvidesMetadataResult{typeArgNumber} : IResult, IEndpointMetadataProvider");
        writer.WriteIndentedLine(1, "{");
        writer.WriteIndentedLine(2, "public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;");
        writer.WriteLine();
        writer.WriteIndentedLine(2, "public static void PopulateMetadata(EndpointMetadataContext context)");
        writer.WriteIndentedLine(2, "{");
        writer.WriteIndentedLine(3, $"context.EndpointMetadata.Add(new ResultTypeProvidedMetadata {{ SourceTypeName = nameof(ProvidesMetadataResult{typeArgNumber}) }});");
        writer.WriteIndentedLine(2, "}");
        writer.WriteIndentedLine(1, "}");
    }
}
public static class StringExtensions
{
    public static void WriteIndent(this StreamWriter writer, string? value = null)
    {
        WriteIndent(writer, 1, value);
    }

    public static void WriteIndent(this StreamWriter writer, int count, string? value = null)
    {
        for (var i = 1; i <= count; i++)
        {
            writer.Write("    ");
        }

        if (value != null)
        {
            writer.Write(value);
        }
    }

    public static void WriteIndentedLine(this StreamWriter writer, string? value)
    {
        WriteIndentedLine(writer, 1, value);
    }

    public static void WriteIndentedLine(this StreamWriter writer, int count, string? value)
    {
        WriteIndent(writer, count, value);
        writer.WriteLine();
    }

    public static string ToWords(this int number) => number switch
    {
        1 => "one",
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        6 => "six",
        7 => "seven",
        8 => "eight",
        9 => "nine",
        10 => "ten",
        11 => "eleven",
        12 => "twelve",
        13 => "thirteen",
        14 => "fourteen",
        15 => "fifteen",
        16 => "sixteen",
        17 => "seventeen",
        18 => "eighteen",
        19 => "nineteen",
        20 => "twenty",
        _ => throw new NotImplementedException("Add more numbers")
    };

    public static string ToOrdinalWords(this int number) => number switch
    {
        1 => "first",
        2 => "second",
        3 => "third",
        4 => "fourth",
        5 => "fifth",
        6 => "sixth",
        7 => "seventh",
        8 => "eighth",
        9 => "ninth",
        10 => "tenth",
        11 => "eleventh",
        12 => "twelfth",
        13 => "thirteenth",
        14 => "fourteenth",
        15 => "fifteenth",
        16 => "sixteenth",
        17 => "seventeenth",
        18 => "eighteenth",
        19 => "nineteenth",
        20 => "twentieth",
        _ => throw new NotImplementedException("Add more numbers")
    };

    public static string TitleCase(this string value) => string.Create(value.Length, value, (c, s) =>
    {
        var origValueSpan = s.AsSpan();
        c[0] = char.ToUpper(origValueSpan[0], CultureInfo.InvariantCulture);
        origValueSpan[1..].TryCopyTo(c[1..]);
    });
}
