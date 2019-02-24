using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace HealingML.Unit
{
    public class SerializerTest
    {
        public class ValueModel
        {
            public int IntType { get; set; }
            public bool BoolType { get; set; }
            public long LongType { get; set; }
            public float FloatType { get; set; }
            public string StringType { get; set; }
        }

        public class ArrayValueModel
        {
            public object[] Array { get; set; }
        }

        public class ObjectValueModel
        {
            public object Inner { get; set; }
        }

        [Fact]
        public void ArrayValueSerializationTest()
        {
            var model = new ArrayValueModel
            {
                Array = new object[] {0, 1, 2}
            };

            var hml = Serializer.Print(model);

            Assert.Equal($"<ArrayValueModel hml:id=\"{model.GetHashCode()}\">\n  <hml:array hml:id=\"{model.Array.GetHashCode()}\" hml:name=\"Array\">\n    <Int32>0</Int32>\n    <Int32>1</Int32>\n    <Int32>2</Int32>\n  </hml:array>\n</ArrayValueModel>\n", hml);
        }


        [Fact]
        public void CircularArrayValueSerializationTest()
        {
            var model2 = new ObjectValueModel
            {
                Inner = new ObjectValueModel()
            };

            var model = new ArrayValueModel
            {
                Array = new object[]
                {
                    model2
                }
            };

            ((ObjectValueModel) model2.Inner).Inner = model;

            var hml = Serializer.Print(model);

            Assert.Equal($"<ArrayValueModel hml:id=\"{model.GetHashCode()}\">\n  <hml:array hml:id=\"{model.Array.GetHashCode()}\" hml:name=\"Array\">\n    <ObjectValueModel hml:id=\"{model2.GetHashCode()}\">\n      <ObjectValueModel hml:id=\"{model2.Inner.GetHashCode()}\" hml:name=\"Inner\">\n        <hml:ref hml:id=\"{model.GetHashCode()}\" hml:name=\"Inner\" />\n      </ObjectValueModel>\n    </ObjectValueModel>\n  </hml:array>\n</ArrayValueModel>\n", hml);
        }

        [Fact]
        public void CircularObjectValueSerializationTest()
        {
            var model = new ObjectValueModel
            {
                Inner = new ObjectValueModel()
            };

            ((ObjectValueModel) model.Inner).Inner = model;

            var hml = Serializer.Print(model);

            Assert.Equal($"<ObjectValueModel hml:id=\"{model.GetHashCode()}\">\n  <ObjectValueModel hml:id=\"{model.Inner.GetHashCode()}\" hml:name=\"Inner\">\n    <hml:ref hml:id=\"{model.GetHashCode()}\" hml:name=\"Inner\" />\n  </ObjectValueModel>\n</ObjectValueModel>\n", hml);
        }

        [Fact]
        public void CustomValueSerializationTest()
        {
            var model = new ValueModel
            {
                IntType = 1,
                BoolType = true,
                LongType = 1L,
                FloatType = 0.53f,
                StringType = "Hello World!"
            };

            var hml = Serializer.Print(model, new Dictionary<Type, ISerializer>
            {
                {typeof(string), new ByteStringSerializer()}
            });

            Assert.Equal($"<ValueModel hml:id=\"{model.GetHashCode()}\" IntType=\"1\" BoolType=\"True\" LongType=\"1\" FloatType=\"0.53\" StringType=\"48656C6C6F20576F726C6421\" />\n", hml);
        }


        [Fact]
        public void MixedArrayValueSerializationTest()
        {
            var model = new ArrayValueModel
            {
                Array = new object[] {0, 1.0, "Hello!", null}
            };

            var hml = Serializer.Print(model);

            Assert.Equal($"<ArrayValueModel hml:id=\"{model.GetHashCode()}\">\n  <hml:array hml:id=\"{model.Array.GetHashCode()}\" hml:name=\"Array\">\n    <Int32>0</Int32>\n    <Double>1</Double>\n    <String>Hello!</String>\n    <hml:null />\n  </hml:array>\n</ArrayValueModel>\n", hml);
        }

        [Fact]
        public void ObjectValueSerializationTest()
        {
            var model = new ObjectValueModel
            {
                Inner = new ObjectValueModel()
            };

            var hml = Serializer.Print(model);

            Assert.Equal($"<ObjectValueModel hml:id=\"{model.GetHashCode()}\">\n  <ObjectValueModel hml:id=\"{model.Inner.GetHashCode()}\" hml:name=\"Inner\" Inner=\"{{null}}\" />\n</ObjectValueModel>\n", hml);
        }

        [Fact]
        public void SelfCircularObjectValueSerializationTest()
        {
            var model = new ObjectValueModel();

            model.Inner = model;

            var hml = Serializer.Print(model);

            Assert.Equal($"<ObjectValueModel hml:id=\"{model.GetHashCode()}\">\n  <hml:ref hml:id=\"{model.GetHashCode()}\" hml:name=\"Inner\" />\n</ObjectValueModel>\n", hml);
        }

        [Fact]
        public void ValueSerializationTest()
        {
            var model = new ValueModel
            {
                IntType = 1,
                BoolType = true,
                LongType = 1L,
                FloatType = 0.53f,
                StringType = "Hello World!"
            };

            var hml = Serializer.Print(model);

            Assert.Equal($"<ValueModel hml:id=\"{model.GetHashCode()}\" IntType=\"1\" BoolType=\"True\" LongType=\"1\" FloatType=\"0.53\" StringType=\"Hello World!\" />\n", hml);
        }
    }

    public class ByteStringSerializer : ISerializer
    {
        public SerializationTarget OverrideTarget => SerializationTarget.Value;

        public object Print(object instance, HashSet<object> visited, IndentHelperBase indent, string name)
        {
            if (instance is string str) return BitConverter.ToString(Encoding.UTF8.GetBytes(str)).Replace("-", string.Empty);

            throw new InvalidDataException();
        }
    }
}
