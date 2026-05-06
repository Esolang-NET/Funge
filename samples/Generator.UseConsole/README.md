# Esolang.Funge.Generator.UseConsole

`Esolang.Funge.Generator` の使い方を示すサンプルプロジェクトです。  
"Hello, World!" を出力する Funge-98 プログラム (`Programs/hello.b98`) を題材に、  
ジェネレーターがサポートするすべての戻り型パターンとインラインソースを実演します。

## プロジェクト構成

```
samples/Generator.UseConsole/
├── Programs/
│   └── hello.b98                          # Funge-98 ソースファイル
├── Esolang.Funge.Generator.UseConsole.cs  # サンプルコード（top-level statements）
└── Esolang.Funge.Generator.UseConsole.csproj
```

### hello.b98

```
64+"!dlroW ,olleH">:#,_@
```

文字列 `"Hello, World!"` をスタックに積み、1 文字ずつ出力する Funge-98 プログラムです。

## ジェネレーターのセットアップ方法

### 1. csproj に Generator を参照する

`OutputItemType="Analyzer"` / `ReferenceOutputAssembly="false"` でアナライザーとして参照します。

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Generator\Esolang.Funge.Generator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 2. `.b98` ファイルを `FungeSource` に追加する

`FungeSource` アイテムグループに追加すると、ビルド時に自動で `AdditionalFiles` へ変換されます。

```xml
<ItemGroup>
  <FungeSource Include="Programs\*.b98" />
</ItemGroup>
```

ビルドターゲットを有効にするため `.targets` を `Import` します（NuGet パッケージ版では自動）。

```xml
<Import Project="..\..\Generator\buildTransitive\Esolang.Funge.Generator.targets" />
```

### 3. `partial` メソッドに属性を付ける

クラスは `partial` 宣言が必要です。

```csharp
namespace Esolang.Funge
{
    partial class FungeSample
    {
        [GenerateFungeMethod("Programs/hello.b98")]
        public static partial string HelloWorld();
    }
}
```

## 戻り型パターンの一覧

このサンプルでは以下のすべての戻り型を示しています。

| メソッド | 宣言 | 説明 |
|---|---|---|
| `HelloWorld` | `partial string HelloWorld()` | 出力を文字列として返す |
| `HelloWorldAsync` | `partial Task<string> HelloWorldAsync()` | 非同期で出力を文字列として返す |
| `HelloWorldWriter` | `partial void HelloWorldWriter(TextWriter output)` | `TextWriter` に出力を書き込む |
| `HelloWorldBytes` | `partial IEnumerable<byte> HelloWorldBytes()` | 出力バイトを同期で列挙する |
| `HelloWorldBytesAsync` | `partial IAsyncEnumerable<byte> HelloWorldBytesAsync()` | 出力バイトを非同期で列挙する |
| `HelloWorldInline` | `partial string HelloWorldInline()` | インラインソース（後述） |

## インラインソース

`.b98` ファイルを用意しなくても、`InlineSource` プロパティで Funge-98 コードを文字列リテラルとして直接埋め込めます。

```csharp
// ファイルベース
[GenerateFungeMethod("Programs/hello.b98")]
public static partial string HelloWorld();

// インライン — .b98 ファイル不要
[GenerateFungeMethod(InlineSource = "64+\"!dlroW ,olleH\">:#,_@")]
public static partial string HelloWorldInline();
```

`InlineSource` が設定されている場合、`sourcePath` 引数は無視されます。

## 実行

```
dotnet run --framework net10.0
```

期待される出力:

```
HelloWorld: Hello, World!
HelloWorldAsync: Hello, World!
HelloWorldWriter: Hello, World!
HelloWorldBytes: Hello, World!
HelloWorldBytesAsync: Hello, World!
HelloWorldInline: Hello, World!
```
