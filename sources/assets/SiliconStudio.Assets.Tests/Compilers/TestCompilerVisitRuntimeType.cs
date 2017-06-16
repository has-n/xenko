using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SiliconStudio.Assets.Analysis;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Assets.Tests.Compilers
{
    [TestFixture]
    public class TestCompilerVisitRuntimeType : CompilerTestBase
    {
        [Test]
        public void CompilerVisitRuntimeType()
        {
            var package = new Package();
            // ReSharper disable once UnusedVariable - we need a package session to compile
            var packageSession = new PackageSession(package);
            var otherAssets = new List<AssetItem>
            {
                new AssetItem("contentRB", new MyAssetContentType(0), package),
                new AssetItem("contentRA", new MyAssetContentType(1), package),
                new AssetItem("content0B", new MyAssetContentType(2), package),
                new AssetItem("content0M", new MyAssetContentType(3), package),
                new AssetItem("content0A", new MyAssetContentType(4), package),
                new AssetItem("content1B", new MyAssetContentType(5), package),
                new AssetItem("content1M", new MyAssetContentType(6), package),
                new AssetItem("content1A", new MyAssetContentType(7), package),
                new AssetItem("content2B", new MyAssetContentType(8), package),
                new AssetItem("content2M", new MyAssetContentType(9), package),
                new AssetItem("content2A", new MyAssetContentType(10), package),
                new AssetItem("content3B", new MyAssetContentType(11), package),
                new AssetItem("content3M", new MyAssetContentType(12), package),
                new AssetItem("content3A", new MyAssetContentType(13), package),
                new AssetItem("content4B", new MyAssetContentType(14), package),
                new AssetItem("content4M", new MyAssetContentType(15), package),
                new AssetItem("content4A", new MyAssetContentType(16), package),
            };

            var assetToVisit = new MyAsset1();
            assetToVisit.Before = AttachedReferenceManager.CreateProxyObject<MyContentType>(otherAssets[0].Id, otherAssets[0].Location);
            assetToVisit.Zafter = AttachedReferenceManager.CreateProxyObject<MyContentType>(otherAssets[1].Id, otherAssets[1].Location);
            assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[2], otherAssets[3], otherAssets[4]));
            assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[5], otherAssets[6], otherAssets[7]));
            assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[8], otherAssets[9], otherAssets[10]));
            assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[11], otherAssets[12], otherAssets[13]));
            assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[14], otherAssets[15], otherAssets[16]));
            assetToVisit.RuntimeTypes[0].A = assetToVisit.RuntimeTypes[1];
            assetToVisit.RuntimeTypes[0].B = assetToVisit.RuntimeTypes[2];
            assetToVisit.RuntimeTypes[1].A = assetToVisit.RuntimeTypes[3];
            assetToVisit.RuntimeTypes[1].B = assetToVisit.RuntimeTypes[4];

            otherAssets.ForEach(x => package.Assets.Add(x));
            var assetItem = new AssetItem("asset", assetToVisit, package);
            package.Assets.Add(assetItem);
            package.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));

            // Create context
            var context = new AssetCompilerContext();

            // Builds the project
            var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
            context.Properties.Set(BuildAssetNode.VisitRuntimeTypes, true);
            var assetBuildResult = assetBuilder.Prepare(context);
            Assert.AreEqual(16, assetBuildResult.BuildSteps.Count);
        }

        private static MyRuntimeType CreateRuntimeType(AssetItem beforeReference, AssetItem middleReference, AssetItem afterReference)
        {
            var result = new MyRuntimeType
            {
                Before = CreateRef<MyContentType>(beforeReference),
                Middle = CreateRef<MyContentType>(middleReference),
                Zafter = CreateRef<MyContentType>(afterReference),
            };
            return result;
        }

        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContentType>), Profile = "Content")]
        public class MyContentType
        {
            public int Var;
        }

        [DataContract]
        public class MyRuntimeType
        {
            public MyContentType Before;
            public MyRuntimeType A;
            public MyContentType Middle;
            public MyRuntimeType B;
            public MyContentType Zafter;
        }

        [DataContract]
        [AssetDescription(FileExtension)]
        [AssetContentType(typeof(MyContentType))]
        public class MyAssetContentType : Asset
        {
            public const string FileExtension = ".xkmact";
            public int Var;
            public MyAssetContentType(int i) { Var = i; }
            public MyAssetContentType() { }
        }

        [DataContract]
        [AssetDescription(".xkmytest")]
        public class MyAsset1 : Asset
        {
            public MyContentType Before;
            public List<MyRuntimeType> RuntimeTypes = new List<MyRuntimeType>();
            public MyContentType Zafter;
        }

        [AssetCompiler(typeof(MyAsset1), typeof(AssetCompilationContext))]
        public class MyAsset1Compiler : TestAssertCompiler<MyAsset1>
        {
            public override IEnumerable<Type> GetRuntimeTypes(AssetCompilerContext context, AssetItem assetItem)
            {
                yield return typeof(MyRuntimeType);
            }
        }

        [AssetCompiler(typeof(MyAssetContentType), typeof(AssetCompilationContext))]
        public class MyAssetContentTypeCompiler : TestAssertCompiler<MyAssetContentType> { }
    }

    [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent1>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<MyContent1>))]
    public class MyContent1
    {
    }

    [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent2>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<MyContent2>))]
    public class MyContent2
    {
        public MyContent2()
        {

        }
    }

    [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent3>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<MyContent3>))]
    public class MyContent3
    {
    }

    [DataContract]
    [AssetDescription(".xkmytest")]
    [AssetContentType(typeof(MyContent1))]
    public class MyAsset1 : Asset
    {
        public MyContent2 MyContent2 { get; set; }
        public MyContent3 MyContent3 { get; set; }
    }

    [DataContract]
    [AssetDescription(".xkmytest")]
    [AssetContentType(typeof(MyContent2))]
    public class MyAsset2 : Asset
    {
        public MyContent3 MyContent3 { get; set; }
    }

    [DataContract]
    [AssetDescription(".xkmytest")]
    [AssetContentType(typeof(MyContent3))]
    public class MyAsset3 : Asset
    {
    }

    [AssetCompiler(typeof(MyAsset1), typeof(AssetCompilationContext))]
    public class MyAsset1Compiler : TestAssertCompiler<MyAsset1>
    {
        public override IEnumerable<KeyValuePair<Type, BuildDependencyType>> GetInputTypes(AssetCompilerContext context, AssetItem assetItem)
        {
            yield return new KeyValuePair<Type, BuildDependencyType>(typeof(MyAsset2), BuildDependencyType.Runtime);
            yield return new KeyValuePair<Type, BuildDependencyType>(typeof(MyAsset3), BuildDependencyType.CompileAsset);
        }
    }

    [AssetCompiler(typeof(MyAsset2), typeof(AssetCompilationContext))]
    public class MyAsset2Compiler : TestAssertCompiler<MyAsset2>
    {
        public override IEnumerable<KeyValuePair<Type, BuildDependencyType>> GetInputTypes(AssetCompilerContext context, AssetItem assetItem)
        {
            yield return new KeyValuePair<Type, BuildDependencyType>(typeof(MyAsset3), BuildDependencyType.Runtime);
        }
    }

    [AssetCompiler(typeof(MyAsset3), typeof(AssetCompilationContext))]
    public class MyAsset3Compiler : TestAssertCompiler<MyAsset3> { }
}
